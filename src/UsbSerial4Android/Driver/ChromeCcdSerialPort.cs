using Android.Hardware.Usb;
using Android.Util;

namespace UsbSerial4Android.Driver;

internal sealed class ChromeCcdSerialPort : CommonUsbSerialPort
{
    private const string TAG = nameof(ChromeCcdSerialPort);

    private readonly ChromeCcdSerialDriver _owningDriver;

    private UsbInterface? _dataInterface;

    public ChromeCcdSerialPort(ChromeCcdSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
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
        Log.Debug(TAG, "claiming interfaces, count=" + _device.InterfaceCount);
        _dataInterface = _device.GetInterface(_portNumber);
        if (!_connection.ClaimInterface(_dataInterface, true))
        {
            throw new IOException("Could not claim shared control/data interface");
        }
        Log.Debug(TAG, "endpoint count=" + _dataInterface.EndpointCount);
        for (int i = 0; i < _dataInterface.EndpointCount; ++i)
        {
            UsbEndpoint ep = _dataInterface.GetEndpoint(i) ?? throw new IOException("No control endpoint");
            if ((ep.Direction == UsbAddressing.In) && (ep.Type == UsbAddressing.XferBulk))
            {
                _readEndpoint = ep;
            }
            else if ((ep.Direction == UsbAddressing.Out) && (ep.Type == UsbAddressing.XferBulk))
            {
                _writeEndpoint = ep;
            }
        }
    }

    protected override void CloseInt()
    {
        try
        {
            _connection?.ReleaseInterface(_dataInterface);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public override void SetParameters(int baudRate, int dataBits, int stopBits, int parity)
    {
        throw new Java.Lang.UnsupportedOperationException();
    }


    public override HashSet<ControlLine> GetSupportedControlLines()
    {
        return [];
    }
}