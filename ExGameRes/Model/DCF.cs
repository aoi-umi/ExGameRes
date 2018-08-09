using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model
{
    public class DCF
    {
        //4bytes  dcf 0x20
        public string Signature1 { get; set; }

        //4bytes
        public int DCFHeaderSize { get; set; }

        //4bytes
        public byte[] DCFHeader { get; set; }

        //4bytes dfdl
        public string Signature2 { get; set; }

        //4bytes
        public int DFDLSize { get; set; }

        //4bytes
        public int DFDLDataOrgSize { get; set; }

        public byte[] DFDLData { get; set; }

        //4bytes dcgd
        public string Signature3 { get; set; }

        //4bytes
        public int DCGDSize { get; set; }

        //QNT data
        public byte[] DCGDData { get; set; }


        public byte[] MaskData { get; set; }
        public DCF(Stream stream)
        {
            InitDCF(stream);
        }

        public DCF(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                InitDCF(stream);
            }
        }

        private void InitDCF(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                Signature1 = Encoding.Default.GetString(br.ReadBytes(4));
                if (Signature1.Substring(0, 3) != Config.Signature.DCF)
                    Helper.ThrowException(Config.Signature.DCF, Helper.ExceptionErrorTypeEnum.FileTypeError);
                DCFHeaderSize = br.ReadInt32();
                DCFHeader = br.ReadBytes(DCFHeaderSize);

                Signature2 = Encoding.Default.GetString(br.ReadBytes(4));
                DFDLSize = br.ReadInt32();
                DFDLDataOrgSize = br.ReadInt32();
                DFDLData = br.ReadBytes(DFDLSize - 4);
                uint outSize = (uint)DFDLDataOrgSize;
                MaskData = new Byte[DFDLDataOrgSize];
                Helper.uncompress(MaskData, ref outSize, DFDLData, (uint)DFDLData.Length);

                Signature3 = Encoding.Default.GetString(br.ReadBytes(4));
                DCGDSize = br.ReadInt32();
                DCGDData = br.ReadBytes(DCGDSize);
            }
        }
    }
}
