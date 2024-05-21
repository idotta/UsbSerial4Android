using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

public sealed class FtdiSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new FtdiSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        return new Dictionary<int, int[]>
        {
            {UsbId.VENDOR_FTDI, [
                UsbId.FTDI_FT232R,
                UsbId.FTDI_FT232H,
                UsbId.FTDI_FT2232H,
                UsbId.FTDI_FT4232H,
                UsbId.FTDI_FT231X,  // same ID for FT230X, FT231X, FT234XD
            ]}
        };
    }

    public bool Probe(UsbDevice device)
    {
        return false;
    }
}
