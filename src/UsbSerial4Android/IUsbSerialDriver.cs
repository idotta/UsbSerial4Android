using Android.Hardware.Usb;

namespace UsbSerial4Android;

public interface IUsbSerialDriver
{
    UsbDevice GetDevice();
    List<IUsbSerialPort> GetPorts();
}
