using Android.Hardware.Usb;
using Java.Lang;
using Java.Lang.Reflect;

namespace UsbSerial4Android;

/// <summary>
/// Maps (vendor id, product id) pairs to the corresponding serial driver,
/// or invoke 'probe' method to check actual USB devices for matching interfaces.
/// </summary>
public class ProbeTable
{
    private readonly Dictionary<(int vendorId, int productId), IUsbSerialDriverFactory> _vidPidProbeTable = [];
    private readonly List<IUsbSerialDriverFactory> _driverFactories = [];

    /// <summary>
    /// Adds or updates a (vendor, product) pair in the table.
    /// </summary>
    /// <param name="vendorId">the USB vendor id</param>
    /// <param name="productId">the USB product id</param>
    /// <param name="driverFactory">the driver class responsible for this pair</param>
    /// <returns>this, for chaining</returns>
    public ProbeTable AddProduct(int vendorId, int productId, IUsbSerialDriverFactory driverFactory)
    {
        _vidPidProbeTable[(vendorId, productId)] = driverFactory;
        return this;
    }

    /// <summary>
    /// Internal method to add all supported products from GetSupportedDevices
    /// </summary>
    /// <param name="driverFactory"></param>
    /// <exception cref="RuntimeException"></exception>
    public void AddDriver(IUsbSerialDriverFactory driverFactory)
    {
        Dictionary<int, int[]> devices = [];
        try
        {
            devices = driverFactory.GetSupportedDevices();
        }
        catch (Java.Lang.Exception e) when (e is IllegalArgumentException || e is IllegalAccessException || e is InvocationTargetException)
        {
            throw new RuntimeException(e);
        }

        foreach (var device in devices)
        {
            int vendorId = device.Key;
            foreach (int productId in device.Value)
            {
                AddProduct(vendorId, productId, driverFactory);
            }
        }
        _driverFactories.Add(driverFactory);
    }

    /// <summary>
    /// Returns the driver for the given USB device, or {@code null} if no match.
    /// </summary>
    /// <param name="usbDevice">the USB device to be probed</param>
    /// <returns>the driver matching this pair, or {@code null}</returns>
    /// <exception cref="RuntimeException"></exception>
    public IUsbSerialDriver? FindDriver(in UsbDevice usbDevice)
    {
        if (_vidPidProbeTable.TryGetValue((usbDevice.VendorId, usbDevice.ProductId), out var driverFactory))
        {
            return driverFactory.Create(usbDevice);
        }
        foreach (var factory in _driverFactories)
        {
            try
            {
                if (factory.Probe(usbDevice))
                {
                    return factory.Create(usbDevice);
                }
            }
            catch (Java.Lang.Exception e) when (e is IllegalArgumentException || e is IllegalAccessException || e is InvocationTargetException)
            {
                throw new RuntimeException(e);
            }
        }
        return null;
    }
}