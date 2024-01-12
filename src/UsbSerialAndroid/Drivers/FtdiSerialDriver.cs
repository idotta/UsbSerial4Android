/* Copyright 2011-2013 Google Inc.
 * Copyright 2013 mike wakerly <opensource@hoho.com>
 * Copyright 2020 kai morich <mail@kai-morich.de>
 *
 * Project home page: https://github.com/mik3y/usb-serial-for-android
 */

using Android.Hardware.Usb;
using UsbSerialAndroid.Utils;

namespace UsbSerialAndroid.Drivers;

public class FtdiSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new FtdiSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        Dictionary<int, int[]> supportedDevices = [];
        supportedDevices.Add(UsbId.VENDOR_FTDI,
                [
                    UsbId.FTDI_FT232R,
                    UsbId.FTDI_FT232H,
                    UsbId.FTDI_FT2232H,
                    UsbId.FTDI_FT4232H,
                    UsbId.FTDI_FT231X,  // same ID for FT230X, FT231X, FT234XD
                ]);
        return supportedDevices;
    }

    public bool Probe(UsbDevice device)
    {
        return false;
    }
}

/*
 * driver is implemented from various information scattered over FTDI documentation
 *
 * baud rate calculation https://www.ftdichip.com/Support/Documents/AppNotes/AN232B-05_BaudRates.pdf
 * control bits https://www.ftdichip.com/Firmware/Precompiled/UM_VinculumFirmware_V205.pdf
 * device type https://www.ftdichip.com/Support/Documents/AppNotes/AN_233_Java_D2XX_for_Android_API_User_Manual.pdf -> bvdDevice
 *
 */

public class FtdiSerialDriver : IUsbSerialDriver
{
    private const string TAG = nameof(FtdiSerialPort);

    private UsbDevice _device;
    private List<IUsbSerialPort> _ports;

    public FtdiSerialDriver(UsbDevice device)
    {
        _device = device;
        _ports = [];
        for (int port = 0; port < device.InterfaceCount; port++)
        {
            _ports.Add(new FtdiSerialPort(this, _device, port));
        }
    }

    public UsbDevice GetDevice()
    {
        return _device;
    }

    public List<IUsbSerialPort> GetPorts()
    {
        return _ports;
    }

    public class FtdiSerialPort : CommonUsbSerialPort
    {
        private readonly FtdiSerialDriver _owningDriver;
        private const int USB_WRITE_TIMEOUT_MILLIS = 5000;
        private const int READ_HEADER_LENGTH = 2; // contains MODEM_STATUS

        private const int REQTYPE_HOST_TO_DEVICE = UsbConstants.UsbTypeVendor | (int)UsbAddressing.Out;
        private const int REQTYPE_DEVICE_TO_HOST = UsbConstants.UsbTypeVendor | (int)UsbAddressing.In;

        private const int RESET_REQUEST = 0;
        private const int MODEM_CONTROL_REQUEST = 1;
        private const int SET_BAUD_RATE_REQUEST = 3;
        private const int SET_DATA_REQUEST = 4;
        private const int GET_MODEM_STATUS_REQUEST = 5;
        private const int SET_LATENCY_TIMER_REQUEST = 9;
        private const int GET_LATENCY_TIMER_REQUEST = 10;

        private const int MODEM_CONTROL_DTR_ENABLE = 0x0101;
        private const int MODEM_CONTROL_DTR_DISABLE = 0x0100;
        private const int MODEM_CONTROL_RTS_ENABLE = 0x0202;
        private const int MODEM_CONTROL_RTS_DISABLE = 0x0200;
        private const int MODEM_STATUS_CTS = 0x10;
        private const int MODEM_STATUS_DSR = 0x20;
        private const int MODEM_STATUS_RI = 0x40;
        private const int MODEM_STATUS_CD = 0x80;
        private const int RESET_ALL = 0;
        private const int RESET_PURGE_RX = 1;
        private const int RESET_PURGE_TX = 2;

        private bool _baudRateWithPort;
        private bool _dtr;
        private bool _rts;
        private int _breakConfig;

