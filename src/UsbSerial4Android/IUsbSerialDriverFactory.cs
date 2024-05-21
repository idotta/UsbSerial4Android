using Android.Hardware.Usb;

namespace UsbSerial4Android;

/// <summary>
/// Creating a factory instead of using reflection
/// </summary>
public interface IUsbSerialDriverFactory
{
    Dictionary<int, int[]> GetSupportedDevices();

    bool Probe(UsbDevice device);

    IUsbSerialDriver Create(UsbDevice device);
}