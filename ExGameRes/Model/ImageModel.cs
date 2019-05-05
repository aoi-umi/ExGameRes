using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model
{
    public class ImageModel
    {        
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
    }

    public class BmpModel
    {
        public uint headerSize = 14 + 40;
        public Byte[] header = new Byte[]{
            0x42,0x4D,0x36,0x00,0x0C,0x00,0x00,0x00,
            0x00,0x00,0x36,0x00,0x00,0x00,0x28,0x00,
            0x00,0x00,0x00,0x02,0x00,0x00,0x00,0x02,
            0x00,0x00,0x01,0x00,0x18,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x00,0x00
        };

        public Byte[] GetData(uint width, uint height, Byte[] pixel)
        {
            var w = width;
            var h = height;
            Byte[] imgData = null,
                WidthBytes = BitConverter.GetBytes(w),
                HeightBytes = BitConverter.GetBytes(h);
            uint rowByteCount = (uint)((w * 3 + 3) & (~3));
            uint imageSize = rowByteCount * h;
            var fileSize = headerSize + imageSize;
            Helper.ResetBytes(header, 2, BitConverter.GetBytes(headerSize + imageSize));
            Helper.ResetBytes(header, 10, BitConverter.GetBytes(headerSize));
            Helper.ResetBytes(header, 18, WidthBytes);
            Helper.ResetBytes(header, 22, HeightBytes);
            Helper.ResetBytes(header, 34, BitConverter.GetBytes(imageSize));
            imgData = new Byte[fileSize];
            header.CopyTo(imgData, 0);
            pixel.CopyTo(imgData, headerSize);
            return imgData;
        }
    }

    public class PngModel
    {
        public uint headerSize = 8 + 25;
        public Byte[] PNGHeader = new Byte[] {
            0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a,
            0x00,0x00,0x00,0x0d,0x49,0x48,0x44,0x52,//[0x08-0x0b]IHDR lenght [0x0c-0x1c]IHDR
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,//[0x10-0x13]width [0x14-0x17]height
            0x08,0x06,0x00,0x00,0x00,0x00,0x00,0x00,0x00//[0x1d-0x20]IHRD crc
        };

        public Byte[] IEND = new Byte[] {
            0x00,0x00,0x00,0x00,0x49,0x45,0x4e,0x44,
            0xae,0x42,0x60,0x82
        };

        public Byte[] GetData(uint width, uint height, Byte[] pixel, Byte[] alpha)
        {
            var h = height;
            var w = width;
            Byte[] imgData = null,
                WidthBytes = BitConverter.GetBytes(w),
                HeightBytes = BitConverter.GetBytes(h);
            uint rowByteCount = w * 4;
            uint imageSize = rowByteCount * h;
            var fileSize = headerSize + imageSize;
            uint crc32 = 0;
            Byte[] CRC32Bytes = null;
            fileSize = headerSize + (uint)IEND.Length;

            Array.Reverse(WidthBytes);
            Array.Reverse(HeightBytes);
            Helper.ResetBytes(PNGHeader, 0x10, WidthBytes);
            Helper.ResetBytes(PNGHeader, 0x14, HeightBytes);
            crc32 = Helper.CRC32(Helper.GetBytes(PNGHeader, 0x0c, 17));
            CRC32Bytes = BitConverter.GetBytes(crc32);
            Array.Reverse(CRC32Bytes);
            Helper.ResetBytes(PNGHeader, 0x1d, CRC32Bytes);

            var PixelWithAlpha = new Byte[imageSize + h];

            for (int i = 0, y = 0; i < h; i++)
            {
                Array.Copy(new Byte[] { 0x00 }, 0, PixelWithAlpha, y * (rowByteCount + 1), 1);
                y++;
                for (int j = 0, x = 0; j < w; j++, x++)
                {
                    int currAlphaPos = (int)((h - i - 1) * w + j);
                    int currPixelPos = currAlphaPos * 3;
                    Byte b = pixel[currPixelPos + 2];
                    pixel[currPixelPos + 2] = pixel[currPixelPos];
                    pixel[currPixelPos] = b;
                    Array.Copy(pixel, currPixelPos, PixelWithAlpha, (i * w + j) * 4 + y, 3);
                    Array.Copy(alpha, currAlphaPos, PixelWithAlpha, (i * w + j) * 4 + 3 + y, 1);
                }
            }
            uint compressLength = (uint)PixelWithAlpha.Length;
            Byte[] compressBytes = Helper.Compress(PixelWithAlpha, ref compressLength);
            Byte[] IDAT = new Byte[compressLength + 12];
            Byte[] compressLengthBytes = BitConverter.GetBytes(compressLength);
            crc32 = Helper.CRC32(compressLengthBytes);
            CRC32Bytes = BitConverter.GetBytes(crc32);
            Array.Reverse(CRC32Bytes);
            Array.Reverse(compressLengthBytes);

            Array.Copy(compressLengthBytes, 0, IDAT, 0, 4);
            Array.Copy(new Byte[] { 0x49, 0x44, 0x41, 0x54 }, 0, IDAT, 4, 4);//IDAT
            Array.Copy(compressBytes, 0, IDAT, 8, compressLength);
            Array.Copy(CRC32Bytes, 0, IDAT, 8 + compressLength, 4);

            fileSize += compressLength + 12;
            imgData = new Byte[fileSize];
            Array.Copy(PNGHeader, 0, imgData, 0, headerSize);
            Array.Copy(IDAT, 0, imgData, headerSize, IDAT.Length);
            Array.Copy(IEND, 0, imgData, headerSize + IDAT.Length, IEND.Length);
            return imgData;
        }
    }
}
