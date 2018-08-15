using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model
{
    public class Qnt
    {
        public string Ext { get; set; }
        public uint Offset { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint BitCount { get; set; }
        public uint PixelTocLength { get; set; }
        public uint AlphaTocLength { get; set; }
        private byte[] PixelData { get; set; }
        private byte[] AlphaData { get; set; }
        private QntHeader QntHeader { get; set; }
        private const int ZLIBBUF_MARGIN = 10240;

        public Qnt(Stream stream)
        {
            InitQnt(stream);
        }

        public Qnt(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                InitQnt(stream);
            }
        }

        private void InitQnt(Stream stream)
        {
            QntHeader = new QntHeader();
            using (BinaryReader br = new BinaryReader(stream))
            {
                QntHeader.Signature = Helper.BytesToString(br.ReadBytes(4));
                if (QntHeader.Signature.Substring(0, 3) != Config.Signature.QNT)
                    throw new MyException(Config.Signature.QNT, MyException.ErrorTypeEnum.FileTypeError);
                QntHeader.Version = br.ReadUInt32();
                if (QntHeader.Version == 0)
                {
                    Offset = QntHeader.Offset = 48;
                }
                else
                {
                    Offset = QntHeader.Offset = br.ReadUInt32();
                }
                QntHeader.X0 = br.ReadUInt32();
                QntHeader.Y0 = br.ReadUInt32();
                Width = QntHeader.Width = br.ReadUInt32();
                Height = QntHeader.Height = br.ReadUInt32();
                BitCount = QntHeader.BitCount = br.ReadUInt32();
                QntHeader.RSV = br.ReadUInt32();
                PixelTocLength = QntHeader.PixelTocLength = br.ReadUInt32();
                AlphaTocLength = QntHeader.AlphaTocLength = br.ReadUInt32();
                if (QntHeader.Version == 0)
                {
                    QntHeader.Unknow4 = br.ReadBytes(8);
                }
                else
                {
                    QntHeader.Unknow4 = br.ReadBytes(24);
                }
                PixelData = br.ReadBytes((int)PixelTocLength);
                if (AlphaTocLength > 0)
                {
                    AlphaData = br.ReadBytes((int)AlphaTocLength);
                    Ext = "png";
                }
                else
                {
                    Ext = "bmp";
                }
            }
        }

        public static Byte[] ExtractQnt(Qnt qntFile)
        {
            Byte[] BMPHeader = new Byte[]{
                0x42,0x4D,0x36,0x00,0x0C,0x00,0x00,0x00,
                0x00,0x00,0x36,0x00,0x00,0x00,0x28,0x00,
                0x00,0x00,0x00,0x02,0x00,0x00,0x00,0x02,
                0x00,0x00,0x01,0x00,0x18,0x00,0x00,0x00,
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0x00,0x00,0x00,0x00,0x00,0x00};
            Byte[] PNGHeader = new Byte[] {
                0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a,
                0x00,0x00,0x00,0x0d,0x49,0x48,0x44,0x52,//[0x08-0x0b]IHDR lenght [0x0c-0x1c]IHDR
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,//[0x10-0x13]width [0x14-0x17]height
                0x08,0x06,0x00,0x00,0x00,0x00,0x00,0x00,0x00//[0x1d-0x20]IHRD crc
            };
            Byte[] IEND = new Byte[] {
                0x00,0x00,0x00,0x00,0x49,0x45,0x4e,0x44,
                0xae,0x42,0x60,0x82
            };

            uint bmpHeaderSize = 14 + 40;
            uint pngHeaderSize = 8 + 25;
            uint w = qntFile.Width, h = qntFile.Height;
            uint lcount = (uint)((w * 3 + 3) & (~3));
            uint imageSize = lcount * h;
            Byte[] Pixel = ExtractPixel(qntFile, !(qntFile.AlphaTocLength > 0));
            Byte[] PixelWithAlpha = null;
            Byte[] Alpha = qntFile.AlphaTocLength > 0 ? ExtractAlpha(qntFile) : null;
            uint fileSize = 0;
            if (Alpha != null)
            {
                lcount = w * 4;
                imageSize = lcount * h;
                ResetBytes(BMPHeader, 28, new Byte[] { 32 });
                Alpha = ExtractAlpha(qntFile);
            }

            Byte[] imgData = null;
            Byte[] WidthBytes = BitConverter.GetBytes(w);
            Byte[] HeightBytes = BitConverter.GetBytes(h);
            if (Alpha == null)
            {
                fileSize = bmpHeaderSize + imageSize;
                ResetBytes(BMPHeader, 2, BitConverter.GetBytes(bmpHeaderSize + imageSize));
                ResetBytes(BMPHeader, 10, BitConverter.GetBytes(bmpHeaderSize));
                ResetBytes(BMPHeader, 18, WidthBytes);
                ResetBytes(BMPHeader, 22, HeightBytes);
                ResetBytes(BMPHeader, 34, BitConverter.GetBytes(imageSize));
                imgData = new Byte[fileSize];
                BMPHeader.CopyTo(imgData, 0);
                Pixel.CopyTo(imgData, bmpHeaderSize);
            }
            else
            {
                uint crc32 = 0;
                Byte[] CRC32Bytes = null;
                fileSize = pngHeaderSize + (uint)IEND.Length;

                Array.Reverse(WidthBytes);
                Array.Reverse(HeightBytes);
                ResetBytes(PNGHeader, 0x10, WidthBytes);
                ResetBytes(PNGHeader, 0x14, HeightBytes);
                crc32 = Helper.CRC32(Helper.GetBytes(PNGHeader, 0x0c, 17));
                CRC32Bytes = BitConverter.GetBytes(crc32);
                Array.Reverse(CRC32Bytes);
                ResetBytes(PNGHeader, 0x1d, CRC32Bytes);

                PixelWithAlpha = new Byte[imageSize + h];

                for (int i = 0, y = 0; i < h; i++)
                {
                    Array.Copy(new Byte[] { 0x00 }, 0, PixelWithAlpha, y * (lcount + 1), 1);
                    y++;
                    for (int j = 0, x = 0; j < w; j++, x++)
                    {
                        int currAlphaPos = (int)((h - i - 1) * w + j);
                        int currPixelPos = currAlphaPos * 3;
                        Byte b = Pixel[currPixelPos + 2];
                        Pixel[currPixelPos + 2] = Pixel[currPixelPos];
                        Pixel[currPixelPos] = b;
                        Array.Copy(Pixel, currPixelPos, PixelWithAlpha, (i * w + j) * 4 + y, 3);
                        Array.Copy(Alpha, currAlphaPos, PixelWithAlpha, (i * w + j) * 4 + 3 + y, 1);
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
                Array.Copy(PNGHeader, 0, imgData, 0, pngHeaderSize);
                Array.Copy(IDAT, 0, imgData, pngHeaderSize, IDAT.Length);
                Array.Copy(IEND, 0, imgData, pngHeaderSize + IDAT.Length, IEND.Length);
            }

            return imgData;
        }

        private static Byte[] ExtractPixel(Qnt qntFile, bool fixWidth)
        {
            uint size = qntFile.Width * qntFile.Height * qntFile.BitCount / 8 + ZLIBBUF_MARGIN;
            uint outTocBuffLength = size;
            Byte[] outTocBuff = Helper.Decompress(qntFile.PixelData, ref outTocBuffLength);

            Byte[] pic = new Byte[outTocBuffLength];
            int i, j;
            uint x, y, w, h;
            w = qntFile.Width;
            h = qntFile.Height;
            j = 0;
            for (i = 2; i >= 0; i--)
            {
                for (y = 0; y < h - 1; y += 2)
                {
                    for (x = 0; x < w - 1; x += 2)
                    {
                        pic[((y + 0) * w + x) * 3 + i] = outTocBuff[j];
                        pic[((y + 1) * w + x) * 3 + i] = outTocBuff[j + 1];
                        pic[((y + 0) * w + x + 1) * 3 + i] = outTocBuff[j + 2];
                        pic[((y + 1) * w + x + 1) * 3 + i] = outTocBuff[j + 3];
                        j += 4;
                    }
                    if (x != w)
                    {
                        pic[(y * w + x) * 3 + i] = outTocBuff[j];
                        pic[((y + 1) * w + x) * 3 + i] = outTocBuff[j + 1];
                        j += 4;
                    }
                }
                if (y != h)
                {
                    for (x = 0; x < w - 1; x += 2)
                    {
                        pic[(y * w + x) * 3 + i] = outTocBuff[j];
                        pic[(y * w + x + 1) * 3 + i] = outTocBuff[j + 2];
                        j += 4;
                    }
                    if (x != w)
                    {
                        pic[(y * w + x) * 3 + i] = outTocBuff[j];
                        pic[((y + 1) * w + x) * 3 + i] = outTocBuff[j + 1];
                        j += 4;
                    }
                }
            }

            if (w > 1)
            {
                for (x = 1; x < w; x++)
                {
                    pic[x * 3] = (byte)(pic[(x - 1) * 3] - pic[x * 3]);
                    pic[x * 3 + 1] = (byte)(pic[(x - 1) * 3 + 1] - pic[x * 3 + 1]);
                    pic[x * 3 + 2] = (byte)(pic[(x - 1) * 3 + 2] - pic[x * 3 + 2]);
                }
            }
            if (h > 1)
            {
                for (y = 1; y < h; y++)
                {
                    pic[(y * w) * 3] = (byte)(pic[((y - 1) * w) * 3] - pic[(y * w) * 3]);
                    pic[(y * w) * 3 + 1] = (byte)(pic[((y - 1) * w) * 3 + 1] - pic[(y * w) * 3 + 1]);
                    pic[(y * w) * 3 + 2] = (byte)(pic[((y - 1) * w) * 3 + 2] - pic[(y * w) * 3 + 2]);

                    for (x = 1; x < w; x++)
                    {
                        int px, py;
                        py = pic[((y - 1) * w + x) * 3];
                        px = pic[(y * w + x - 1) * 3];
                        pic[(y * w + x) * 3] = (byte)(((py + px) >> 1) - pic[(y * w + x) * 3]);
                        py = pic[((y - 1) * w + x) * 3 + 1];
                        px = pic[(y * w + x - 1) * 3 + 1];
                        pic[(y * w + x) * 3 + 1] = (byte)(((py + px) >> 1) - pic[(y * w + x) * 3 + 1]);
                        py = pic[((y - 1) * w + x) * 3 + 2];
                        px = pic[(y * w + x - 1) * 3 + 2];
                        pic[(y * w + x) * 3 + 2] = (byte)(((py + px) >> 1) - pic[(y * w + x) * 3 + 2]);
                    }
                }
            }

            uint lcount = fixWidth ? (uint)((qntFile.Width * 3 + 3) & (~3)) : qntFile.Width * 3;
            byte[] truePic = new byte[lcount * qntFile.Height];
            int offset = 0;
            for (i = 0; i < h; i++)
            {
                for (j = 0; j < w; j++)
                {
                    truePic[offset++] = pic[((h - 1 - i) * w + j) * 3 + 2];
                    truePic[offset++] = pic[((h - 1 - i) * w + j) * 3 + 1];
                    truePic[offset++] = pic[((h - 1 - i) * w + j) * 3 + 0];
                }
                if (fixWidth)
                {
                    for (j = 0; j < lcount - w * 3; j++) truePic[offset++] = 0;
                }
            }
            return truePic;
        }

        private static Byte[] ExtractAlpha(Qnt qntFile)
        {
            uint size = qntFile.Width * qntFile.Height * qntFile.BitCount / 8 + ZLIBBUF_MARGIN;
            uint outTocBuffLength = size;
            Byte[] outTocBuff = Helper.Decompress(qntFile.AlphaData, ref outTocBuffLength);

            Byte[] alpha = new Byte[outTocBuffLength];
            uint i, x, y, w, h;
            w = qntFile.Width;
            h = qntFile.Height;

            i = 1;
            if (w > 1)
            {
                alpha[0] = outTocBuff[0];
                for (x = 1; x < w; x++)
                {
                    alpha[x] = (byte)(alpha[x - 1] - outTocBuff[i]);
                    i++;
                }
                if (w % 2 != 0) i += 1;
            }

            if (h > 1)
            {
                for (y = 1; y < h; y++)
                {
                    alpha[y * w] = (byte)(alpha[(y - 1) * w] - outTocBuff[i]);
                    i++;
                    for (x = 1; x < w; x++)
                    {
                        int pax, pay;
                        pax = alpha[y * w + x - 1];
                        pay = alpha[(y - 1) * w + x];
                        alpha[y * w + x] = (byte)(((pax + pay) >> 1) - outTocBuff[i]);
                        i++;
                    }
                    if (w % 2 != 0) i += 1;
                }
            }
            Byte[] trueAlpha = new Byte[outTocBuffLength];
            for (y = 0; y < h; y++)
            {
                for (x = 0; x < w; x++)
                {
                    trueAlpha[y * w + x] = alpha[(h - y - 1) * w + x];
                }
            }
            return trueAlpha;
        }

        private static void ResetBytes(Byte[] src, int startPos, Byte[] newBytes)
        {
            for (int i = 0; i < newBytes.Length; i++)
            {
                src[startPos + i] = newBytes[i];
            }
        }
    }

    public class QntHeader
    {
        //4 bytes
        public string Signature { get; set; }

        //4bytes
        public uint Version { get; set; }

        //4 bytes
        public uint Offset { get; set; }

        //4 bytes
        public uint X0 { get; set; }

        //4 bytes
        public uint Y0 { get; set; }

        //4 bytes
        public uint Width { get; set; }

        //4 bytes
        public uint Height { get; set; }

        //4 bytes
        public uint BitCount { get; set; }

        //4 bytes
        public uint RSV { get; set; }

        //4 bytes
        public uint PixelTocLength { get; set; }

        //4 bytes
        public uint AlphaTocLength { get; set; }

        //24 bytes
        public byte[] Unknow4 { get; set; }
    }
}
