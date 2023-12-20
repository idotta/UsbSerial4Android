using Java.IO;

namespace UsbSerialAndroid;

public class SerialTimeoutException(string s) : InterruptedIOException(s)
{
}