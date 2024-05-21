using UsbSerial4Android.Driver;

namespace UsbSerial4Android;

public class UsbSerialProber
{
    private readonly ProbeTable _probeTable;

    public UsbSerialProber(ProbeTable probeTable)
    {
        _probeTable = probeTable;
    }

    public static UsbSerialProber GetDefaultProber()
    {
        return new UsbSerialProber(GetDefaultProbeTable());
    }

    public static ProbeTable GetDefaultProbeTable()
    {
        ProbeTable probeTable = new();

        probeTable.AddDriver(new CdcAcmSerialDriverFactory());
        probeTable.AddDriver(new Ch34XSerialDriverFactory());
        probeTable.AddDriver(new ChromeCcdSerialDriverFactory());
        probeTable.AddDriver(new Cp21XXSerialDriverFactory());
        probeTable.AddDriver(new FtdiSerialDriverFactory());
        probeTable.AddDriver(new GsmModemSerialDriverFactory());
        probeTable.AddDriver(new ProlificSerialDriverFactory());

        return probeTable;
    }

    /// <summary>
    /// Finds and builds all possible UsbSerialDriver UsbSerialDrivers
    /// from the currently-attached UsbDevice hierarchy. This method does
    /// not require permission from the Android USB system, since it does not
    /// open any of the devices.
    /// </summary>
    /// <param name="usbManager"></param>
    /// <returns>a list, possibly empty, of all compatible drivers</returns>
    public List<IUsbSerialDriver> FindAllDrivers(in Android.Hardware.Usb.UsbManager usbManager)
    {
        return usbManager.DeviceList?
            .Select(device => ProbeDevice(device.Value))
            .Where(device => device != null)
            .Select(device => device!)
            .ToList() ?? [];
    }

    /// <summary>
    /// Probes a single device for a compatible driver.
    /// </summary>
    /// <param name="usbDevice">the usb device to probe</param>
    /// <returns>a new UsbSerialDriver compatible with this device, or null if none available.</returns>
    public IUsbSerialDriver? ProbeDevice(in Android.Hardware.Usb.UsbDevice usbDevice)
    {
        return _probeTable.FindDriver(usbDevice);
    }
}