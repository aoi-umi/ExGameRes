using ALDExplorer2.ImageFileFormats;
using FreeImageAPI;
using System;
using System.IO;

namespace ExGameRes.Model
{
    public class Ajp
    {
        public string Ext { get; set; }
        private AjpHeader ajpHeader;
        private byte[] jpegData;
        private byte[] pmsData;
        public Ajp(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                InitAjp(stream);
            }
        }

        public Ajp(Stream stream)
        {
            InitAjp(stream);
        }

        private void InitAjp(Stream stream)
        {
            using (var br = new BinaryReader(stream))
            {
                ajpHeader = new AjpHeader
                {
                    Signature = Helper.BytesToString(br.ReadBytes(4)),
                    Version = br.ReadInt32(),
                    HeaderSize1 = br.ReadInt32(),
                    Width = br.ReadInt32(),
                    Height = br.ReadInt32(),
                    HeaderSize2 = br.ReadInt32(),
                    JpegDataLength = br.ReadInt32(),
                    AlphaLocation = br.ReadInt32(),
                    SizeOfDataAfterJpeg = br.ReadInt32(),
                    Unknown1 = br.ReadBytes(16),
                    Unknown2 = br.ReadInt32(),
                    Unknown3 = br.ReadBytes(16),
                };

                var data = br.ReadBytes(ajpHeader.JpegDataLength - 16);
                ajpHeader.JpegFooter = br.ReadBytes(16);
                var resolution = BitConverter.ToUInt16(data, 0);
                jpegData = new byte[ajpHeader.JpegDataLength];
                var jpegHeader = new byte[]
                {
                    0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, (byte)'J',(byte)'F',
                    (byte)'I', (byte)'F', 0x00, 0x01, 0x02, 0x00, (byte)(resolution>> 8), (byte)(resolution & 0xFF)
                };
                Array.Copy(jpegHeader, 0, jpegData, 0, 16);
                Array.Copy(data, 0, jpegData, 16, data.Length);

                int pmsSize = ajpHeader.SizeOfDataAfterJpeg - 16;
                if (pmsSize >= 0)
                {
                    pmsData = new byte[pmsSize + 16];
                    var pmsHeader = new byte[] { 0x50, 0x4D, 0x02, 0x00, 0x40, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    Array.Copy(pmsHeader, 0, pmsData, 0, pmsHeader.Length);
                    Array.Copy(br.ReadBytes(pmsSize), 0, pmsData, pmsHeader.Length, pmsSize);
                }
                Ext = "jpg";
            }
        }

        public static byte[] ExtractAjp(Ajp ajpFile)
        {
            byte[] outData;
            using (var ajpStream = new MemoryStream(ajpFile.jpegData))
            {
                var jpegImage = new FreeImageBitmap(ajpStream, FREE_IMAGE_FORMAT.FIF_JPEG);
                //pms
                if (ajpFile.pmsData != null)
                {
                    FreeImageBitmap pmsImage = Pms.LoadImage(ajpFile.pmsData);
                    jpegImage.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_32_BPP);
                    jpegImage.SetChannel(pmsImage, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                }

                using (var ms = new MemoryStream())
                {
                    jpegImage.Save(ms, ajpFile.pmsData == null ? FREE_IMAGE_FORMAT.FIF_JPEG : FREE_IMAGE_FORMAT.FIF_PNG);
                    outData = ms.ToArray();
                }
            }
            return outData;
        }
    }
    public class AjpHeader
    {
        public string Signature;
        public int Version;
        public int HeaderSize1;  //0x38
        public int Width;
        public int Height;
        public int HeaderSize2;  //0x38
        public int JpegDataLength;
        public int AlphaLocation;
        public int SizeOfDataAfterJpeg;
        public byte[] Unknown1;
        public int Unknown2;
        public byte[] Unknown3;
        public byte[] JpegFooter;

        public bool HasAlpha
        {
            get
            {
                return AlphaLocation != 0 && SizeOfDataAfterJpeg != 0;
            }
        }
    }
}
