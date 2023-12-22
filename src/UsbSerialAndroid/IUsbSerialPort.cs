/* Copyright 2011-2013 Google Inc.
 * Copyright 2013 mike wakerly <opensource@hoho.com>
 *
 * Project home page: https://github.com/mik3y/usb-serial-for-android
 */

using Android.Hardware.Usb;

namespace UsbSerialAndroid;

public enum DataBits
{
    _5 = 5,
    _6 = 6,
    _7 = 7,
    _8 = 8
}

public enum Parity
{
    None,
    Odd,
    Even,
    Mark,
    Space
}

public enum StopBits
{
    _1 = 1,
    _1_5 = 3,
    _2 = 2
}

public enum ControlLine
{
    RTS,
    CTS,
    DTR,
    DSR,
    CD,
    RI
}

/// <summary>
/// Interface for a single serial port.
/// </summary>
public interface IUsbSerialPort : IDisposable
{
    /// <summary>
    /// Returns the driver used by this port.
    /// </summary>
    /// <returns></returns>
    IUsbSerialDriver GetDriver();

    /// <summary>
    /// Returns the currently-bound USB device.
    /// </summary>
    /// <returns></returns>
    UsbDevice GetDevice();

    /// <summary>
    /// Port number within driver.
    /// </summary>
    /// <returns></returns>
    int GetPortNumber();

    /// <summary>
    /// Returns the write endpoint.
    /// </summary>
    /// <returns></returns>
    UsbEndpoint? GetWriteEndpoint();

    /// <summary>
    /// Returns the read endpoint.
    /// </summary>
    /// <returns></returns>
    UsbEndpoint? GetReadEndpoint();

    /// <summary>
    /// The serial number of the underlying UsbDeviceConnection, or {@code null}.
    /// </summary>
    /// <returns>value from {@link UsbDeviceConnection#getSerial()}</returns>
    /// /// <exception cref="SecurityException">SecurityException starting with target SDK 29 (Android 10) if permission for USB device is not granted</exception>
    string GetSerial();

    /// <summary>
    /// Opens and initializes the port. Upon success, caller must ensure that
    /// {@link #close()} is eventually called.
    /// </summary>
    /// <param name="connection">an open device connection, acquired with {@link UsbManager#openDevice(android.hardware.usb.UsbDevice)}</param>
    /// <exception cref="IOException">on error opening or initializing the port.</exception>
    void Open(UsbDeviceConnection connection);

    /// <summary>
    /// Closes the port and {@link UsbDeviceConnection}
    /// </summary>
    /// <exception cref="IOException">on error closing the port.</exception>
    void Close();

    /// <summary>
    /// Reads as many bytes as possible into the destination buffer.
    /// </summary>
    /// <param name="dest">the destination byte buffer</param>
    /// <param name="timeout">the timeout for reading in milliseconds, 0 is infinite</param>
    /// <returns>the actual number of bytes read</returns>
    /// <exception cref="IOException">if an error occurred during reading</exception>
    int Read(byte[] dest, int timeout);

    /// <summary>
    /// Reads bytes with specified length into the destination buffer.
    /// </summary>
    /// <param name="dest">the destination byte buffer</param>
    /// <param name="length">the maximum length of the data to read</param>
    /// <param name="timeout">the timeout for reading in milliseconds, 0 is infinite</param>
    /// <returns>the actual number of bytes read</returns>
    /// <exception cref="IOException">if an error occurred during reading</exception>
    int Read(byte[] dest, int length, int timeout);

    /// <summary>
    /// Writes as many bytes as possible from the source buffer.
    /// </summary>
    /// <param name="src">the source byte buffer</param>
    /// <param name="timeout">the timeout for writing in milliseconds, 0 is infinite</param>
    /// <exception cref="SerialTimeoutException">if timeout reached before sending all data. ex.bytesTransferred may contain bytes transferred</exception>
    /// <exception cref="IOException">if an error occurred during writing</exception>
    void Write(byte[] src, int timeout);

    /// <summary>
    /// Writes bytes with specified length from the source buffer.
    /// </summary>
    /// <param name="src">the source byte buffer</param>
    /// <param name="length">the length of the data to write</param>
    /// <param name="timeout">the timeout for writing in milliseconds, 0 is infinite</param>
    /// <exception cref="IOException">if an error occurred during writing</exception>
    void Write(byte[] src, int length, int timeout);

