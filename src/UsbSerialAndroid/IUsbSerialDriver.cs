using Android.Hardware.Usb;

namespace UsbSerialAndroid;

/* Copyright 2011-2013 Google Inc.
 * Copyright 2013 mike wakerly <opensource@hoho.com>
 *
 * Project home page: https://github.com/mik3y/usb-serial-for-android
 */

/// <summary>
///
/// </summary>
public interface IUsbSerialDriver
{
    /// <summary>
    /// Returns the raw UsbDevice backing this port.
    /// </summary>
    /// <returns>The raw UsbDevice</returns>
    UsbDevice GetDevice();

    /// <summary>
    /// Returns all available ports for this device. This list must have at least
    /// one entry.
    /// </summary>
    /// <returns>The ports</returns>
    List<IUsbSerialPort> GetPorts();
}