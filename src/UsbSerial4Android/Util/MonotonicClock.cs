using System.Diagnostics;

namespace UsbSerial4Android.Util;

public static class MonotonicClock
{
    public static long Millis()
    {
        return Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}