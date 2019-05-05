﻿using Ionic.Zlib;
using Ionic.Crc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ExGameRes
{
    public static class Helper
    {
        //[DllImport("zlib1.dll", CallingConvention = CallingConvention.Cdecl)]
        //private static extern int uncompress(Byte[] dest, ref uint destLen, Byte[] source, uint sourceLen);

        //[DllImport("zlib1.dll", CallingConvention = CallingConvention.Cdecl)]
        //private static extern int compress(Byte[] dest, ref uint destLen, Byte[] source, uint sourceLen);

        //private static Byte[] CompressV1(Byte[] source, ref uint destLen)
        //{
        //    Byte[] dest = new Byte[destLen];
        //    int result = compress(dest, ref destLen, source, (uint)source.Length);
        //    if (result < 0)
        //        throw new Exception("压缩出错,错误代码:" + result);
        //    return dest;
        //}
        //private static Byte[] DecompressV1(Byte[] source, ref uint destLen)
        //{
        //    var dest = new Byte[destLen];
        //    int result = uncompress(dest, ref destLen, source, (uint)source.Length);
        //    if (result < 0)
        //        throw new Exception("解压出错,错误代码:" + result);
        //    return dest;
        //}

        public static Byte[] Compress(Byte[] source, ref uint destLen)
        {
            var dest = ZlibStream.CompressBuffer(source);
            destLen = (uint)dest.Length;
            return dest;
        }

        public static Byte[] Decompress(Byte[] source, ref uint destLen)
        {
            var dest = ZlibStream.UncompressBuffer(source);
            destLen = (uint)dest.Length;
            return dest;
        }

        public static async void WriteBytesToFile(string path, Byte[] bytes)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        public static void TryHandler(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示");
            }
        }

        public static BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(byteArray);
            bmp.EndInit();
            return bmp;
        }

        public static MemoryStream ByteArrayToStream(byte[] byteArray)
        {
            MemoryStream ms = new MemoryStream(byteArray);
            return ms;
        }

        public static uint CRC32(Byte[] buf)
        {
            var crc = new CRC32();
            using (var stream = ByteArrayToStream(buf))
            {
                return (uint)crc.GetCrc32(stream);
            }
        }

        public static string GetHeader(string filePath)
        {
            string header = string.Empty;
            using (var fs = new FileStream(filePath, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                var bytes = br.ReadBytes(4);
                header = BytesToString(bytes);
            }

            return header;
        }

        public static void MergeBMPData(Byte[] src, Byte[] dest, Byte[] mask = null)
        {
            int bmpHeaderSize = 54;
            if (src.Length != dest.Length)
                throw new MyException("src与dest长度不相等");
            if (mask == null)
            {
                for (int i = bmpHeaderSize; i < src.Length; i++)
                {
                    if (dest[i] == 0 && src[i] != 0) dest[i] = src[i];
                }
            }
            else
            {
                int maskSize = BitConverter.ToInt32(Helper.GetBytes(mask, 0, 4), 0);
                int width = BitConverter.ToInt32(Helper.GetBytes(src, 18, 4), 0);
                int height = BitConverter.ToInt32(Helper.GetBytes(src, 22, 4), 0);
                if (maskSize != mask.Length - 4 || (mask.Length - 4) * 3 * 0x100 != (src.Length - bmpHeaderSize))
                    throw new MyException("mask数据有误", MyException.ErrorTypeEnum.DefaultError);

                int maskBitSize = 0x10;
                int maskWidth = width / maskBitSize;
                int maskHeight = height / maskBitSize;
                for (int i = 0; i < maskHeight; i++)
                {
                    for (int j = 0; j < maskWidth; j++)
                    {
                        if (mask[4 + i * maskWidth + j] != 1)
                            continue;

                        for (int y = 0; y < maskBitSize; y++)
                        {
                            for (int x = 0; x < maskBitSize; x++)
                            {
                                int tagPos = bmpHeaderSize + ((maskHeight - i - 1) * width * maskBitSize + j * maskBitSize + y * width + x) * 3;
                                dest[tagPos] = src[tagPos];
                                dest[tagPos + 1] = src[tagPos + 1];
                                dest[tagPos + 2] = src[tagPos + 2];
                            }
                        }
                    }
                }
            }
        }

        public static Byte[] GetBytes(Byte[] src, int startPos, int length)
        {
            Byte[] b = new Byte[length];
            Array.Copy(src, startPos, b, 0, length);
            return b;
        }

        public static string Encode(string str, Encoding encoding)
        {
            Encoding defaultEncoding = Encoding.Default;
            return encoding.GetString(defaultEncoding.GetBytes(str));
        }

        public static string BytesToString(byte[] bytes)
        {
            return BytesToString(bytes, Encoding.Default);
        }

        public static string BytesToString(byte[] bytes, Encoding encoding)
        {
            var str = string.Empty;
            var index = bytes.ToList().FindIndex(b => b == 0);
            if (index >= 0)
                bytes = Helper.GetBytes(bytes, 0, index);
            str = encoding.GetString(bytes);
            return str;
        }
    }

    public class MyException : Exception
    {
        public enum ErrorTypeEnum
        {
            DefaultError,
            FileTypeError
        }
        public new string Message { get; private set; }
        public MyException(string message) : base(message)
        {
        }
        public MyException(string message, ErrorTypeEnum errorType)
        {
            string error = string.Empty;
            switch (errorType)
            {
                case ErrorTypeEnum.FileTypeError:
                    error = string.Format("该文件不是{0}格式文件", message);
                    break;
                case ErrorTypeEnum.DefaultError:
                default:
                    error = message;
                    break;
            }
            Message = error;
        }
    }
}
