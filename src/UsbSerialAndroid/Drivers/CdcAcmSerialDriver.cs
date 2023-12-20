/* Copyright 2011-2013 Google Inc.
 * Copyright 2013 mike wakerly <opensource@hoho.com>
 *
 * Project home page: https://github.com/mik3y/usb-serial-for-android
 */

using Android.Hardware.Usb;
using Android.Util;
using Java.Lang;
using UsbSerialAndroid.Utils;

namespace UsbSerialAndroid.Drivers;

public sealed class CdcAcmSerialDriverFactory : IUsbSerialDriverFactory
{
    public IUsbSerialDriver Create(UsbDevice device)
    {
        return new CdcAcmSerialDriver(device);
    }

    public Dictionary<int, int[]> GetSupportedDevices()
    {
        return [];
    }

    public bool Probe(UsbDevice device)
    {
        return device.CountAcmPorts() > 0;
    }
}

/// <summary>
/// USB CDC/ACM serial driver implementation.
/// @author mike wakerly (opensource@hoho.com)
/// @see <a
/// href="http://www.usb.org/developers/devclass_docs/usbcdc11.pdf">
/// Universal Serial Bus Class Definitions for Communication Devices, v1.1</a>
/// </summary>
public class CdcAcmSerialDriver : IUsbSerialDriver
{
    public const int USB_SUBCLASS_ACM = 2;

    private const string TAG = nameof(CdcAcmSerialDriver);

    private readonly UsbDevice mDevice;

    private readonly List<IUsbSerialPort> mPorts = [];

    public CdcAcmSerialDriver(UsbDevice device)
    {
        mDevice = device ?? throw new ArgumentNullException(nameof(device));
        int ports = device.CountAcmPorts();
        for (int port = 0; port < ports; port++)
        {
            mPorts.Add(new CdcAcmSerialPort(this, mDevice, port));
        }
        if (mPorts.Count == 0)
        {
            mPorts.Add(new CdcAcmSerialPort(this, mDevice, -1));
        }
    }

    public UsbDevice GetDevice()
    {
        return mDevice;
    }

    public List<IUsbSerialPort> GetPorts()
    {
        return mPorts;
    }

    public class CdcAcmSerialPort : CommonUsbSerialPort
    {
        private readonly CdcAcmSerialDriver _owningDriver;
        private UsbInterface? _controlInterface;
        private UsbInterface? _dataInterface;
        private UsbEndpoint? _controlEndpoint;

        private int _controlIndex;
        private bool _rts = false;
        private bool _dtr = false;

        private const int USB_RECIP_INTERFACE = 0x01;
        private const int USB_RT_ACM = UsbConstants.UsbTypeClass | USB_RECIP_INTERFACE;
        private const int SET_LINE_CODING = 0x20;  // USB CDC 1.1 section 6.2
        private const int GET_LINE_CODING = 0x21;
        private const int SET_CONTROL_LINE_STATE = 0x22;
        private const int SEND_BREAK = 0x23;

        public CdcAcmSerialPort(CdcAcmSerialDriver owningDriver, UsbDevice device, int portNumber) : base(device, portNumber)
        {
            _owningDriver = owningDriver ?? throw new ArgumentNullException(nameof(owningDriver));
        }

        public override IUsbSerialDriver GetDriver()
        {
            return _owningDriver;
        }

        protected override void OpenInt()
        {
            Log.Debug(TAG, "interfaces:");
            for (int i = 0; i < _device.InterfaceCount; i++)
            {
                Log.Debug(TAG, _device.GetInterface(i).ToString());
            }
            if (_portNumber == -1)
            {
                Log.Debug(TAG, "device might be castrated ACM device, trying single interface logic");
                OpenSingleInterface();
            }
            else
            {
                Log.Debug(TAG, "trying default interface logic");
                OpenInterface();
            }
        }