        public FtdiSerialPort(FtdiSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
        {
            _owningDriver = owningDriver ?? throw new ArgumentNullException(nameof(owningDriver));
        }

        public override IUsbSerialDriver GetDriver()
        {
            return _owningDriver;
        }

        protected override void OpenInt()
        {
            if (!_connection.ClaimInterface(_device.GetInterface(_portNumber), true))
            {
                throw new IOException("Could not claim interface " + _portNumber);
            }

            if (_device.GetInterface(_portNumber).EndpointCount < 2)
            {
                throw new IOException("Not enough endpoints");
            }

            _readEndpoint = _device.GetInterface(_portNumber).GetEndpoint(0);
            _writeEndpoint = _device.GetInterface(_portNumber).GetEndpoint(1);

            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, RESET_REQUEST,
                    RESET_ALL, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Reset failed: result=" + result);
            }
            result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, MODEM_CONTROL_REQUEST,
                            (_dtr ? MODEM_CONTROL_DTR_ENABLE : MODEM_CONTROL_DTR_DISABLE) |
                                    (_rts ? MODEM_CONTROL_RTS_ENABLE : MODEM_CONTROL_RTS_DISABLE),
                            _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Init RTS,DTR failed: result=" + result);
            }

            // _device.getVersion() would require API 23
            byte[] rawDescriptors = _connection.GetRawDescriptors();
            if (rawDescriptors == null || rawDescriptors.Length < 14)
            {
                throw new IOException("Could not get device descriptors");
            }
            int deviceType = rawDescriptors[13];
            _baudRateWithPort = deviceType == 7 || deviceType == 8 || deviceType == 9 // ...H devices
                            || _device.InterfaceCount > 1; // FT2232C
        }

        protected override void CloseInt()
        {
            try
            {
                _connection.ReleaseInterface(_device.GetInterface(_portNumber));
            }
            catch (Exception) { /* ignored */ }
        }

        public override int Read(byte[] dest, int timeout)
        {
            if (dest.Length <= READ_HEADER_LENGTH)
            {
                throw new Java.Lang.IllegalArgumentException("Read buffer too small");
                // could allocate larger buffer, including space for 2 header bytes, but this would
                // result in buffers not being 64 byte aligned any more, causing data loss at continuous
                // data transfer at high baud rates when buffers are fully filled.
            }
            return Read(dest, dest.Length, timeout);
        }

        public override int Read(byte[] dest, int length, int timeout)
        {
            if (length <= READ_HEADER_LENGTH)
            {
                throw new Java.Lang.IllegalArgumentException("Read length too small");
                // could allocate larger buffer, including space for 2 header bytes, but this would
                // result in buffers not being 64 byte aligned any more, causing data loss at continuous
                // data transfer at high baud rates when buffers are fully filled.
            }
            length = Math.Min(length, dest.Length);
            int nread;
            if (timeout != 0)
            {
                long endTime = MonotonicClock.Millis() + timeout;
                do
                {
                    nread = base.Read(dest, length, Math.Max(1, (int)(endTime - MonotonicClock.Millis())), false);
                } while (nread == READ_HEADER_LENGTH && MonotonicClock.Millis() < endTime);
                if (nread <= 0 && MonotonicClock.Millis() < endTime)
                    TestConnection();
            }
            else
            {
                do
                {
                    nread = base.Read(dest, length, timeout);
                } while (nread == READ_HEADER_LENGTH);
            }
            return ReadFilter(dest, nread);
            //if (_connection == null || _usbRequest == null)
            //{
            //    throw new IOException("Connection closed");
            //}
            //if (length <= 0)
            //{
            //    throw new Java.Lang.IllegalArgumentException("Read length too small");
            //}
            //int totalBytesRead;
            //int readMax = Math.Min(length, dest.Length);
            //totalBytesRead = _connection.BulkTransfer(_readEndpoint, dest, readMax, timeout);

            //if (totalBytesRead < READ_HEADER_LENGTH)
            //{
            //    throw new IOException("Expected at least " + READ_HEADER_LENGTH + " bytes");
            //}

            //return ReadFilter(dest, totalBytesRead);
        }

        protected int ReadFilter(byte[] buffer, int totalBytesRead)
        {
            int maxPacketSize = _readEndpoint.MaxPacketSize;
            int destPos = 0;
            for (int srcPos = 0; srcPos < totalBytesRead; srcPos += maxPacketSize)
            {
                int length = Math.Min(srcPos + maxPacketSize, totalBytesRead) - (srcPos + READ_HEADER_LENGTH);
                if (length < 0)
                    throw new IOException("Expected at least " + READ_HEADER_LENGTH + " bytes");
                Array.Copy(buffer, srcPos + READ_HEADER_LENGTH, buffer, destPos, length);
                destPos += length;
            }
            //Log.Debug(TAG, "read filter " + totalBytesRead + " -> " + destPos);
            return destPos;
        }

