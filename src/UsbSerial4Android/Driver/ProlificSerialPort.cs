using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class ProlificSerialPort : CommonUsbSerialPort
{
    private const string TAG = nameof(ProlificSerialPort);

    private readonly ProlificSerialDriver _owningDriver;

    private readonly static int[] standardBaudRates = {
            75, 150, 300, 600, 1200, 1800, 2400, 3600, 4800, 7200, 9600, 14400, 19200,
            28800, 38400, 57600, 115200, 128000, 134400, 161280, 201600, 230400, 268800,
            403200, 460800, 614400, 806400, 921600, 1228800, 2457600, 3000000, 6000000
    };
    
    private enum DeviceType { DEVICE_TYPE_01, DEVICE_TYPE_T, DEVICE_TYPE_HX, DEVICE_TYPE_HXN }

    private const int USB_READ_TIMEOUT_MILLIS = 1000;
    private const int USB_WRITE_TIMEOUT_MILLIS = 5000;

    private const int USB_RECIP_INTERFACE = 0x01;

    private const int VENDOR_READ_REQUEST = 0x01;
    private const int VENDOR_WRITE_REQUEST = 0x01;
    private const int VENDOR_READ_HXN_REQUEST = 0x81;
    private const int VENDOR_WRITE_HXN_REQUEST = 0x80;

    private const int VENDOR_OUT_REQTYPE = (int)UsbAddressing.Out | UsbConstants.UsbTypeVendor;
    private const int VENDOR_IN_REQTYPE = (int)UsbAddressing.In | UsbConstants.UsbTypeVendor;
    private const int CTRL_OUT_REQTYPE = (int)UsbAddressing.Out | UsbConstants.UsbTypeClass | USB_RECIP_INTERFACE;

    private const int WRITE_ENDPOINT = 0x02;
    private const int READ_ENDPOINT = 0x83;
    private const int INTERRUPT_ENDPOINT = 0x81;

    private const int RESET_HXN_REQUEST = 0x07;
    private const int FLUSH_RX_REQUEST = 0x08;
    private const int FLUSH_TX_REQUEST = 0x09;
    private const int SET_LINE_REQUEST = 0x20; // same as CDC SET_LINE_CODING
    private const int SET_CONTROL_REQUEST = 0x22; // same as CDC SET_CONTROL_LINE_STATE
    private const int SEND_BREAK_REQUEST = 0x23; // same as CDC SEND_BREAK
    private const int GET_CONTROL_HXN_REQUEST = 0x80;
    private const int GET_CONTROL_REQUEST = 0x87;
    private const int STATUS_NOTIFICATION = 0xa1; // similar to CDC SERIAL_STATE but different length

    /* RESET_HXN_REQUEST */
    private const int RESET_HXN_RX_PIPE = 1;
    private const int RESET_HXN_TX_PIPE = 2;

    /* SET_CONTROL_REQUEST */
    private const int CONTROL_DTR = 0x01;
    private const int CONTROL_RTS = 0x02;

    /* GET_CONTROL_REQUEST */
    private const int GET_CONTROL_FLAG_CD = 0x02;
    private const int GET_CONTROL_FLAG_DSR = 0x04;
    private const int GET_CONTROL_FLAG_RI = 0x01;
    private const int GET_CONTROL_FLAG_CTS = 0x08;

    /* GET_CONTROL_HXN_REQUEST */
    private const int GET_CONTROL_HXN_FLAG_CD = 0x40;
    private const int GET_CONTROL_HXN_FLAG_DSR = 0x20;
    private const int GET_CONTROL_HXN_FLAG_RI = 0x80;
    private const int GET_CONTROL_HXN_FLAG_CTS = 0x08;

    /* interrupt endpoint read */
    private const int STATUS_FLAG_CD = 0x01;
    private const int STATUS_FLAG_DSR = 0x02;
    private const int STATUS_FLAG_RI = 0x08;
    private const int STATUS_FLAG_CTS = 0x80;

    private const int STATUS_BUFFER_SIZE = 10;
    private const int STATUS_BYTE_IDX = 8;

    private DeviceType mDeviceType = DeviceType.DEVICE_TYPE_HX;
    private UsbEndpoint mInterruptEndpoint;
    private int mControlLinesValue = 0;
    private int mBaudRate = -1, mDataBits = -1, mStopBits = -1, mParity = -1;

    private int mStatus = 0;
    private volatile Thread mReadStatusThread = null;
    private readonly object mReadStatusThreadLock = new object();
    private bool mStopReadStatusThread = false;
    private Exception mReadStatusException = null;

    public ProlificSerialPort(ProlificSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
    {
        _owningDriver = owningDriver ?? throw new ArgumentNullException(nameof(owningDriver));
    }

    public override IUsbSerialDriver GetDriver()
    {
        return _owningDriver;
    }

    public override HashSet<ControlLine> GetSupportedControlLines()
    {
        throw new NotImplementedException();
    }

    public override void SetParameters(int baudRate, int dataBits, int stopBits, int parity)
    {
        throw new NotImplementedException();
    }

    protected override void CloseInt()
    {
        throw new NotImplementedException();
    }

    protected override void OpenInt()
    {
        throw new NotImplementedException();
    }
}