        private void OpenSingleInterface()
        {
            // the following code is inspired by the cdc-acm driver in the linux kernel

            _controlIndex = 0;
            _controlInterface = _device.GetInterface(0);
            _dataInterface = _device.GetInterface(0);
            if (!_connection.ClaimInterface(_controlInterface, true))
            {
                throw new IOException("Could not claim shared control/data interface");
            }

            for (int i = 0; i < _controlInterface.EndpointCount; ++i)
            {
                UsbEndpoint ep = _controlInterface.GetEndpoint(i);
                if ((ep.Direction == UsbAddressing.In) && (ep.Type == UsbAddressing.XferInterrupt))
                {
                    _controlEndpoint = ep;
                }
                else if ((ep.Direction == UsbAddressing.In) && (ep.Type == UsbAddressing.XferBulk))
                {
                    _readEndpoint = ep;
                }
                else if ((ep.Direction == UsbAddressing.Out) && (ep.Type == UsbAddressing.XferBulk))
                {
                    _writeEndpoint = ep;
                }
            }
            if (_controlEndpoint == null)
            {
                throw new IOException("No control endpoint");
            }
        }

        private void OpenInterface()
        {
            _controlInterface = null;
            _dataInterface = null;
            int j = GetInterfaceIdFromDescriptors();
            Log.Debug(TAG, "interface count=" + _device.InterfaceCount + ", IAD=" + j);
            if (j >= 0)
            {
                for (int i = 0; i < _device.InterfaceCount; i++)
                {
                    UsbInterface usbInterface = _device.GetInterface(i);
                    if (usbInterface.Id == j || usbInterface.Id == j + 1)
                    {
                        if (usbInterface.InterfaceClass == UsbClass.Comm &&
                                usbInterface.InterfaceSubclass == (UsbClass)USB_SUBCLASS_ACM)
                        {
                            _controlIndex = usbInterface.Id;
                            _controlInterface = usbInterface;
                        }
                        if (usbInterface.InterfaceClass == UsbClass.CdcData)
                        {
                            _dataInterface = usbInterface;
                        }
                    }
                }
            }
            if (_controlInterface == null || _dataInterface == null)
            {
                Log.Debug(TAG, "no IAD fallback");
                int controlInterfaceCount = 0;
                int dataInterfaceCount = 0;
                for (int i = 0; i < _device.InterfaceCount; i++)
                {
                    UsbInterface usbInterface = _device.GetInterface(i);
                    if (usbInterface.InterfaceClass == UsbClass.Comm &&
                            usbInterface.InterfaceSubclass == (UsbClass)USB_SUBCLASS_ACM)
                    {
                        if (controlInterfaceCount == _portNumber)
                        {
                            _controlIndex = usbInterface.Id;
                            _controlInterface = usbInterface;
                        }
                        controlInterfaceCount++;
                    }
                    if (usbInterface.InterfaceClass == UsbClass.CdcData)
                    {
                        if (dataInterfaceCount == _portNumber)
                        {
                            _dataInterface = usbInterface;
                        }
                        dataInterfaceCount++;
                    }
                }
            }

            if (_controlInterface == null)
            {
                throw new IOException("No control interface");
            }
            Log.Debug(TAG, "Control interface id " + _controlInterface.Id);

            if (!_connection.ClaimInterface(_controlInterface, true))
            {
                throw new IOException("Could not claim control interface");
            }
            _controlEndpoint = _controlInterface.GetEndpoint(0);
            if (_controlEndpoint.Direction != UsbAddressing.In || _controlEndpoint.Type != UsbAddressing.XferInterrupt)
            {
                throw new IOException("Invalid control endpoint");
            }

            if (_dataInterface == null)
            {
                throw new IOException("No data interface");
            }
            Log.Debug(TAG, "data interface id " + _dataInterface.Id);
            if (!_connection.ClaimInterface(_dataInterface, true))
            {
                throw new IOException("Could not claim data interface");
            }
            for (int i = 0; i < _dataInterface.EndpointCount; i++)
            {
                UsbEndpoint ep = _dataInterface.GetEndpoint(i);
                if (ep.Direction == UsbAddressing.In && ep.Type == UsbAddressing.XferBulk)
                    _readEndpoint = ep;
                if (ep.Direction == UsbAddressing.Out && ep.Type == UsbAddressing.XferBulk)
                    _writeEndpoint = ep;
            }
        }

