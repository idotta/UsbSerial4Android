using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Util;

namespace UsbSerialAndroid.Util;

public sealed class PermissionHandler
{
    private const string TAG = nameof(PermissionHandler);
    private readonly Context _context;
    private readonly IUsbSerialDriver _serialDriver;

    public PermissionHandler(Context context, IUsbSerialDriver serialDriver)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serialDriver = serialDriver ?? throw new ArgumentNullException(nameof(serialDriver));
    }

    public async Task<bool> RequestPermissionAsync(UsbManager usbManager)
    {
        if (!usbManager.HasPermission(_serialDriver.GetDevice()))
        {
            Log.Debug(TAG, "USB permission ...");
            TaskCompletionSource<bool> tcs = new();
            PermissionBroadcastReceiver permissionReceiver = new(tcs);
            PendingIntentFlags flags = Build.VERSION.SdkInt >= BuildVersionCodes.M ? PendingIntentFlags.Mutable : 0;
            string? action = _context.PackageName;
            if (action != null)
            {
                action += ".USB_PERMISSION";
            }
            PendingIntent? permissionIntent = PendingIntent.GetBroadcast(_context, 0, new Intent(action), flags);
            IntentFilter filter = new(action);
            _context.RegisterReceiver(permissionReceiver, filter);
            usbManager.RequestPermission(_serialDriver.GetDevice(), permissionIntent);
            bool granted = await tcs.Task;
            Log.Debug(TAG, "USB permission " + granted);
            _context.UnregisterReceiver(permissionReceiver);
            return granted;
        }
        return true;
    }

    private sealed class PermissionBroadcastReceiver(TaskCompletionSource<bool> tcs) : BroadcastReceiver
    {
        private readonly TaskCompletionSource<bool> _tcs = tcs;

        public override void OnReceive(Context? context, Intent? intent)
        {
            _tcs.TrySetResult(intent?.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false) ?? false);
        }
    };
}