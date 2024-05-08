using Java.IO;

namespace UsbSerialAndroid.Exceptions;

public class SerialTimeoutException(string s) : InterruptedIOException(s)
{
}