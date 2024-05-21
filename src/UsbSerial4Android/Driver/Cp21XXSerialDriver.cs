using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class Cp21XXSerialDriver : IUsbSerialDriver
{
    private readonly UsbDevice _device;
    private readonly List<IUsbSerialPort> _ports = [];

    public Cp21XXSerialDriver(UsbDevice device)
    {
        _device = device;
        for (int port = 0; port < device.InterfaceCount; port++)
        {
            _ports.Add(new Cp21XXSerialPort(this, _device, port));
        }
    }

    public UsbDevice GetDevice() => _device;

    public List<IUsbSerialPort> GetPorts() => _ports;
}