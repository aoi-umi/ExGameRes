using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model
{
    public class Ajp
    {
        private AjpHeader ajpHeader;
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
                    Unknown3 = br.ReadBytes(16)
                };
            }
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
