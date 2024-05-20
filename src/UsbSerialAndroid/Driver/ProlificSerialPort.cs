using Android.Hardware.Usb;

namespace UsbSerialAndroid.Driver;

internal sealed class ProlificSerialPort : CommonUsbSerialPort
{
    private const string TAG = nameof(ProlificSerialPort);

    private readonly ProlificSerialDriver _owningDriver;

    public ProlificSerialPort(ProlificSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
    {
        _owningDriver = owningDriver ?? throw new ArgumentNullException(nameof(owningDriver));
    }

    public override IUsbSerialDriver GetDriver()
    {
        throw new NotImplementedException();
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
