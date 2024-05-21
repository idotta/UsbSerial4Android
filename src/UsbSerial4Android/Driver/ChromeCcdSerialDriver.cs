using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class ChromeCcdSerialDriver : IUsbSerialDriver
{
    private readonly UsbDevice _device;
    private readonly List<IUsbSerialPort> _ports = [];

    public ChromeCcdSerialDriver(UsbDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        for (int i = 0; i < 3; i++)
            _ports.Add(new ChromeCcdSerialPort(this, _device, i));
    }

    public UsbDevice GetDevice() => _device;

    public List<IUsbSerialPort> GetPorts() => _ports;
}