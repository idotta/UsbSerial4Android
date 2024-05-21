using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

public sealed class GsmModemSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new GsmModemSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        return new Dictionary<int, int[]>
        {
            {UsbId.VENDOR_UNISOC, [
                UsbId.FIBOCOM_L610,
                UsbId.FIBOCOM_L612
            ]}
        };
    }

    public bool Probe(UsbDevice device)
    {
        return false;
    }
}
