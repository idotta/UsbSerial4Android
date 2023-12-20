using System.Diagnostics;

namespace UsbSerialAndroid.Utils;

public static class MonotonicClock
{
    public static long Millis()
    {
        return Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}