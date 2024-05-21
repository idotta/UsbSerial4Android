namespace UsbSerial4Android;

public static class UsbId
{
    public const int VENDOR_FTDI = 0x0403;
    public const int FTDI_FT232R = 0x6001;
    public const int FTDI_FT2232H = 0x6010;
    public const int FTDI_FT4232H = 0x6011;
    public const int FTDI_FT232H = 0x6014;
    public const int FTDI_FT231X = 0x6015; // same ID for FT230X, FT231X, FT234XD

    public const int VENDOR_SILABS = 0x10c4;
    public const int SILABS_CP2102 = 0xea60; // same ID for CP2101, CP2103, CP2104, CP2109
    public const int SILABS_CP2105 = 0xea70;
    public const int SILABS_CP2108 = 0xea71;

    public const int VENDOR_PROLIFIC = 0x067b;
    public const int PROLIFIC_PL2303 = 0x2303;   // device type 01, T, HX
    public const int PROLIFIC_PL2303GC = 0x23a3; // device type HXN
    public const int PROLIFIC_PL2303GB = 0x23b3; // "
    public const int PROLIFIC_PL2303GT = 0x23c3; // "
    public const int PROLIFIC_PL2303GL = 0x23d3; // "
    public const int PROLIFIC_PL2303GE = 0x23e3; // "
    public const int PROLIFIC_PL2303GS = 0x23f3; // "

    public const int VENDOR_GOOGLE = 0x18d1;
    public const int GOOGLE_CR50 = 0x5014;

    public const int VENDOR_QINHENG = 0x1a86;
    public const int QINHENG_CH340 = 0x7523;
    public const int QINHENG_CH341A = 0x5523;

    public const int VENDOR_UNISOC = 0x1782;
    public const int FIBOCOM_L610 = 0x4D10;
    public const int FIBOCOM_L612 = 0x4D12;
}