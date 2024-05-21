using Android.Hardware.Usb;
using Android.Util;

namespace UsbSerial4Android.Driver;

internal sealed class Ch340SerialPort : CommonUsbSerialPort
{
    private const string TAG = nameof(Ch340SerialPort);

    private const int LCR_ENABLE_RX = 0x80;
    private const int LCR_ENABLE_TX = 0x40;
    private const int LCR_MARK_SPACE = 0x20;
    private const int LCR_PAR_EVEN = 0x10;
    private const int LCR_ENABLE_PAR = 0x08;
    private const int LCR_STOP_BITS_2 = 0x04;
    private const int LCR_CS8 = 0x03;
    private const int LCR_CS7 = 0x02;
    private const int LCR_CS6 = 0x01;
    private const int LCR_CS5 = 0x00;

    private const int GCL_CTS = 0x01;
    private const int GCL_DSR = 0x02;
    private const int GCL_RI = 0x04;
    private const int GCL_CD = 0x08;
    private const int SCL_DTR = 0x20;
    private const int SCL_RTS = 0x40;

    private readonly Ch34XSerialDriver _owningDriver;

    private const int USB_TIMEOUT_MILLIS = 5000;
    private const int DEFAULT_BAUD_RATE = 9600;

    private bool _dtr = false;
    private bool _rts = false;

