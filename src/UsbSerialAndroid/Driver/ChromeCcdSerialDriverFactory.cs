using Android.Hardware.Usb;

namespace UsbSerialAndroid.Driver;

internal sealed class ChromeCcdSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new ChromeCcdSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        return new Dictionary<int, int[]>
        {
            {UsbId.VENDOR_GOOGLE, new[] {UsbId.GOOGLE_CR50}}
        };
    }

    public bool Probe(UsbDevice device)
    {
        return false;
    }
}
