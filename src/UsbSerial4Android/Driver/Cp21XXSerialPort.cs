using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class Cp21XXSerialPort : CommonUsbSerialPort
{
    private readonly Cp21XXSerialDriver _owningDriver;

    private const int USB_WRITE_TIMEOUT_MILLIS = 5000;

    // Configuration Request Types

    private const int REQTYPE_HOST_TO_DEVICE = 0x41;
    private const int REQTYPE_DEVICE_TO_HOST = 0xc1;

    // Configuration Request Codes

    private const int SILABSER_IFC_ENABLE_REQUEST_CODE = 0x00;
    private const int SILABSER_SET_LINE_CTL_REQUEST_CODE = 0x03;
    private const int SILABSER_SET_BREAK_REQUEST_CODE = 0x05;
    private const int SILABSER_SET_MHS_REQUEST_CODE = 0x07;
    private const int SILABSER_SET_BAUDRATE = 0x1E;
    private const int SILABSER_FLUSH_REQUEST_CODE = 0x12;
    private const int SILABSER_GET_MDMSTS_REQUEST_CODE = 0x08;

    private const int FLUSH_READ_CODE = 0x0a;
    private const int FLUSH_WRITE_CODE = 0x05;

    // SILABSER_IFC_ENABLE_REQUEST_CODE

    private const int UART_ENABLE = 0x0001;
    private const int UART_DISABLE = 0x0000;

    // SILABSER_SET_MHS_REQUEST_CODE

    private const int DTR_ENABLE = 0x101;
    private const int DTR_DISABLE = 0x100;
    private const int RTS_ENABLE = 0x202;
    private const int RTS_DISABLE = 0x200;

    // SILABSER_GET_MDMSTS_REQUEST_CODE

    private const int STATUS_CTS = 0x10;
    private const int STATUS_DSR = 0x20;
    private const int STATUS_RI = 0x40;
    private const int STATUS_CD = 0x80;

    private bool _dtr = false;
    private bool _rts = false;

    // second port of Cp2105 has limited baudRate, dataBits, stopBits, parity
    // unsupported baudrate returns error at controlTransfer(), other parameters are silently ignored
    private bool _isRestrictedPort;

    public Cp21XXSerialPort(Cp21XXSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
    {
        _owningDriver = owningDriver;
    }

    public override IUsbSerialDriver GetDriver() => _owningDriver;

    private void SetConfigSingle(int request, int value)
    {
        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }
        int? result = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, request, value, _portNumber, null, 0, USB_WRITE_TIMEOUT_MILLIS);
        if (result != 0)
        {
            throw new IOException("Control transfer failed: " + request + " / " + value + " -> " + result);
        }
    }

    private byte GetStatus()
    {
        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }
        byte[] buffer = new byte[1];
        int? result = _connection.ControlTransfer((UsbAddressing)REQTYPE_DEVICE_TO_HOST, SILABSER_GET_MDMSTS_REQUEST_CODE, 0, _portNumber, buffer, buffer.Length, USB_WRITE_TIMEOUT_MILLIS);
        if (result != buffer.Length)
        {
            throw new IOException("Control transfer failed: " + SILABSER_GET_MDMSTS_REQUEST_CODE + " / " + 0 + " -> " + result);
        }
        return buffer[0];
    }

    protected override void OpenInt()
    {
        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }

        _isRestrictedPort = _device.InterfaceCount == 2 && _portNumber == 1;
        if (_portNumber >= _device.InterfaceCount)
        {
            throw new IOException("Unknown port number");
        }
        UsbInterface dataIface = _device.GetInterface(_portNumber);
        if (!_connection.ClaimInterface(dataIface, true))
        {
            throw new IOException("Could not claim interface " + _portNumber);
        }
        for (int i = 0; i < dataIface.EndpointCount; i++)
        {
            UsbEndpoint ep = dataIface.GetEndpoint(i) ?? throw new IOException("No data endpoint");
            if (ep.Type == UsbAddressing.XferBulk)
            {
                if (ep.Direction == UsbAddressing.In)
                {
                    _readEndpoint = ep;
                }
                else
                {
                    _writeEndpoint = ep;
                }
            }
        }

        SetConfigSingle(SILABSER_IFC_ENABLE_REQUEST_CODE, UART_ENABLE);
        SetConfigSingle(SILABSER_SET_MHS_REQUEST_CODE, (_dtr ? DTR_ENABLE : DTR_DISABLE) | (_rts ? RTS_ENABLE : RTS_DISABLE));
    }

    protected override void CloseInt()
    {
        try
        {
            SetConfigSingle(SILABSER_IFC_ENABLE_REQUEST_CODE, UART_DISABLE);
        }
        catch (Exception)
        {
            // Ignore
        }
        try
        {
            _connection?.ReleaseInterface(_device.GetInterface(_portNumber));
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    private void SetBaudRate(int baudRate)
    {
        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }
        byte[] data =
        [
            (byte)(baudRate & 0xff),
            (byte)((baudRate >> 8) & 0xff),
            (byte)((baudRate >> 16) & 0xff),
            (byte)((baudRate >> 24) & 0xff)
        ];
        int? ret = _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, SILABSER_SET_BAUDRATE, 0, _portNumber, data, 4, USB_WRITE_TIMEOUT_MILLIS);
        if (ret < 0)
        {
            throw new IOException("Error setting baud rate");
        }
    }

    public override void SetParameters(int baudRate, int dataBits, int stopBits, int parity)
    {
        if (baudRate <= 0)
        {
            throw new ArgumentException("Invalid baud rate: " + baudRate);
        }
        SetBaudRate(baudRate);

        int configDataBits = 0;
        switch (dataBits)
        {
            case Constants.DATABITS_5:
                if (_isRestrictedPort)
                    throw new NotSupportedException("Unsupported data bits: " + dataBits);
                configDataBits |= 0x0500;
                break;

            case Constants.DATABITS_6:
                if (_isRestrictedPort)
                    throw new NotSupportedException("Unsupported data bits: " + dataBits);
                configDataBits |= 0x0600;
                break;

            case Constants.DATABITS_7:
                if (_isRestrictedPort)
                    throw new NotSupportedException("Unsupported data bits: " + dataBits);
                configDataBits |= 0x0700;
                break;

            case Constants.DATABITS_8:
                configDataBits |= 0x0800;
                break;

            default:
                throw new ArgumentException("Invalid data bits: " + dataBits);
        }

        switch (parity)
        {
            case Constants.PARITY_NONE:
                break;

            case Constants.PARITY_ODD:
                configDataBits |= 0x0010;
                break;

            case Constants.PARITY_EVEN:
                configDataBits |= 0x0020;
                break;

            case Constants.PARITY_MARK:
                if (_isRestrictedPort)
                    throw new NotSupportedException("Unsupported parity: mark");
                configDataBits |= 0x0030;
                break;

            case Constants.PARITY_SPACE:
                if (_isRestrictedPort)
                    throw new NotSupportedException("Unsupported parity: space");
                configDataBits |= 0x0040;
                break;

            default:
                throw new ArgumentException("Invalid parity: " + parity);
        }

        switch (stopBits)
        {
            case Constants.STOPBITS_1:
                break;

            case Constants.STOPBITS_1_5:
                throw new NotSupportedException("Unsupported stop bits: 1.5");
            
            case Constants.STOPBITS_2:
                if (_isRestrictedPort)
                    throw new NotSupportedException("Unsupported stop bits: 2");
                configDataBits |= 2;
                break;

            default:
                throw new ArgumentException("Invalid stop bits: " + stopBits);
        }
        SetConfigSingle(SILABSER_SET_LINE_CTL_REQUEST_CODE, configDataBits);
    }

    public override bool GetCD()
    {
        return (GetStatus() & STATUS_CD) != 0;
    }

    public override bool GetCTS()
    {
        return (GetStatus() & STATUS_CTS) != 0;
    }

    public override bool GetDSR()
    {
        return (GetStatus() & STATUS_DSR) != 0;
    }

    public override bool GetDTR()
    {
        return _dtr;
    }

    public override void SetDTR(bool value)
    {
        _dtr = value;
        SetConfigSingle(SILABSER_SET_MHS_REQUEST_CODE, _dtr ? DTR_ENABLE : DTR_DISABLE);
    }

    public override bool GetRI()
    {
        return (GetStatus() & STATUS_RI) != 0;
    }

    public override bool GetRTS()
    {
        return _rts;
    }

    public override void SetRTS(bool value)
    {
        _rts = value;
        SetConfigSingle(SILABSER_SET_MHS_REQUEST_CODE, _rts ? RTS_ENABLE : RTS_DISABLE);
    }

    public override HashSet<ControlLine> GetControlLines()
    {
        byte status = GetStatus();
        HashSet<ControlLine> set = [];
        if (_rts)
        {
            set.Add(ControlLine.RTS);
        }
        if ((status & STATUS_CTS) != 0)
        {
            set.Add(ControlLine.CTS);
        }
        if (_dtr)
        {
            set.Add(ControlLine.DTR);
        }
        if ((status & STATUS_DSR) != 0)
        {
            set.Add(ControlLine.DSR);
        }
        if ((status & STATUS_CD) != 0)
        {
            set.Add(ControlLine.CD);
        }
        if ((status & STATUS_RI) != 0)
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
        int value = (purgeReadBuffers ? FLUSH_READ_CODE : 0)
                | (purgeWriteBuffers ? FLUSH_WRITE_CODE : 0);

        if (value != 0)
        {
            SetConfigSingle(SILABSER_FLUSH_REQUEST_CODE, value);
        }
    }

    public override void SetBreak(bool value)
    {
        SetConfigSingle(SILABSER_SET_BREAK_REQUEST_CODE, value ? 1 : 0);
    }
}