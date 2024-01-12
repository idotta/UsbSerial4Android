/* Copyright 2011-2013 Google Inc.
 * Copyright 2013 mike wakerly <opensource@hoho.com>
 *
 * Project home page: https://github.com/mik3y/usb-serial-for-android
 */

using Android.Hardware.Usb;
using Android.Util;
using Java.Lang;
using UsbSerialAndroid.Utils;

namespace UsbSerialAndroid;

/// <summary>
/// A base class shared by several driver implementations.
/// @author mike wakerly (opensource@hoho.com)
/// </summary>
public abstract class CommonUsbSerialPort : IUsbSerialPort
{
    public static bool DEBUG = false;

    private const string TAG = nameof(CommonUsbSerialPort);

    private const int MAX_READ_SIZE = 16 * 1024; // = old bulkTransfer limit

    protected readonly UsbDevice _device;
    protected readonly int _portNumber;

    // non-null when open()
    protected UsbDeviceConnection? _connection = null;

    protected UsbEndpoint? _readEndpoint;
    protected UsbEndpoint? _writeEndpoint;
    protected UsbRequest? _usbRequest;

    /**
     * Internal write buffer.
     *  Guarded by {@link #mWriteBufferLock}.
     *  Default length = mReadEndpoint.getMaxPacketSize()
     **/
    protected byte[]? _writeBuffer;
    protected readonly object _writeBufferLock = new();

    private bool _disposedValue;

    protected CommonUsbSerialPort(UsbDevice device, int portNumber)
    {
        _device = device;
        _portNumber = portNumber;
    }

    public override string ToString()
    {
        return $"<{nameof(CommonUsbSerialPort)} device_name={_device.DeviceName} device_id={_device.DeviceId} port_number={_portNumber}>";
    }

    public abstract IUsbSerialDriver GetDriver();

    public UsbDevice GetDevice()
    {
        return _device;
    }

    public int GetPortNumber()
    {
        return _portNumber;
    }

    public UsbEndpoint? GetWriteEndpoint()
    {
        return _writeEndpoint;
    }

    public UsbEndpoint? GetReadEndpoint()
    {
        return _readEndpoint;
    }

    /**
     * Returns the device serial number
     *  @return serial number
     */

    public string GetSerial()
    {
        return _connection?.Serial ?? "";
    }

    /**
     * Sets the size of the internal buffer used to exchange data with the USB
     * stack for write operations.  Most users should not need to change this.
     *
     * @param bufferSize the size in bytes, <= 0 resets original size
     */

    private void SetWriteBufferSize(int bufferSize)
    {
        lock (_writeBufferLock)
        {
            if (bufferSize <= 0)
            {
                if (_writeEndpoint != null)
                {
                    bufferSize = _writeEndpoint.MaxPacketSize;
                }
                else
                {
                    _writeBuffer = null;
                    return;
                }
            }
            if (_writeBuffer != null && bufferSize == _writeBuffer.Length)
            {
                return;
            }
            _writeBuffer = new byte[bufferSize];
        }
    }

    public void Open(UsbDeviceConnection connection)
    {
        if (_connection != null)
        {
            throw new IOException("Already open");
        }

        _connection = connection ?? throw new IllegalArgumentException("Connection is null");

        try
        {
            OpenInt();
            if (_readEndpoint == null || _writeEndpoint == null)
            {
                throw new IOException("Could not get read & write endpoints");
            }
            _usbRequest = new UsbRequest();
            _usbRequest.Initialize(_connection, _readEndpoint);
        }
        catch (System.Exception e)
        {
            try
            {
                Close();
            }
            catch (System.Exception)
            {
                // ignored
            }
            throw;
        }
    }

    protected abstract void OpenInt();

    public void Close()
    {
        if (_connection == null)
        {
            throw new IOException("Already closed");
        }

        try
        {
            _usbRequest?.Cancel();
        }
        catch (System.Exception)
        {
            // ignored
        }
        _usbRequest = null;
        try
        {
            CloseInt();
        }
        catch (System.Exception)
        {
            // ignored
        }
        try
        {
            _connection.Close();
        }
        catch (System.Exception)
        {
            // ignored
        }
        _connection = null;
    }

    protected abstract void CloseInt();

    /**
     * use simple USB request supported by all devices to test if connection is still valid
     */

    protected void TestConnection()
    {
        if (_connection == null || _usbRequest == null)
        {
            throw new IOException("Connection closed");
        }

        byte[]
        buf = new byte[2];
        int len = _connection.ControlTransfer((UsbAddressing)0x80 /*DEVICE*/, 0 /*GET_STATUS*/, 0, 0, buf, buf.Length, 200);
        if (len < 0)
            throw new IOException("USB get_status request failed");
    }

    public virtual int Read(byte[] dest, int timeout)
    {
        if (dest.Length <= 0)
        {
            throw new IllegalArgumentException("Read buffer too small");
        }

        return Read(dest, dest.Length, timeout);
    }

    public virtual int Read(byte[] dest, int length, int timeout)
    {
        return Read(dest, length, timeout, true);
    }

    protected virtual int Read(byte[] dest, int length, int timeout, bool testConnection)

