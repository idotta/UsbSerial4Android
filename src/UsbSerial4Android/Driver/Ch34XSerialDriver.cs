using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal class Ch34XSerialDriver : IUsbSerialDriver
{
    private readonly UsbDevice _device;
    private readonly IUsbSerialPort _port;

    public Ch34XSerialDriver(UsbDevice device)
    {
        _device = device;
        _port = new Ch340SerialPort(this, _device, 0);
    }

    public UsbDevice GetDevice() => _device;

    public List<IUsbSerialPort> GetPorts() => [_port];
}