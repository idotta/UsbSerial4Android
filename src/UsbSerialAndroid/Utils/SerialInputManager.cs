using Android.Util;

namespace UsbSerialAndroid.Utils;

/// <summary>
/// Utility class which services a {@link UsbSerialPort} in its {@link #run()} method.
/// </summary>
public class SerialInputManager : IDisposable
{
    public enum State
    {
        Stopped,
        Running,
        Stopping
    }

    private const string TAG = nameof(SerialInputManager);
    private readonly IUsbSerialPort _serialPort;
    private Thread? _thread;
    private bool _disposedValue;
    private IObserver<byte[]>? _dataObserver;

    public SerialInputManager(IUsbSerialPort serialPort)
    {
        _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        ReadBufferSize = _serialPort.GetReadEndpoint()?.MaxPacketSize ?? 4096;
    }

    public State CurrState { get; private set; } = State.Stopped;

    /// <summary>
    /// By default a higher priority than UI thread is used to prevent data loss
    /// </summary>
    public ThreadPriority ThreadPriority { get; init; } = ThreadPriority.Highest;

    /// <summary>
    /// Default read timeout is infinite, to avoid data loss with bulkTransfer API
    /// </summary>
    public int ReadTimeout { get; init; }

    /// <summary>
    /// Default value is GetReadEndPoint().MaxPacketSize
    /// </summary>
    public int ReadBufferSize { get; init; }

    /// <summary>
    /// Start SerialInputManager in separate thread
    /// </summary>
    /// <exception cref="Java.Lang.IllegalStateException"></exception>
    public void Start(IObserver<byte[]> dataObserver)
    {
        if (CurrState != State.Stopped)
        {
            throw new Java.Lang.IllegalStateException("Already started");
        }
        if (_thread != null && _thread.IsAlive)
        {
            _thread.Join();
        }
        _dataObserver = dataObserver;
        _thread = new(new ThreadStart(Run))
        {
            Priority = ThreadPriority
        };
        _thread.Start();
    }

    /// <summary>
    /// Stop SerialInputManager thread
    /// </summary>
    public void Stop()
    {
        if (CurrState == State.Running)
        {
            Log.Info(TAG, "Stop requested");
            CurrState = State.Stopping;

            // when using readTimeout == 0 (default), additionally use usbSerialPort.close() to
            // interrupt blocking read
            if (ReadTimeout == 0)
            {
                _serialPort.Close();
            }
        }
    }

    /// <summary>
    /// Continuously services the read buffer until {@link #stop()} is called,
    /// or until a driver exception is raised.
    /// </summary>
    /// <exception cref="Java.Lang.IllegalStateException"></exception>
    private void Run()
    {
        if (CurrState != State.Stopped)
        {
            throw new Java.Lang.IllegalStateException("Already running");
        }
        CurrState = State.Running;
        Log.Info(TAG, "Running ...");
        try
        {
            byte[] buffer = new byte[ReadBufferSize];
            while (CurrState == State.Running)
            {
                int len = _serialPort.Read(buffer, ReadTimeout);
                if (len > 0)
                {
#if (DEBUG)
                    //Log.Debug(TAG, "Read data len=" + len);
#endif
                    // TODO: This part can cause overhead and might need optimization
                    if (_dataObserver != null)
                    {
                        byte[] data = new byte[len];
                        Array.Copy(buffer, 0, data, 0, len);
                        _dataObserver.OnNext(data);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Warn(TAG, "Run ending due to exception: " + e.Message, e);
            _dataObserver?.OnError(e);
        }
        finally
        {
            CurrState = State.Stopped;
            Log.Info(TAG, "Stopped");
            _dataObserver?.OnCompleted();
            _dataObserver = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            if (_thread != null && _thread.IsAlive)
            {
                Stop();
                _thread.Join();
            }
            _thread = null;
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~SerialInputManager()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}