using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class ProlificSerialDriver : IUsbSerialDriver
{
    private readonly UsbDevice _device;
    private readonly IUsbSerialPort _port;

    public ProlificSerialDriver(UsbDevice device)
    {
        _device = device;
        _port = new ProlificSerialPort(this, device, 0);
    }

    public UsbDevice GetDevice() => _device;

    public List<IUsbSerialPort> GetPorts() => [_port];
}