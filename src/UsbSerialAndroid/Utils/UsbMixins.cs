using Android.Hardware.Usb;

namespace UsbSerialAndroid.Utils;

public static class UsbMixins
{
    public static List<byte[]> GetDescriptors(this UsbDeviceConnection connection)
    {
        List<byte[]> descriptors = [];
        var rawDescriptors = connection.GetRawDescriptors();
        if (rawDescriptors != null)
        {
            int pos = 0;
            while (pos < rawDescriptors.Length)
            {
                int len = rawDescriptors[pos] & 0xFF;
                if (len == 0)
                    break;
                if (pos + len > rawDescriptors.Length)
                    len = rawDescriptors.Length - pos;
                byte[] descriptor = new byte[len];
                Array.Copy(rawDescriptors, pos, descriptor, 0, len);
                descriptors.Add(descriptor);
                pos += len;
            }
        }
        return descriptors;
    }

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