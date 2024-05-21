using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal sealed class GsmModemSerialDriver : IUsbSerialDriver
{
    private readonly UsbDevice _device;
    private readonly IUsbSerialPort _port;

    public GsmModemSerialDriver(UsbDevice device)
    {
        _device = device;
        _port = new GsmModemSerialPort(this, device, 0);
    }

    public UsbDevice GetDevice() => _device;

    public List<IUsbSerialPort> GetPorts() => [_port];
}