    public Ch340SerialPort(Ch34XSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
    {
        _owningDriver = owningDriver ?? throw new ArgumentNullException(nameof(owningDriver));
    }

    public override IUsbSerialDriver GetDriver()
    {
        return _owningDriver;
    }

    protected override void OpenInt()
    {
        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }
        for (int i = 0; i < _device.InterfaceCount; i++)
        {
            UsbInterface usbIface = _device.GetInterface(i);
            if (!_connection.ClaimInterface(usbIface, true))
            {
                throw new IOException("Could not claim data interface");
            }
        }

        UsbInterface dataInterface = _device.GetInterface(_device.InterfaceCount - 1);
        for (int i = 0; i < dataInterface.EndpointCount; i++)
        {
            UsbEndpoint ep = dataInterface.GetEndpoint(i) ?? throw new IOException("No data endpoint");
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

        Initialize();
        SetBaudRate(DEFAULT_BAUD_RATE);
    }

    protected override void CloseInt()
    {
        try
        {
            for (int i = 0; i < _device.InterfaceCount; i++)
            {
                _connection?.ReleaseInterface(_device.GetInterface(i));
            }
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    private int ControlOut(int request, int value, int index)
    {
        if (_connection is null)
        {
            throw new IOException("Connection is invalid");
        }
        const int REQTYPE_HOST_TO_DEVICE = (int)UsbAddressing.Out | UsbConstants.UsbTypeVendor;
        return _connection.ControlTransfer((UsbAddressing)REQTYPE_HOST_TO_DEVICE, request, value, index, null, 0, USB_TIMEOUT_MILLIS);
    }

    private int ControlIn(int request, int value, int index, byte[] buffer)
    {
        if (_connection is null)
        {
            throw new IOException("Connection is invalid");
        }
        const int REQTYPE_DEVICE_TO_HOST = (int)UsbAddressing.In | UsbConstants.UsbTypeVendor;
        return _connection.ControlTransfer((UsbAddressing)REQTYPE_DEVICE_TO_HOST, request, value, index, buffer, buffer.Length, USB_TIMEOUT_MILLIS);
    }

    private void CheckState(string msg, int request, int value, int[] expected)
    {
        byte[] buffer = new byte[expected.Length];
        int ret = ControlIn(request, value, 0, buffer);

        if (ret < 0)
        {
            throw new IOException("Failed send cmd [" + msg + "]");
        }

        if (ret != expected.Length)
        {
            throw new IOException("Expected " + expected.Length + " bytes, but get " + ret + " [" + msg + "]");
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] == -1)
            {
                continue;
            }

            int current = buffer[i] & 0xff;
            if (expected[i] != current)
            {
                throw new IOException("Expected 0x" + expected[i].ToString("X2") + " byte, but get 0x" + current.ToString("X2") + " [" + msg + "]");
            }
        }
    }

    private void SetControlLines()
    {
        if (ControlOut(0xa4, ~((_dtr ? SCL_DTR : 0) | (_rts ? SCL_RTS : 0)), 0) < 0)
        {
            throw new IOException("Failed to set control lines");
        }
    }

    private byte GetStatus()
    {
        byte[] buffer = new byte[2];
        int ret = ControlIn(0x95, 0x0706, 0, buffer);
        if (ret < 0)
        {
            throw new IOException("Error getting control lines");
        }
        return buffer[0];
    }

    private void Initialize()
    {
        CheckState("init #1", 0x5f, 0, [-1 /* 0x27, 0x30 */, 0x00]);

        if (ControlOut(0xa1, 0, 0) < 0)
        {
            throw new IOException("Init failed: #2");
        }

        SetBaudRate(DEFAULT_BAUD_RATE);

        CheckState("init #4", 0x95, 0x2518, [-1 /* 0x56, c3*/, 0x00]);

        if (ControlOut(0x9a, 0x2518, LCR_ENABLE_RX | LCR_ENABLE_TX | LCR_CS8) < 0)
        {
            throw new IOException("Init failed: #5");
        }

        CheckState("init #6", 0x95, 0x0706, [-1/*0xf?*/, -1/*0xec,0xee*/]);

        if (ControlOut(0xa1, 0x501f, 0xd90a) < 0)
        {
            throw new IOException("Init failed: #7");
        }

        SetBaudRate(DEFAULT_BAUD_RATE);

        SetControlLines();

        CheckState("init #10", 0x95, 0x0706, [-1/* 0x9f, 0xff*/, -1/*0xec,0xee*/]);
    }

    private void SetBaudRate(int baudRate)
    {
        long factor;
        long divisor;

        if (baudRate == 921600)
        {
            divisor = 7;
            factor = 0xf300;
        }
        else
        {
            const long BAUDBASE_FACTOR = 1532620800;
            const int BAUDBASE_DIVMAX = 3;

#if DEBUG
            if ((baudRate & (3 << 29)) == (1 << 29))
            {
                baudRate &= ~(1 << 29); // for testing purpose bypass dedicated baud rate handling
            }
#endif
            factor = BAUDBASE_FACTOR / baudRate;
            divisor = BAUDBASE_DIVMAX;
            while ((factor > 0xfff0) && divisor > 0)
            {
                factor >>= 3;
                divisor--;
            }
            if (factor > 0xfff0)
            {
                throw new NotSupportedException("Unsupported baud rate: " + baudRate);
            }
            factor = 0x10000 - factor;
        }

        divisor |= 0x0080; // else ch341a waits until buffer full
        int val1 = (int)((factor & 0xff00) | divisor);
        int val2 = (int)(factor & 0xff);
        Log.Debug(TAG, $"baud rate={baudRate}, 0x1312=0x{val1:X4}, 0x0f2c=0x{val2:X4}");
        int ret = ControlOut(0x9a, 0x1312, val1);
        if (ret < 0)
        {
            throw new IOException("Error setting baud rate: #1)");
        }
        ret = ControlOut(0x9a, 0x0f2c, val2);
        if (ret < 0)
        {
            throw new IOException("Error setting baud rate: #2");
        }
    }

    public override void SetParameters(int baudRate, int dataBits, int stopBits, int parity)
    {
        if (baudRate <= 0)
        {
            throw new ArgumentException("Invalid baud rate: " + baudRate);
        }
        SetBaudRate(baudRate);

        int lcr = LCR_ENABLE_RX | LCR_ENABLE_TX;

        lcr |= dataBits switch
        {
            Constants.DATABITS_5 => LCR_CS5,
            Constants.DATABITS_6 => LCR_CS6,
            Constants.DATABITS_7 => LCR_CS7,
            Constants.DATABITS_8 => LCR_CS8,
            _ => throw new ArgumentException("Invalid data bits: " + dataBits),
        };

        switch (parity)
        {
            case Constants.PARITY_NONE:
                break;

            case Constants.PARITY_ODD:
                lcr |= LCR_ENABLE_PAR;
                break;

            case Constants.PARITY_EVEN:
                lcr |= LCR_ENABLE_PAR | LCR_PAR_EVEN;
                break;

            case Constants.PARITY_MARK:
                lcr |= LCR_ENABLE_PAR | LCR_MARK_SPACE;
                break;

            case Constants.PARITY_SPACE:
                lcr |= LCR_ENABLE_PAR | LCR_MARK_SPACE | LCR_PAR_EVEN;
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
                lcr |= LCR_STOP_BITS_2;
                break;

            default:
                throw new ArgumentException("Invalid stop bits: " + stopBits);
        }

        int ret = ControlOut(0x9a, 0x2518, lcr);
        if (ret < 0)
        {
            throw new IOException("Error setting control byte");
        }
    }

    public override bool GetCD() => (GetStatus() & GCL_CD) == 0;

    public override bool GetCTS() => (GetStatus() & GCL_CTS) == 0;

    public override bool GetDSR() => (GetStatus() & GCL_DSR) == 0;

    public override bool GetDTR() => _dtr;

    public override void SetDTR(bool value)
    {
        _dtr = value;
        SetControlLines();
    }

    public override bool GetRI() => (GetStatus() & GCL_RI) == 0;

    public override bool GetRTS() => _rts;

    public override void SetRTS(bool value)
    {
        _rts = value;
        SetControlLines();
    }

    public override HashSet<ControlLine> GetControlLines()
    {
        int status = GetStatus();
        HashSet<ControlLine> set = [];
        if (_rts)
        {
            set.Add(ControlLine.RTS);
        }
        if ((status & GCL_CTS) == 0)
        {
            set.Add(ControlLine.CTS);
        }
        if (_dtr)
        {
            set.Add(ControlLine.DTR);
        }
        if ((status & GCL_DSR) == 0)
        {
            set.Add(ControlLine.DSR);
        }
        if ((status & GCL_CD) == 0)
        {
            set.Add(ControlLine.CD);
        }
        if ((status & GCL_RI) == 0)
        {
            set.Add(ControlLine.RI);
        }
        return set;
    }

    public override HashSet<ControlLine> GetSupportedControlLines()
    {
        return [ControlLine.RTS, ControlLine.CTS, ControlLine.DTR, ControlLine.DSR, ControlLine.CD, ControlLine.RI];
    }

    public override void SetBreak(bool value)
    {
        byte[] req = new byte[2];
        if (ControlIn(0x95, 0x1805, 0, req) < 0)
        {
            throw new IOException("Error getting BREAK condition");
        }
        if (value)
        {
            req[0] = (byte)(req[0] & ~1);
            req[1] = (byte)(req[1] & ~0x40);
        }
        else
        {
            req[0] |= 1;
            req[1] |= 0x40;
        }
        int val = (req[1] & 0xff) << 8 | (req[0] & 0xff);
        if (ControlOut(0x9a, 0x1805, val) < 0)
        {
            throw new IOException("Error setting BREAK condition");
        }
    }
}