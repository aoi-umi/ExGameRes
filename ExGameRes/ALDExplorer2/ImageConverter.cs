using FreeImageAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ALDExplorer2
{
    public interface IImageHeader
    {
        string Signature { get; set; }
        int Version { get; set; }
        int X { get; set; }
        int Y { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        int ColorDepth { get; set; }
        bool HasAlphaChannel { get; }

        string GetComment();
        bool ParseComment(string comment);
    }

    public class CommentUtil
    {
        public static string GetComment(object obj)
        {
            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            fields = GetIntStringAndByteArrayFields(fields);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldValue = field.GetValue(obj);
                if (fieldValue != null)
                {
                    sb.Append(field.Name);
                    sb.Append(" = ");
                    string stringValue = fieldValue.ToString();
                    if (field.FieldType == typeof(string))
                    {
                        stringValue = stringValue.Replace("\\", "\\\\");
                        stringValue = stringValue.Replace("\"", "\\\"");
                        stringValue = stringValue.Replace("\n", "\\n");
                        stringValue = stringValue.Replace("\r", "\\r");
                        stringValue = stringValue.Replace("\t", "\\t");
                        stringValue = "\"" + stringValue + "\"";
                    }
                    if (field.FieldType == typeof(byte[]))
                    {
                        var byteArray = fieldValue as byte[];
                        stringValue = "\"" + byteArray.ToHexString() + "\"";
                    }
                    sb.Append(stringValue);
                }
                if (i < fields.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }

        private static FieldInfo[] GetIntStringAndByteArrayFields(FieldInfo[] fields)
        {
            fields = fields.Where(f => f.FieldType == typeof(int) || f.FieldType == typeof(string) || f.FieldType == typeof(byte[])).ToArray();
            return fields;
        }

        public static string ReadToken(TextReader tr)
        {
            StringBuilder sb = new StringBuilder();

            //eat white space
            int c = tr.Peek();
            if (c == -1)
            {
                return null;
            }
            while (Char.IsWhiteSpace((char)c))
            {
                c = tr.Read();
                c = tr.Peek();
                if (c == -1)
                {
                    return null;
                }
            }
            if (c == '=' || c == ',')
            {
                c = tr.Read();
                sb.Append((char)c);
            }
            else if (c == '"')
            {
                c = tr.Read();
                //read a quoted string
                while (true)
                {
                    c = tr.Read();
                    if (c == '\\')
                    {
                        c = tr.Read();
                        switch (c)
                        {
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            default:
                                sb.Append((char)c);
                                break;
                        }
                    }
                    else if (c == '"')
                    {
                        break;
                    }
                    else if (c == -1)
                    {
                        return null;
                    }
                    sb.Append((char)c);
                }
            }
            else if (c >= '0' && c <= '9')
            {
                while (c >= '0' && c <= '9')
                {
                    c = tr.Read();
                    sb.Append((char)c);
                    c = tr.Peek();
                }
            }
            else
            {
                while (!Char.IsWhiteSpace((char)c) && !(c == ',' || c == '=') && c != -1)
                {
                    c = tr.Read();
                    sb.Append((char)c);
                    c = tr.Peek();
                }
            }
            return sb.ToString();
        }

        public static bool ParseComment(object obj, string comment)
        {
            if (comment == null) return false;
            var sr = new StringReader(comment);
            var fields = GetIntStringAndByteArrayFields(obj.GetType().GetFields());
            var dic = new Dictionary<string, FieldInfo>();
            foreach (var field in fields)
            {
                dic.Add(field.Name.ToUpperInvariant(), field);
            }

            FieldInfo currentField = null;
            for (string token = ReadToken(sr); token != null; token = ReadToken(sr))
            {
                if (dic.ContainsKey(token.ToUpperInvariant()))
                {
                    currentField = dic[token.ToUpperInvariant()];
                }
                else if (token == "=")
                {
                    token = ReadToken(sr);
                    if (token == null) return false;
                    if (currentField == null) return false;
                    if (currentField.FieldType == typeof(int))
                    {
                        int intValue;
                        if (int.TryParse(token, out intValue))
                        {
                            currentField.SetValue(obj, intValue);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (currentField.FieldType == typeof(string))
                    {
                        currentField.SetValue(obj, token);
                    }
                    else if (currentField.FieldType == typeof(byte[]))
                    {
                        var bytes = BinaryUtil.GetBytesFromHexString(token);
                        if (bytes == null)
                        {
                            return false;
                        }
                        currentField.SetValue(obj, bytes);
                    }
                    currentField = null;
                }
                else if (token == ",")
                {

                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
    public static partial class Extensions
    {
        public static Scanline<byte> GetScanlineFromTop8Bit(this FreeImageBitmap bitmap, int y)
        {
            return bitmap.GetScanline<byte>(bitmap.Height - 1 - y);
        }
        public static Scanline<RGBTRIPLE> GetScanlineFromTop24Bit(this FreeImageBitmap bitmap, int y)
        {
            return bitmap.GetScanline<RGBTRIPLE>(bitmap.Height - 1 - y);
        }
        public static Scanline<int> GetScanlineFromTop32Bit(this FreeImageBitmap bitmap, int y)
        {
            return bitmap.GetScanline<int>(bitmap.Height - 1 - y);
        }
        public static Scanline<FI4BIT> GetScanlineFromTop4Bit(this FreeImageBitmap bitmap, int y)
        {
            return bitmap.GetScanline<FI4BIT>(bitmap.Height - 1 - y);
        }
    }

    public static class SwfToAffConverter
    {
        static byte[] xorKey = new byte[] { 0xC8, 0xBB, 0x8F, 0xB7, 0xED, 0x43, 0x99, 0x4A, 0xA2, 0x7E, 0x5B, 0xB0, 0x68, 0x18, 0xF8, 0x88, 0x53 };

        public static byte[] ConvertSwfToAff(byte[] swfBytes)
        {
            MemoryStream ms = new MemoryStream();
            ConvertSwfToAff(swfBytes, ms);
            return ms.ToArray();
        }

        public static void ConvertSwfToAff(byte[] swfBytes, Stream outputStream)
        {
            var bw = new BinaryWriter(outputStream);

            //"AFF\0", 1, filesize, 0x4D2

            bw.Write(ASCIIEncoding.ASCII.GetBytes("AFF"));
            bw.Write((byte)0);
            bw.Write((int)1);
            bw.Write((int)swfBytes.Length + 16);
            bw.Write((int)0x4D2);

            //screw around with first 0x40 bytes of SWF file
            int count = Math.Min(swfBytes.Length, 0x40);

            for (int i = 0; i < count; i++)
            {
                int i2 = i & 0x0F;
                swfBytes[i] ^= xorKey[i2];
            }

            bw.Write(swfBytes);
            bw.Flush();
        }

        public static void ConvertSwfToAff(string swfFileName, string affFileName)
        {
            var swfBytes = File.ReadAllBytes(swfFileName);

            using (FileStream fs = new FileStream(affFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                ConvertSwfToAff(swfBytes, fs);

                fs.Flush();
                fs.Close();
            }
        }

        public static byte[] ConvertAffToSwf(byte[] bytes)
        {
            return ConvertAffToSwf(new MemoryStream(bytes));
        }

        public static byte[] ConvertAffToSwf(Stream inputStream)
        {
            var br = new BinaryReader(inputStream);
            byte[] affHeader = br.ReadBytes(16);
            byte[] swfBytes = br.ReadBytes((int)br.BaseStream.Length - 16);
            int count = Math.Min(swfBytes.Length, 0x40);
            for (int i = 0; i < count; i++)
            {
                int i2 = i & 0x0F;
                swfBytes[i] ^= xorKey[i2];
            }
            return swfBytes;
        }

        public static void ConvertAffToSwf(string affFileName, string swfFileName)
        {
            //not yet tested
            using (FileStream fs = new FileStream(affFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var swfBytes = ConvertAffToSwf(fs);
                File.WriteAllBytes(swfFileName, swfBytes);
            }
        }
    }
    public static partial class BinaryUtil
    {
        static Encoding shiftJis = Encoding.GetEncoding("shift_jis");

        public static string IntToString(int intValue)
        {
            var bytes = BitConverter.GetBytes(intValue);
            int indexOfZero = Array.IndexOf<byte>(bytes, 0);
            if (indexOfZero == -1) indexOfZero = 4;
            return shiftJis.GetString(bytes, 0, indexOfZero);
        }

        public static int StringToInt(string stringValue)
        {
            byte[] bytes = shiftJis.GetBytes(stringValue);
            if (bytes.Length > 4)
            {
                bytes = bytes.Take(4).ToArray();
            }
            else if (bytes.Length < 4)
            {
                bytes = bytes.Concat(Enumerable.Repeat((Byte)0, 4 - bytes.Length)).ToArray();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        public static byte[] GetBytesFromHexString(string hexString)
        {
            if ((hexString.Length & 1) == 1)
            {
                return null;
            }
            List<byte> bytes = new List<byte>(hexString.Length / 2);
            for (int i = 0; i < hexString.Length; i += 2)
            {
                string hexPair = hexString.Substring(i, 2);
                byte b;
                if (byte.TryParse(hexPair, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out b))
                {
                    bytes.Add(b);
                }
                else
                {
                    return null;
                }
            }
            return bytes.ToArray();
        }

        public static unsafe string FixedBytesToString(byte* bytes, int bufferLength, Encoding encoding)
        {
            unsafe
            {
                int strLen = 0;
                {
                    int i;
                    for (i = 0; i < bufferLength; i++)
                    {
                        if (bytes[i] == 0)
                        {
                            break;
                        }
                    }
                    strLen = i;
                }
                byte[] fileNameBytes = new byte[strLen];
                Marshal.Copy((IntPtr)bytes, fileNameBytes, 0, strLen);
                return encoding.GetString(fileNameBytes);
            }
        }

        public static unsafe void StringToFixedBytes(byte* bytes, int bufferLength, string str, Encoding encoding)
        {
            unsafe
            {
                byte[] fileNameBytes = encoding.GetBytes(str);
                int lengthToCopy = Math.Min(fileNameBytes.Length, bufferLength);
                Marshal.Copy(fileNameBytes, 0, (IntPtr)bytes, lengthToCopy);
            }

        }
    }
}