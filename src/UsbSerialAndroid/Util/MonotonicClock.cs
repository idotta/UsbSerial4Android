using System.Diagnostics;

namespace UsbSerialAndroid.Util;

public static class MonotonicClock
{
    public static long Millis()
    {
        return Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}