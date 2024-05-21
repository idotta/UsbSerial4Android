/*
 * Copyright (C) 2006 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Java.Security;
using System.Text;

namespace UsbSerial4Android.Util;

/// <summary>
/// Clone of Android's /core/java/com/android/internal/util/HexDump class, for use in debugging.
/// Changes: space separated hex strings
/// </summary>
public static class HexDump
{
    private static readonly char[] HEX_DIGITS = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
    };

    public static string DumpHexString(byte[] array)
    {
        return DumpHexString(array, 0, array.Length);
    }

    public static string DumpHexString(byte[] array, int offset, int length)
    {
        StringBuilder result = new();

        byte[] line = new byte[8];
        int lineIndex = 0;

        for (int i = offset; i < offset + length; i++)
        {
            if (lineIndex == line.Length)
            {
                for (int j = 0; j < line.Length; j++)
                {
                    if (line[j] > ' ' && line[j] < '~')
                    {
                        result.Append(Encoding.Default.GetString(line).AsSpan(j, 1));
                    }
                    else
                    {
                        result.Append('.');
                    }
                }

                result.Append('\n');
                lineIndex = 0;
            }

            byte b = array[i];
            result.Append(HEX_DIGITS[(b >>> 4) & 0x0F]);
            result.Append(HEX_DIGITS[b & 0x0F]);
            result.Append(" ");

            line[lineIndex++] = b;
        }

        for (int i = 0; i < (line.Length - lineIndex); i++)
        {
            result.Append("   ");
        }
        for (int i = 0; i < lineIndex; i++)
        {
            if (line[i] > ' ' && line[i] < '~')
            {
                result.Append(Encoding.Default.GetString(line).AsSpan(i, 1));
            }
            else
            {
                result.Append('.');
            }
        }

        return result.ToString();
    }

    public static string ToHexString(byte b)
    {
        return ToHexString(ToByteArray(b));
    }

    public static string ToHexString(byte[] array)
    {
        return ToHexString(array, 0, array.Length);
    }

    public static string ToHexString(byte[] array, int offset, int length)
    {
        char[] buf = new char[length > 0 ? length * 3 - 1 : 0];

        int bufIndex = 0;
        for (int i = offset; i < offset + length; i++)
        {
            if (i > offset)
                buf[bufIndex++] = ' ';
            byte b = array[i];
            buf[bufIndex++] = HEX_DIGITS[(b >>> 4) & 0x0F];
            buf[bufIndex++] = HEX_DIGITS[b & 0x0F];
        }

        return new string(buf);
    }

    public static string ToHexString(int i)
    {
        return ToHexString(ToByteArray(i));
    }

    public static string ToHexString(short i)
    {
        return ToHexString(ToByteArray(i));
    }

    public static byte[] ToByteArray(byte b)
    {
        byte[] array = [b];
        return array;
    }

    public static byte[] ToByteArray(int i)
    {
        byte[] array = new byte[4];

        array[3] = (byte)(i & 0xFF);
        array[2] = (byte)((i >> 8) & 0xFF);
        array[1] = (byte)((i >> 16) & 0xFF);
        array[0] = (byte)((i >> 24) & 0xFF);

        return array;
    }

    public static byte[] ToByteArray(short i)
    {
        byte[] array = new byte[2];

        array[1] = (byte)(i & 0xFF);
        array[0] = (byte)((i >> 8) & 0xFF);

        return array;
    }

    private static int ToByte(char c)
    {
        if (c >= '0' && c <= '9')
            return (c - '0');
        if (c >= 'A' && c <= 'F')
            return (c - 'A' + 10);
        if (c >= 'a' && c <= 'f')
            return (c - 'a' + 10);

        throw new InvalidParameterException("Invalid hex char '" + c + "'");
    }

    /** accepts any separator, e.g. space or newline */

    public static byte[] HexStringToByteArray(string hexString)
    {
        int length = hexString.Length;
        byte[] buffer = new byte[(length + 1) / 3];

        for (int i = 0; i < length; i += 3)
        {
            buffer[i / 3] = (byte)((ToByte(hexString[i]) << 4) | ToByte(hexString[i + 1]));
        }

        return buffer;
    }
}