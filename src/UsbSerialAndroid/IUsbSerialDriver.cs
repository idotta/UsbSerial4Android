using Android.Hardware.Usb;

namespace UsbSerialAndroid;

public interface IUsbSerialDriver
{
    UsbDevice GetDevice();
    List<IUsbSerialPort> GetPorts();
}