        private void SetBaudrate(int baudRate)
        {
            int divisor, subdivisor, effectiveBaudRate;
            if (baudRate > 3500000)
            {
                throw new Java.Lang.UnsupportedOperationException("Baud rate to high");
            }
            else if (baudRate >= 2500000)
            {
                divisor = 0;
                subdivisor = 0;
                effectiveBaudRate = 3000000;
            }
            else if (baudRate >= 1750000)
            {
                divisor = 1;
                subdivisor = 0;
                effectiveBaudRate = 2000000;
            }
            else
            {
                divisor = (24000000 << 1) / baudRate;
                divisor = (divisor + 1) >> 1; // round
                subdivisor = divisor & 0x07;
                divisor >>= 3;
                if (divisor > 0x3fff) // exceeds bit 13 at 183 baud
                    throw new Java.Lang.UnsupportedOperationException("Baud rate to low");
                effectiveBaudRate = (24000000 << 1) / ((divisor << 3) + subdivisor);
                effectiveBaudRate = (effectiveBaudRate + 1) >> 1;
            }
            double baudRateError = Math.Abs(1.0 - (effectiveBaudRate / (double)baudRate));
            if (baudRateError >= 0.031) // can happen only > 1.5Mbaud
                throw new Java.Lang.UnsupportedOperationException($"Baud rate deviation {(baudRateError * 100):0.##}% is higher than allowed 3%");
            int value = divisor;
            int index = 0;
            switch (subdivisor)
            {
                case 0: break; // 16,15,14 = 000 - sub-integer divisor = 0
                case 4: value |= 0x4000; break; // 16,15,14 = 001 - sub-integer divisor = 0.5
                case 2: value |= 0x8000; break; // 16,15,14 = 010 - sub-integer divisor = 0.25
                case 1: value |= 0xc000; break; // 16,15,14 = 011 - sub-integer divisor = 0.125
                case 3: value |= 0x0000; index |= 1; break; // 16,15,14 = 100 - sub-integer divisor = 0.375
                case 5: value |= 0x4000; index |= 1; break; // 16,15,14 = 101 - sub-integer divisor = 0.625
                case 6: value |= 0x8000; index |= 1; break; // 16,15,14 = 110 - sub-integer divisor = 0.75
                case 7: value |= 0xc000; index |= 1; break; // 16,15,14 = 111 - sub-integer divisor = 0.875
            }
            if (_baudRateWithPort)
            {
                index <<= 8;
                index |= _portNumber + 1;
            }
            //Log.Debug(TAG, "baud rate={}, effective={}, error={0:0.#}%, value=0x{0:04x}, index=0x{0:04x}, divisor={}, subdivisor={}", baudRate, effectiveBaudRate, baudRateError * 100, value, index, divisor, subdivisor);

            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, SET_BAUD_RATE_REQUEST,
                    value, index, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Setting baudrate failed: result=" + result);
            }
        }

        public override void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity)
        {
            if (baudRate <= 0)
            {
                throw new Java.Lang.IllegalArgumentException("Invalid baud rate: " + baudRate);
            }
            SetBaudrate(baudRate);

            int config = 0;
            config |= dataBits switch
            {
                DataBits._5 or DataBits._6 => throw new Java.Lang.UnsupportedOperationException("Unsupported data bits: " + dataBits),
                DataBits._7 or DataBits._8 => (int)dataBits,
                _ => throw new Java.Lang.IllegalArgumentException("Invalid data bits: " + dataBits),
            };

            switch (parity)
            {
                case Parity.None:
                    break;

                case Parity.Odd:
                    config |= 0x100;
                    break;

                case Parity.Even:
                    config |= 0x200;
                    break;

                case Parity.Mark:
                    config |= 0x300;
                    break;

                case Parity.Space:
                    config |= 0x400;
                    break;

                default:
                    throw new Java.Lang.IllegalArgumentException("Invalid parity: " + parity);
            }

            switch (stopBits)
            {
                case StopBits._1:
                    break;

                case StopBits._1_5:
                    throw new Java.Lang.UnsupportedOperationException("Unsupported stop bits: 1.5");
                case StopBits._2:
                    config |= 0x1000;
                    break;

                default:
                    throw new Java.Lang.IllegalArgumentException("Invalid stop bits: " + stopBits);
            }

            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, SET_DATA_REQUEST,
                    config, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Setting parameters failed: result=" + result);
            }
            _breakConfig = config;
        }

        private int GetStatus()
        {
            byte[] data = new byte[2];
            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_DEVICE_TO_HOST, GET_MODEM_STATUS_REQUEST,
                    0, _portNumber + 1, data, data.Length, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 2)
            {
                throw new IOException("Get modem status failed: result=" + result);
            }
            return data[0];
        }

        public override bool GetCD()
        {
            return (GetStatus() & MODEM_STATUS_CD) != 0;
        }

        public override bool GetCTS()
        {
            return (GetStatus() & MODEM_STATUS_CTS) != 0;
        }

        public override bool GetDSR()
        {
            return (GetStatus() & MODEM_STATUS_DSR) != 0;
        }

        public override bool GetDTR()
        {
            return _dtr;
        }

        public override void SetDTR(bool value)
        {
            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, MODEM_CONTROL_REQUEST,
                    value ? MODEM_CONTROL_DTR_ENABLE : MODEM_CONTROL_DTR_DISABLE, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Set DTR failed: result=" + result);
            }
            _dtr = value;
        }

        public override bool GetRI()
        {
            return (GetStatus() & MODEM_STATUS_RI) != 0;
        }

        public override bool GetRTS()
        {
            return _rts;
        }

        public override void SetRTS(bool value)
        {
            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, MODEM_CONTROL_REQUEST,
                    value ? MODEM_CONTROL_RTS_ENABLE : MODEM_CONTROL_RTS_DISABLE, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Set DTR failed: result=" + result);
            }
            _rts = value;
        }

        public override HashSet<ControlLine> GetControlLines()
        {
            int status = GetStatus();

            HashSet<ControlLine> set = [];

            if (_rts)
            {
                set.Add(ControlLine.RTS);
            }

            if ((status & MODEM_STATUS_CTS) != 0)
            {
                set.Add(ControlLine.CTS);
            }

            if (_dtr)
            {
                set.Add(ControlLine.DTR);
            }

            if ((status & MODEM_STATUS_DSR) != 0)
            {
                set.Add(ControlLine.DSR);
            }
            if ((status & MODEM_STATUS_CD) != 0)
            {
                set.Add(ControlLine.CD);
            }
            if ((status & MODEM_STATUS_RI) != 0)
            {
                set.Add(ControlLine.RI);
            }
            return set;
        }

        public override HashSet<ControlLine> GetSupportedControlLines()
        {
            return [ControlLine.RTS, ControlLine.CTS, ControlLine.DTR, ControlLine.DSR, ControlLine.CD, ControlLine.RI];
        }

        public override void PurgeHwBuffers(bool purgeWriteBuffers, bool purgeReadBuffers)
        {
            if (purgeWriteBuffers)
            {
                int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, RESET_REQUEST,
                        RESET_PURGE_RX, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
                if (result != 0)
                {
                    throw new IOException("Purge write buffer failed: result=" + result);
                }
            }

            if (purgeReadBuffers)
            {
                int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, RESET_REQUEST,
                        RESET_PURGE_TX, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
                if (result != 0)
                {
                    throw new IOException("Purge read buffer failed: result=" + result);
                }
            }
        }

        public override void SetBreak(bool value)
        {
            int config = _breakConfig;
            if (value) config |= 0x4000;
            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, SET_DATA_REQUEST,
                    config, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Setting BREAK failed: result=" + result);
            }
        }

        public void SetLatencyTimer(int latencyTime)
        {
            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, SET_LATENCY_TIMER_REQUEST,
                    latencyTime, _portNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Set latency timer failed: result=" + result);
            }
        }

        public int GetLatencyTimer()
        {
            byte[]
        data = new byte[1];
            int result = _connection.ControlTransfer((UsbAddressing)REQTYPE_DEVICE_TO_HOST, GET_LATENCY_TIMER_REQUEST,
                    0, _portNumber + 1, data, data.Length, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 1)
            {
                throw new IOException("Get latency timer failed: result=" + result);
            }
            return data[0];
        }
    }
}