    /// <summary>
    /// Sets various serial port parameters.
    /// </summary>
    /// <param name="baudRate">baud rate as an integer, for example {@code 115200}.</param>
    /// <param name="dataBits">one of {@link #DATABITS_5}, {@link #DATABITS_6}, {@link #DATABITS_7}, or {@link #DATABITS_8}.</param>
    /// <param name="stopBits">one of {@link #STOPBITS_1}, {@link #STOPBITS_1_5}, or {@link #STOPBITS_2}.</param>
    /// <param name="parity">one of {@link #PARITY_NONE}, {@link #PARITY_ODD}, {@link #PARITY_EVEN}, {@link #PARITY_MARK}, or {@link #PARITY_SPACE}.</param>
    /// <exception cref="IOException">on error setting the port parameters</exception>
    /// <exception cref="UnsupportedOperationException">if values are not supported by a specific device</exception>
    void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity);

    /**
     * Gets the CD (Carrier Detect) bit from the underlying UART.
     *
     * @return the current state
     * @throws IOException if an error occurred during reading
     * @throws UnsupportedOperationException if not supported
     */

    bool GetCD();

    /**
     * Gets the CTS (Clear To Send) bit from the underlying UART.
     *
     * @return the current state
     * @throws IOException if an error occurred during reading
     * @throws UnsupportedOperationException if not supported
     */

    bool GetCTS();

    /**
     * Gets the DSR (Data Set Ready) bit from the underlying UART.
     *
     * @return the current state
     * @throws IOException if an error occurred during reading
     * @throws UnsupportedOperationException if not supported
     */

    bool GetDSR();

    /**
     * Gets the DTR (Data Terminal Ready) bit from the underlying UART.
     *
     * @return the current state
     * @throws IOException if an error occurred during reading
     * @throws UnsupportedOperationException if not supported
     */

    bool GetDTR();

    /**
     * Sets the DTR (Data Terminal Ready) bit on the underlying UART, if supported.
     *
     * @param value the value to set
     * @throws IOException if an error occurred during writing
     * @throws UnsupportedOperationException if not supported
     */

    void SetDTR(bool value);

    /**
     * Gets the RI (Ring Indicator) bit from the underlying UART.
     *
     * @return the current state
     * @throws IOException if an error occurred during reading
     * @throws UnsupportedOperationException if not supported
     */

    bool GetRI();

    /**
     * Gets the RTS (Request To Send) bit from the underlying UART.
     *
     * @return the current state
     * @throws IOException if an error occurred during reading
     * @throws UnsupportedOperationException if not supported
     */

    bool GetRTS();

    /**
     * Sets the RTS (Request To Send) bit on the underlying UART, if supported.
     *
     * @param value the value to set
     * @throws IOException if an error occurred during writing
     * @throws UnsupportedOperationException if not supported
     */

    void SetRTS(bool value);

    /**
     * Gets all control line values from the underlying UART, if supported.
     * Requires less USB calls than calling getRTS() + ... + getRI() individually.
     *
     * @return EnumSet.contains(...) is {@code true} if set, else {@code false}
     * @throws IOException if an error occurred during reading
     */

    HashSet<ControlLine> GetControlLines();

    /**
     * Gets all control line supported flags.
     *
     * @return EnumSet.contains(...) is {@code true} if supported, else {@code false}
     * @throws IOException if an error occurred during reading
     */

    HashSet<ControlLine> GetSupportedControlLines();

    /**
     * Purge non-transmitted output data and / or non-read input data.
     *
     * @param purgeWriteBuffers {@code true} to discard non-transmitted output data
     * @param purgeReadBuffers {@code true} to discard non-read input data
     * @throws IOException if an error occurred during flush
     * @throws UnsupportedOperationException if not supported
     */

    void PurgeHwBuffers(bool purgeWriteBuffers, bool purgeReadBuffers);

    /**
     * send BREAK condition.
     *
     * @param value set/reset
     */

    void SetBreak(bool value);

    /**
     * Returns the current state of the connection.
     */

    bool IsOpen();
}