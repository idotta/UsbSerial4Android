using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class CdcAcmSerialDriver : IUsbSerialDriver
{
    public const int USB_SUBCLASS_ACM = 2;

    private readonly UsbDevice _device;
    private readonly List<IUsbSerialPort> _ports = [];

    public CdcAcmSerialDriver(UsbDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        int ports = device.CountAcmPorts();
        for (int port = 0; port < ports; port++)
        {
            _ports.Add(new CdcAcmSerialPort(this, _device, port));
        }
        if (_ports.Count == 0)
        {
            _ports.Add(new CdcAcmSerialPort(this, _device, -1));
        }
    }

    public UsbDevice GetDevice() => _device;

    public List<IUsbSerialPort> GetPorts() => _ports;
}