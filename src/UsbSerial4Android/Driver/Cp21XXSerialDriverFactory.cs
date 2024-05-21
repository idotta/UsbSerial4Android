using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

public sealed class Cp21XXSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new Cp21XXSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        return new Dictionary<int, int[]>
        {
            {UsbId.VENDOR_SILABS, [
                UsbId.SILABS_CP2102, // same ID for CP2101, CP2103, CP2104, CP2109
            UsbId.SILABS_CP2105,
            UsbId.SILABS_CP2108,
            ]}
        };
    }

    public bool Probe(UsbDevice device)
    {
        return false;
    }
}