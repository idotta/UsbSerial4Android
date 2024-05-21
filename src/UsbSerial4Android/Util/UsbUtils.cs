using Android.Hardware.Usb;

namespace UsbSerial4Android.Util;

public static class UsbUtils
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
}