using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model.AliceSoft
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
            using (var br = new BinaryReader(stream))
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
            uint w = qntFile.Width, h = qntFile.Height;
            //uint rowByteCount = (uint)((w * 3 + 3) & (~3));
            //uint imageSize = rowByteCount * h;
            Byte[] pixel = ExtractPixel(qntFile, !(qntFile.AlphaTocLength > 0));
            Byte[] alpha = qntFile.AlphaTocLength > 0 ? ExtractAlpha(qntFile) : null;
            //uint fileSize = 0;
            if (alpha != null)
            {
                //rowByteCount = w * 4;
                //imageSize = rowByteCount * h;
                //ResetBytes(BMPHeader, 28, new Byte[] { 32 });
                alpha = ExtractAlpha(qntFile);
            }

            Byte[] imgData = null;
            if (alpha == null)
            {
                var bmpModel = new BmpModel();
                imgData = bmpModel.GetData(w, h, pixel);
            }
            else
            {
                var pngModel = new PngModel();
                imgData = pngModel.GetData(w, h, pixel, alpha);                
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
