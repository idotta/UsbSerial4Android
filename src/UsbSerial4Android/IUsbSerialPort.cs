using Android.Hardware.Usb;

namespace UsbSerial4Android;

public interface IUsbSerialPort
{
    /// <summary>
    /// Returns the driver used by this port.
    /// </summary>
    IUsbSerialDriver GetDriver();

    /// <summary>
    /// Returns the currently-bound USB device.
    /// </summary>
    UsbDevice GetDevice();

    /// <summary>
    /// Port number within driver.
    /// </summary>
    int GetPortNumber();

    /// <summary>
    /// Returns the write endpoint.
    /// </summary>
    UsbEndpoint? GetWriteEndpoint();

    /// <summary>
    /// Returns the read endpoint.
    /// </summary>
    UsbEndpoint? GetReadEndpoint();

    /// <summary>
    /// The serial number of the underlying UsbDeviceConnection, or null.
    /// </summary>
    /// <returns>Value from UsbDeviceConnection.getSerial()</returns>
    /// <exception cref="System.Security.SecurityException">Starting with target SDK 29 (Android 10) if permission for USB device is not granted</exception>
    string GetSerial();

    /// <summary>
    /// Opens and initializes the port. Upon success, caller must ensure that close() is eventually called.
    /// </summary>
    /// <param name="connection">An open device connection, acquired with UsbManager.openDevice(UsbDevice)</param>
    /// <exception cref="System.IO.IOException">On error opening or initializing the port.</exception>
    void Open(UsbDeviceConnection connection);

    /// <summary>
    /// Closes the port and UsbDeviceConnection.
    /// </summary>
    /// <exception cref="System.IO.IOException">On error closing the port.</exception>
    void Close();

    /// <summary>
    /// Reads as many bytes as possible into the destination buffer.
    /// </summary>
    /// <param name="dest">The destination byte buffer</param>
    /// <param name="timeout">The timeout for reading in milliseconds, 0 is infinite</param>
    /// <returns>The actual number of bytes read</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    int Read(byte[] dest, int timeout);

    /// <summary>
    /// Reads bytes with specified length into the destination buffer.
    /// </summary>
    /// <param name="dest">The destination byte buffer</param>
    /// <param name="length">The maximum length of the data to read</param>
    /// <param name="timeout">The timeout for reading in milliseconds, 0 is infinite</param>
    /// <returns>The actual number of bytes read</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    int Read(byte[] dest, int length, int timeout);

    /// <summary>
    /// Writes as many bytes as possible from the source buffer.
    /// </summary>
    /// <param name="src">The source byte buffer</param>
    /// <param name="timeout">The timeout for writing in milliseconds, 0 is infinite</param>
    /// <exception cref="Exceptions.SerialTimeoutException">If timeout reached before sending all data. ex.bytesTransferred may contain bytes transferred</exception>
    /// <exception cref="System.IO.IOException">If an error occurred during writing</exception>
    void Write(byte[] src, int timeout);

    /// <summary>
    /// Writes bytes with specified length from the source buffer.
    /// </summary>
    /// <param name="src">The source byte buffer</param>
    /// <param name="length">The length of the data to write</param>
    /// <param name="timeout">The timeout for writing in milliseconds, 0 is infinite</param>
    /// <exception cref="Exceptions.SerialTimeoutException">If timeout reached before sending all data. ex.bytesTransferred may contain bytes transferred</exception>
    /// <exception cref="System.IO.IOException">If an error occurred during writing</exception>
    void Write(byte[] src, int offset, int length, int timeout);

    /// <summary>
    /// Sets various serial port parameters.
    /// </summary>
    /// <param name="baudRate">Baud rate as an integer, for example 115200</param>
    /// <param name="dataBits">One of DATABITS_5, DATABITS_6, DATABITS_7, or DATABITS_8</param>
    /// <param name="stopBits">One of STOPBITS_1, STOPBITS_1_5, or STOPBITS_2</param>
    /// <param name="parity">One of PARITY_NONE, PARITY_ODD, PARITY_EVEN, PARITY_MARK, or PARITY_SPACE</param>
    /// <exception cref="System.IO.IOException">On error setting the port parameters</exception>
    /// <exception cref="System.NotSupportedException">If values are not supported by a specific device</exception>
    void SetParameters(int baudRate, int dataBits, int stopBits, int parity);

    /// <summary>
    /// Gets the CD (Carrier Detect) bit from the underlying UART.
    /// </summary>
    /// <returns>The current state</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    bool GetCD();

    /// <summary>
    /// Gets the CTS (Clear To Send) bit from the underlying UART.
    /// </summary>
    /// <returns>The current state</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    bool GetCTS();

    /// <summary>
    /// Gets the DSR (Data Set Ready) bit from the underlying UART.
    /// </summary>
    /// <returns>The current state</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    bool GetDSR();

    /// <summary>
    /// Gets the DTR (Data Terminal Ready) bit from the underlying UART.
    /// </summary>
    /// <returns>The current state</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    bool GetDTR();

    /// <summary>
    /// Sets the DTR (Data Terminal Ready) bit on the underlying UART, if supported.
    /// </summary>
    /// <param name="value">The value to set</param>
    /// <exception cref="System.IO.IOException">If an error occurred during writing</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    void SetDTR(bool value);

    /// <summary>
    /// Gets the RI (Ring Indicator) bit from the underlying UART.
    /// </summary>
    /// <returns>The current state</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    bool GetRI();

    /// <summary>
    /// Gets the RTS (Request To Send) bit from the underlying UART.
    /// </summary>
    /// <returns>The current state</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    bool GetRTS();

    /// <summary>
    /// Sets the RTS (Request To Send) bit on the underlying UART, if supported.
    /// </summary>
    /// <param name="value">The value to set</param>
    /// <exception cref="System.IO.IOException">If an error occurred during writing</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    void SetRTS(bool value);

    /// <summary>
    /// Gets all control line values from the underlying UART, if supported.
    /// </summary>
    /// <returns>EnumSet.contains(...) is true if set, else false</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    HashSet<ControlLine> GetControlLines();

    /// <summary>
    /// Gets all control line supported flags.
    /// </summary>
    /// <returns>EnumSet.contains(...) is true if supported, else false</returns>
    /// <exception cref="System.IO.IOException">If an error occurred during reading</exception>
    HashSet<ControlLine> GetSupportedControlLines();

    /// <summary>
    /// Purge non-transmitted output data and / or non-read input data.
    /// </summary>
    /// <param name="purgeWriteBuffers">True to discard non-transmitted output data</param>
    /// <param name="purgeReadBuffers">True to discard non-read input data</param>
    /// <exception cref="System.IO.IOException">If an error occurred during flush</exception>
    /// <exception cref="System.NotSupportedException">If not supported</exception>
    void PurgeHwBuffers(bool purgeWriteBuffers, bool purgeReadBuffers);

    /// <summary>
    /// Send BREAK condition.
    /// </summary>
    /// <param name="value">Set/reset</param>
    void SetBreak(bool value);

    /// <summary>
    /// Returns the current state of the connection.
    /// </summary>
    /// <returns>True if the connection is open, otherwise false</returns>
    bool IsOpen();
}