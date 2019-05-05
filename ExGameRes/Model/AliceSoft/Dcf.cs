using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model.AliceSoft
{
    public class Dcf
    {
        //4bytes  dcf 0x20
        public string Signature1 { get; set; }

        //4bytes
        public int DcfHeaderSize { get; set; }

        //4bytes
        public byte[] DcfHeader { get; set; }

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
        public Dcf(Stream stream)
        {
            InitDcf(stream);
        }

        public Dcf(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                InitDcf(stream);
            }
        }

        private void InitDcf(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                Signature1 = Helper.BytesToString(br.ReadBytes(4));
                if (Signature1.Substring(0, 3) != Config.Signature.DCF)
                    throw new MyException(Config.Signature.DCF, MyException.ErrorTypeEnum.FileTypeError);
                DcfHeaderSize = br.ReadInt32();
                DcfHeader = br.ReadBytes(DcfHeaderSize);

                Signature2 = Helper.BytesToString(br.ReadBytes(4));
                DFDLSize = br.ReadInt32();
                DFDLDataOrgSize = br.ReadInt32();
                DFDLData = br.ReadBytes(DFDLSize - 4);
                uint outSize = (uint)DFDLDataOrgSize;
                MaskData = Helper.Decompress(DFDLData, ref outSize);

                Signature3 = Helper.BytesToString(br.ReadBytes(4));
                DCGDSize = br.ReadInt32();
                DCGDData = br.ReadBytes(DCGDSize);
            }
        }
    }
}
