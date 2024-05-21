using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

public sealed class ProlificSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new ProlificSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        return new Dictionary<int, int[]>
        {
            {UsbId.VENDOR_PROLIFIC, [
                UsbId.PROLIFIC_PL2303,
                UsbId.PROLIFIC_PL2303GC,
                UsbId.PROLIFIC_PL2303GB,
                UsbId.PROLIFIC_PL2303GT,
                UsbId.PROLIFIC_PL2303GL,
                UsbId.PROLIFIC_PL2303GE,
                UsbId.PROLIFIC_PL2303GS,
            ]}
        };
    }

    public bool Probe(UsbDevice device)
    {
        return false;
    }
}