        private int GetInterfaceIdFromDescriptors()
        {
            var descriptors = _connection.GetDescriptors();
            Log.Debug(TAG, "USB descriptor:");
            foreach (var descriptor in descriptors)
            {
                Log.Debug(TAG, HexDump.ToHexString(descriptor));
            }

            if (descriptors.Count > 0 &&
                descriptors[0].Length == 18 &&
                descriptors[0][1] == 1 && // bDescriptorType
                descriptors[0][4] == (byte)UsbClass.Misc && //bDeviceClass
                descriptors[0][5] == 2 && // bDeviceSubClass
                descriptors[0][6] == 1)
            { // bDeviceProtocol
                // is IAD device, see https://www.usb.org/sites/default/files/iadclasscode_r10.pdf
                int port = -1;
                for (int d = 1; d < descriptors.Count; d++)
                {
                    if (descriptors[d].Length == 8 &&
                            descriptors[d][1] == 0x0b && // bDescriptorType == IAD
                            descriptors[d][4] == (byte)UsbClass.Comm && // bFunctionClass == CDC
                            descriptors[d][5] == USB_SUBCLASS_ACM)
                    { // bFunctionSubClass == ACM
                        port++;
                        if (port == _portNumber &&
                                descriptors[d][3] == 2)
                        { // bInterfaceCount
                            return descriptors[d][2]; // bFirstInterface
                        }
                    }
                }
            }
            return -1;
        }

        private int SendAcmControlMessage(int request, int value, byte[]? buf)
        {
            int len = _connection.ControlTransfer(
                    (UsbAddressing)USB_RT_ACM, request, value, _controlIndex, buf, buf != null ? buf.Length : 0, 5000);
            if (len < 0)
            {
                throw new IOException("controlTransfer failed");
            }
            return len;
        }

        protected override void CloseInt()
        {
            try
            {
                _connection.ReleaseInterface(_controlInterface);
                _connection.ReleaseInterface(_dataInterface);
            }
            catch (Java.Lang.Exception) { /* ignored */ }
        }

        public override void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity)
        {
            if (baudRate <= 0)
            {
                throw new Java.Lang.IllegalArgumentException("Invalid baud rate: " + baudRate);
            }
            if (dataBits < DataBits._5 || dataBits > DataBits._8)
            {
                throw new Java.Lang.IllegalArgumentException("Invalid data bits: " + dataBits);
            }

            var stopBitsByte = stopBits switch
            {
                StopBits._1 => (byte)0,
                StopBits._1_5 => (byte)1,
                StopBits._2 => (byte)2,
                _ => throw new IllegalArgumentException("Invalid stop bits: " + stopBits),
            };

            var parityBitesByte = parity switch
            {
                Parity.None => (byte)0,
                Parity.Odd => (byte)1,
                Parity.Even => (byte)2,
                Parity.Mark => (byte)3,
                Parity.Space => (byte)4,
                _ => throw new IllegalArgumentException("Invalid parity: " + parity),
            };

            byte[] msg = {
                    (byte) ( baudRate & 0xff),
                    (byte) ((baudRate >> 8 ) & 0xff),
                    (byte) ((baudRate >> 16) & 0xff),
                    (byte) ((baudRate >> 24) & 0xff),
                    stopBitsByte,
                    parityBitesByte,
                    (byte) dataBits};

            SendAcmControlMessage(SET_LINE_CODING, 0, msg);
        }

        public override bool GetDTR()
        {
            return _dtr;
        }

        public override void SetDTR(bool value)
        {
            _dtr = value;
            SetDtrRts();
        }

        public override bool GetRTS()
        {
            return _rts;
        }

        public override void SetRTS(bool value)
        {
            _rts = value;
            SetDtrRts();
        }

        private void SetDtrRts()
        {
            int value = (_rts ? 0x2 : 0) | (_dtr ? 0x1 : 0);
            SendAcmControlMessage(SET_CONTROL_LINE_STATE, value, null);
        }

        public override HashSet<ControlLine> GetControlLines()
        {
            HashSet<ControlLine> set = new();

            if (_rts)
            {
                set.Add(ControlLine.RTS);
            }

            if (_dtr)
            {
                set.Add(ControlLine.DTR);
            }

            return set;
        }

        public override HashSet<ControlLine> GetSupportedControlLines()
        {
            return [ControlLine.RTS, ControlLine.DTR];
        }

        public override void SetBreak(bool value)
        {
            SendAcmControlMessage(SEND_BREAK, value ? 0xffff : 0, null);
        }
    }
}