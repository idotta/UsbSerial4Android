using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

public sealed class Ch34XSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new CdcAcmSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        return new Dictionary<int, int[]>
        {
            {UsbId.VENDOR_QINHENG, [UsbId.QINHENG_CH340, UsbId.QINHENG_CH341A]}
        };
    }

    public bool Probe(UsbDevice device)
    {
        return false;
    }
}
