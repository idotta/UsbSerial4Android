using Android.Hardware.Usb;
using Android.Util;

namespace UsbSerial4Android.Driver;

internal sealed class GsmModemSerialPort : CommonUsbSerialPort
{
    private const string TAG = nameof(GsmModemSerialPort);

    private readonly GsmModemSerialDriver _owningDriver;

    private UsbInterface? _dataInterface;

    public GsmModemSerialPort(GsmModemSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
    {
        _owningDriver = owningDriver ?? throw new ArgumentNullException(nameof(owningDriver));
    }

    public override IUsbSerialDriver GetDriver() => _owningDriver;

    protected override void OpenInt()
    {
        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }
        Log.Debug(TAG, "claiming interfaces, count=" + _device.InterfaceCount);
        _dataInterface = _device.GetInterface(0);
        if (!_connection.ClaimInterface(_dataInterface, true))
        {
            throw new IOException("Could not claim shared control/data interface");
        }
        Log.Debug(TAG, "endpoint count=" + _dataInterface.EndpointCount);
        for (int i = 0; i < _dataInterface.EndpointCount; ++i)
        {
            UsbEndpoint ep = _dataInterface.GetEndpoint(i) ?? throw new IOException("No data endpoint"); ;
            if ((ep.Direction == UsbAddressing.In) && (ep.Type == UsbAddressing.XferBulk))
            {
                _readEndpoint = ep;
            }
            else if ((ep.Direction == UsbAddressing.Out) && (ep.Type == UsbAddressing.XferBulk))
            {
                _writeEndpoint = ep;
            }
        }
        InitGsmModem();
    }

    protected override void CloseInt()
    {
        try
        {
            _connection?.ReleaseInterface(_dataInterface);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    private int InitGsmModem()
    {
        if (_connection == null)
        {
            throw new IOException("Connection closed");
        }
        int len = _connection.ControlTransfer((UsbAddressing)0x21, 0x22, 0x01, 0, null, 0, 5000);
        if (len < 0)
        {
            throw new IOException("init failed");
        }
        return len;
    }

    public override void SetParameters(int baudRate, int dataBits, int stopBits, int parity)
    {
        throw new NotSupportedException();
    }

    public override HashSet<ControlLine> GetSupportedControlLines()
    {
        return [];
    }
}