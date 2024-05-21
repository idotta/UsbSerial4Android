using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class FtdiSerialDriver : IUsbSerialDriver
{
    private readonly UsbDevice _device;
    private readonly List<IUsbSerialPort> _ports = [];

    public FtdiSerialDriver(UsbDevice device)
    {
        _device = device;
        for (int port = 0; port < device.InterfaceCount; port++)
        {
            _ports.Add(new FtdiSerialPort(this, _device, port));
        }
    }

    public UsbDevice GetDevice() => _device;

    public List<IUsbSerialPort> GetPorts() => _ports;
}