    {
        if (_connection == null || _usbRequest == null)
        {
            throw new IOException("Connection closed");
        }

        if (length <= 0)
        {
            throw new IllegalArgumentException("Read length too small");
        }
        length = System.Math.Min(length, dest.Length);
        int nread;
        if (timeout != 0)
        {
            // bulkTransfer will cause data loss with short timeout + high baud rates + continuous transfer
            //   https://stackoverflow.com/questions/9108548/android-usb-host-bulktransfer-is-losing-data
            // but mConnection.requestWait(timeout) available since Android 8.0 es even worse,
            // as it crashes with short timeout, e.g.
            //   A/libc: Fatal signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 0x276a in tid 29846 (pool-2-thread-1), pid 29618 (.usbserial.test)
            //     /system/lib64/libusbhost.so (usb_request_wait+192)
            //     /system/lib64/libandroid_runtime.so (android_hardware_UsbDeviceConnection_request_wait(_JNIEnv*, _jobject*, long)+84)
            // data loss / crashes were observed with timeout up to 200 msec
            long endTime = testConnection ? MonotonicClock.Millis() + timeout : 0;
            int readMax = System.Math.Min(length, MAX_READ_SIZE);
            nread = _connection.BulkTransfer(_readEndpoint, dest, readMax, timeout);
            // Android error propagation is improvable:
            //  nread == -1 can be: timeout, connection lost, buffer to small, ???
            if (nread == -1 && testConnection && MonotonicClock.Millis() < endTime)
                TestConnection();
        }
        else
        {
            Java.Nio.ByteBuffer buf = Java.Nio.ByteBuffer.Wrap(dest, 0, length);
            if (!_usbRequest.Queue(buf))
            {
                throw new IOException("Queueing USB request failed");
            }
            UsbRequest? response = (_connection?.RequestWait()) ?? throw new IOException("Waiting for USB request failed");
            nread = buf.Position();
            // Android error propagation is improvable:
            //   response != null & nread == 0 can be: connection lost, buffer to small, ???
            if (nread == 0)
            {
                TestConnection();
            }
        }
        return System.Math.Max(nread, 0);
    }

    public void Write(byte[] src, int timeout)
    {
        Write(src, src.Length, timeout);
    }

    public void Write(byte[] src, int length, int timeout)

    {
        int offset = 0;

        long endTime = (timeout == 0) ? 0 : (MonotonicClock.Millis() + timeout);
        length = System.Math.Min(length, src.Length);

        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }

        while (offset < length)
        {
            int requestTimeout;
            int requestLength;
            int actualLength;

            lock (_writeBufferLock)
            {
                byte[] writeBuffer;

                _writeBuffer ??= new byte[_writeEndpoint!.MaxPacketSize];
                requestLength = System.Math.Min(length - offset, _writeBuffer.Length);
                if (offset == 0)
                {
                    writeBuffer = src;
                }
                else
                {
                    // bulkTransfer does not support offsets, make a copy.
                    Array.Copy(src, offset, _writeBuffer, 0, requestLength);
                    writeBuffer = _writeBuffer;
                }
                if (timeout == 0 || offset == 0)
                {
                    requestTimeout = timeout;
                }
                else
                {
                    requestTimeout = (int)(endTime - MonotonicClock.Millis());
                    if (requestTimeout == 0)
                        requestTimeout = -1;
                }
                if (requestTimeout < 0)
                {
                    actualLength = -2;
                }
                else
                {
                    actualLength = _connection.BulkTransfer(_writeEndpoint, writeBuffer, requestLength, requestTimeout);
                }
            }
            if (DEBUG)
            {
                Log.Debug(TAG, "Wrote " + actualLength + "/" + requestLength + " offset " + offset + "/" + length + " timeout " + requestTimeout);
            }
            if (actualLength <= 0)
            {
                if (timeout != 0 && MonotonicClock.Millis() >= endTime)
                {
                    SerialTimeoutException ex = new("Error writing " + requestLength + " bytes at offset " + offset + " of total " + src.Length + ", rc=" + actualLength)
                    {
                        BytesTransferred = offset
                    };
                    throw ex;
                }
                else
                {
                    throw new IOException("Error writing " + requestLength + " bytes at offset " + offset + " of total " + length);
                }
            }
            offset += actualLength;
        }
    }

    public bool IsOpen()
    {
        return _connection != null;
    }

    public abstract void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity);

    public virtual bool GetCD()
    { throw new UnsupportedOperationException(); }

    public virtual bool GetCTS()
    { throw new UnsupportedOperationException(); }

    public virtual bool GetDSR()
    { throw new UnsupportedOperationException(); }

    public virtual bool GetDTR()
    { throw new UnsupportedOperationException(); }

    public virtual void SetDTR(bool value)
    { throw new UnsupportedOperationException(); }

    public virtual bool GetRI()
    { throw new UnsupportedOperationException(); }

    public virtual bool GetRTS()
    { throw new UnsupportedOperationException(); }

    public virtual void SetRTS(bool value)
    { throw new UnsupportedOperationException(); }

    public virtual HashSet<ControlLine> GetControlLines()
    { throw new UnsupportedOperationException(); }

    public abstract HashSet<ControlLine> GetSupportedControlLines();

    public virtual void PurgeHwBuffers(bool purgeWriteBuffers, bool purgeReadBuffers)
    { throw new UnsupportedOperationException(); }

    public virtual void SetBreak(bool value)
    { throw new UnsupportedOperationException(); }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~CommonUsbSerialPort()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}