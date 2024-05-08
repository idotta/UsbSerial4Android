using Android.Hardware.Usb;
using Android.Util;
using Java.Lang;
using Java.Nio;
using UsbSerialAndroid.Exceptions;
using UsbSerialAndroid.Util;

namespace UsbSerialAndroid.Driver
{
    public abstract class CommonUsbSerialPort : IUsbSerialPort
    {
        public static bool DEBUG = false;

        private const string TAG = nameof(CommonUsbSerialPort);
        private const int MAX_READ_SIZE = 16 * 1024;

        protected readonly UsbDevice _device;
        protected readonly int _portNumber;

        protected UsbDeviceConnection? _connection = null;
        protected UsbEndpoint? _readEndpoint;
        protected UsbEndpoint? _writeEndpoint;
        protected UsbRequest? _usbRequest;

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
            return string.Format("<{0} device_name={1} device_id={2} port_number={3}>", GetType().Name, _device.DeviceName, _device.DeviceId, _portNumber);
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

        public UsbEndpoint GetWriteEndpoint()
        {
            return _writeEndpoint;
        }

        public UsbEndpoint GetReadEndpoint()
        {
            return _readEndpoint;
        }

        public string GetSerial()
        {
            return _connection?.Serial ?? string.Empty;
        }

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

            _connection = connection ?? throw new ArgumentException("Connection is null");
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
            catch (System.Exception)
            {
                try
                {
                    Close();
                }
                catch (System.Exception) { /* ignored */ }
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
            UsbDeviceConnection connection = _connection;
            _connection = null;
            try
            {
                _usbRequest.Cancel();
            }
            catch (System.Exception) { /* ignored */ }
            _usbRequest = null;
            try
            {
                CloseInt();
            }
            catch (System.Exception) { /* ignored */ }
            try
            {
                connection.Close();
            }
            catch (System.Exception) { /* ignored */ }
        }

        protected abstract void CloseInt();

        protected void TestConnection(bool full)
        {
            if (_connection == null)
            {
                throw new IOException("Connection closed");
            }
            if (!full)
            {
                return;
            }
            byte[] buf = new byte[2];
            int len = _connection.ControlTransfer((UsbAddressing)0x80, 0, 0, 0, buf, buf.Length, 200);
            if (len < 0)
            {
                throw new IOException("USB get_status request failed");
            }
        }

        public virtual int Read(byte[] dest, int timeout)
        {
            if (dest.Length == 0)
            {
                throw new ArgumentException("Read buffer too small");
            }
            return Read(dest, dest.Length, timeout);
        }

        public virtual int Read(byte[] dest, int length, int timeout)
        {
            return Read(dest, length, timeout, true);
        }

        protected virtual int Read(byte[] dest, int length, int timeout, bool testConnection)
        {
            if (_connection == null)
            {
                throw new IOException("Connection closed");
            }
            if (length <= 0)
            {
                throw new ArgumentException("Read length too small");
            }
            length = System.Math.Min(length, dest.Length);
            int nread;
            if (timeout != 0)
            {
                long endTime = testConnection ? MonotonicClock.Millis() + timeout : 0;
                int readMax = System.Math.Min(length, MAX_READ_SIZE);
                nread = _connection.BulkTransfer(_readEndpoint, dest, readMax, timeout);
                if (nread == -1 && testConnection)
                {
                    TestConnection(MonotonicClock.Millis() < endTime);
                }
            }
            else
            {
                if (_usbRequest == null)
                {
                    throw new IOException("Connection closed");
                }
                ByteBuffer buf = ByteBuffer.Wrap(dest, 0, length);
                if (!_usbRequest.Queue(buf))
                {
                    throw new IOException("Queueing USB request failed");
                }
                _ = _connection.RequestWait() ?? throw new IOException("Waiting for USB request failed");
                nread = buf.Position();
                if (nread == 0)
                {
                    TestConnection(true);
                }
            }
            return System.Math.Max(nread, 0);
        }

        public void Write(byte[] src, int timeout)
        {
            Write(src, 0, src.Length, timeout);
        }

        public void Write(byte[] src, int offset, int length, int timeout)
        {
            long endTime = timeout == 0 ? 0 : MonotonicClock.Millis() + timeout;
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

                    _writeBuffer ??= new byte[_writeEndpoint.MaxPacketSize];
                    requestLength = System.Math.Min(length - offset, _writeBuffer.Length);
                    if (offset == 0)
                    {
                        writeBuffer = src;
                    }
                    else
                    {
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
                        {
                            requestTimeout = -1;
                        }
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
                    Log.Debug(TAG, string.Format("Wrote {0}/{1} offset {2}/{3} timeout {4}", actualLength, requestLength, offset, length, requestTimeout));
                }
                if (actualLength <= 0)
                {
                    if (timeout != 0 && MonotonicClock.Millis() >= endTime)
                    {
                        SerialTimeoutException ex = new(string.Format("Error writing {0} bytes at offset {1} of total {2}, rc={3}", requestLength, offset, src.Length, actualLength))
                        {
                            BytesTransferred = offset
                        };
                        throw ex;
                    }
                    else
                    {
                        throw new IOException(string.Format("Error writing {0} bytes at offset {1} of total {2}", requestLength, offset, length));
                    }
                }
                offset += actualLength;
            }
        }

        public bool IsOpen()
        {
            return _connection != null;
        }

        public abstract void SetParameters(int baudRate, int dataBits, int stopBits, int parity);

        public virtual bool GetCD()
        {
            throw new NotSupportedException();
        }

        public virtual bool GetCTS()
        {
            throw new NotSupportedException();
        }

        public virtual bool GetDSR()
        {
            throw new NotSupportedException();
        }

        public virtual bool GetDTR()
        {
            throw new NotSupportedException();
        }

        public virtual void SetDTR(bool value)
        {
            throw new NotSupportedException();
        }

        public virtual bool GetRI()
        {
            throw new NotSupportedException();
        }

        public virtual bool GetRTS()
        {
            throw new NotSupportedException();
        }

        public virtual void SetRTS(bool value)
        {
            throw new NotSupportedException();
        }

        public virtual HashSet<ControlLine> GetControlLines()
        {
            throw new NotSupportedException();
        }

        public abstract HashSet<ControlLine> GetSupportedControlLines();

        public virtual void PurgeHwBuffers(bool purgeWriteBuffers, bool purgeReadBuffers)
        {
            throw new NotSupportedException();
        }

        public virtual void SetBreak(bool value)
        {
            throw new NotSupportedException();
        }

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
}
