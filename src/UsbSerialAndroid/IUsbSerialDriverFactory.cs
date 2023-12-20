using Android.Hardware.Usb;

namespace UsbSerialAndroid;

/// <summary>
/// Creating a factory instead of using reflection
/// </summary>
public interface IUsbSerialDriverFactory
{
    Dictionary<int, int[]> GetSupportedDevices();

    bool Probe(UsbDevice device);

    IUsbSerialDriver Create(UsbDevice device);
}