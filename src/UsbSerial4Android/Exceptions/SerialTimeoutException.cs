using Java.IO;

namespace UsbSerial4Android.Exceptions;

public class SerialTimeoutException(string s) : InterruptedIOException(s)
{
}