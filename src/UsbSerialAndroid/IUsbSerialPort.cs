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

/**
 * Interface for a single serial port.
 *
 * @author mike wakerly (opensource@hoho.com)
 */

public interface IUsbSerialPort : IDisposable
{
    /**
     * Returns the driver used by this port.
     */

    IUsbSerialDriver GetDriver();

    /**
     * Returns the currently-bound USB device.
     */

    UsbDevice GetDevice();

    /**
     * Port number within driver.
     */

    int GetPortNumber();

    /**
     * Returns the write endpoint.
     * @return write endpoint
     */

    UsbEndpoint? GetWriteEndpoint();

    /**
     * Returns the read endpoint.
     * @return read endpoint
     */

    UsbEndpoint? GetReadEndpoint();

    /**
     * The serial number of the underlying UsbDeviceConnection, or {@code null}.
     *
     * @return value from {@link UsbDeviceConnection#getSerial()}
     * @throws SecurityException starting with target SDK 29 (Android 10) if permission for USB device is not granted
     */

    string GetSerial();

    /**
     * Opens and initializes the port. Upon success, caller must ensure that
     * {@link #close()} is eventually called.
     *
     * @param connection an open device connection, acquired with
     *                   {@link UsbManager#openDevice(android.hardware.usb.UsbDevice)}
     * @throws IOException on error opening or initializing the port.
     */

    void Open(UsbDeviceConnection connection);

    /**
     * Closes the port and {@link UsbDeviceConnection}
     *
     * @throws IOException on error closing the port.
     */

    void Close();

    /**
     * Reads as many bytes as possible into the destination buffer.
     *
     * @param dest the destination byte buffer
     * @param timeout the timeout for reading in milliseconds, 0 is infinite
     * @return the actual number of bytes read
     * @throws IOException if an error occurred during reading
     */

    int Read(byte[] dest, int timeout);

    /**
     * Reads bytes with specified length into the destination buffer.
     *
     * @param dest the destination byte buffer
     * @param length the maximum length of the data to read
     * @param timeout the timeout for reading in milliseconds, 0 is infinite
     * @return the actual number of bytes read
     * @throws IOException if an error occurred during reading
     */

    int Read(byte[] dest, int length, int timeout);

    /**
     * Writes as many bytes as possible from the source buffer.
     *
     * @param src the source byte buffer
     * @param timeout the timeout for writing in milliseconds, 0 is infinite
     * @throws SerialTimeoutException if timeout reached before sending all data.
     *                                ex.bytesTransferred may contain bytes transferred
     * @throws IOException if an error occurred during writing
     */

    void Write(byte[] src, int timeout);

    /**
     * Writes bytes with specified length from the source buffer.
     *
     * @param src the source byte buffer
     * @param length the length of the data to write
     * @param timeout the timeout for writing in milliseconds, 0 is infinite
     * @throws SerialTimeoutException if timeout reached before sending all data.
     *                                ex.bytesTransferred may contain bytes transferred
     * @throws IOException if an error occurred during writing
     */

    void Write(byte[] src, int length, int timeout);

    /**
     * Sets various serial port parameters.
     *
     * @param baudRate baud rate as an integer, for example {@code 115200}.
     * @param dataBits one of {@link #DATABITS_5}, {@link #DATABITS_6},
     *                 {@link #DATABITS_7}, or {@link #DATABITS_8}.
     * @param stopBits one of {@link #STOPBITS_1}, {@link #STOPBITS_1_5}, or {@link #STOPBITS_2}.
     * @param parity one of {@link #PARITY_NONE}, {@link #PARITY_ODD},
     *               {@link #PARITY_EVEN}, {@link #PARITY_MARK}, or {@link #PARITY_SPACE}.
     * @throws IOException on error setting the port parameters
     * @throws UnsupportedOperationException if values are not supported by a specific device
     */

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