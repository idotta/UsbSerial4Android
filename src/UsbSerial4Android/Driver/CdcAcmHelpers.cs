using Android.Hardware.Usb;

namespace UsbSerial4Android.Driver;

internal static class CdcAcmHelpers
{
    public const int USB_SUBCLASS_ACM = 2;

    public static int CountAcmPorts(this UsbDevice device)
    {
        int controlInterfaceCount = 0;
        int dataInterfaceCount = 0;
        for (int i = 0; i < device.InterfaceCount; i++)
        {
            if (device.GetInterface(i).InterfaceClass == UsbClass.Comm && device.GetInterface(i).InterfaceSubclass == (UsbClass)USB_SUBCLASS_ACM)
            {
                controlInterfaceCount++;
            }
            if (device.GetInterface(i).InterfaceClass == UsbClass.CdcData)
                dataInterfaceCount++;
        }
        return Math.Min(controlInterfaceCount, dataInterfaceCount);
    }
}
