using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExGameRes.Model.AliceSoft
{
    public class AfaArch
    {
        public uint Version { get; private set; }
        public uint DataOffset { get; private set; }
        public uint EntryCount { get; private set; }
        public uint TocLength { get; private set; }
        public uint OriginalTocLength { get; private set; }
        public List<AfaEntryInfo> EntryList { get; private set; }
        private Byte[] InfoData { get; set; }
        private AfaHdr1 AfaHeader1 { get; set; }
        private AfaHdr2 AfaHeader2 { get; set; }

        public AfaArch(Stream stream)
        {
            InitAfa(stream);
        }

        public AfaArch(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                InitAfa(fs);
            }
        }
        private void InitAfa(Stream stream)
        {
            AfaHeader1 = new AfaHdr1();
            AfaHeader2 = new AfaHdr2();
            using (BinaryReader br = new BinaryReader(stream))
            {
                AfaHeader1.Signature = Helper.BytesToString(br.ReadBytes(4));
                if (AfaHeader1.Signature != Config.Signature.AFAH)
                    throw new MyException(Config.Signature.AFAH, MyException.ErrorTypeEnum.FileTypeError);
                AfaHeader1.Length = br.ReadUInt32();
                AfaHeader1.Signature2 = Helper.BytesToString(br.ReadBytes(8));
                if (AfaHeader1.Signature2 != Config.Signature.AlicArch)
                    throw new MyException(Config.Signature.AlicArch, MyException.ErrorTypeEnum.FileTypeError);
                Version = AfaHeader1.Version = br.ReadUInt32();
                AfaHeader1.Unknow = br.ReadUInt32();
                DataOffset = AfaHeader1.Offset = br.ReadUInt32();

                AfaHeader2.Signature = Helper.BytesToString(br.ReadBytes(4));
                TocLength = AfaHeader2.TocLength = br.ReadUInt32();
                OriginalTocLength = AfaHeader2.OriginalTocLength = br.ReadUInt32();
                EntryCount = AfaHeader2.EntryCount = br.ReadUInt32();

                InfoData = br.ReadBytes((int)AfaHeader2.TocLength);
            }

            //获取文件
            uint outTocBuffLength = OriginalTocLength;
            Byte[] outTocBuff = Helper.Decompress(InfoData, ref outTocBuffLength);
            EntryList = new List<AfaEntryInfo>();
            using (MemoryStream ms = new MemoryStream(outTocBuff))
            using (BinaryReader br = new BinaryReader(ms))
            {
                for (int i = 0; i < EntryCount; i++)
                {
                    EntryList.Add(GetAfaEntryInfo(Version, br));
                }

            }
        }

        private AfaEntryInfo GetAfaEntryInfo(uint Version, BinaryReader br)
        {
            var afaEntryInfo = new AfaEntryInfo();
            var afaEntry1 = afaEntryInfo.AfaEntry1;
            var afaEntry2 = afaEntryInfo.AfaEntry2;
            afaEntry1.FilenameLength = br.ReadUInt32();
            afaEntry1.FilenameLengthPadded = br.ReadUInt32();
            afaEntryInfo.Filename = Helper.BytesToString(br.ReadBytes((int)afaEntry1.FilenameLength));
            if (afaEntry1.FilenameLengthPadded > afaEntry1.FilenameLength)
                br.ReadBytes((int)(afaEntry1.FilenameLengthPadded - afaEntry1.FilenameLength));

            afaEntry2.Unknow1 = br.ReadUInt32();
            afaEntry2.Unknow2 = br.ReadUInt32();
            if (Version == 1)
                afaEntry2.Unknow3 = br.ReadUInt32();
            afaEntry2.Offset = br.ReadUInt32();
            afaEntryInfo.Offset = afaEntry2.Offset + DataOffset;
            afaEntryInfo.Length = afaEntry2.Length = br.ReadUInt32();
            return afaEntryInfo;
        }
    }

    public class AfaHdr1
    {
        //4 bytes
        public string Signature { get; set; }
        //4 bytes
        public uint Length { get; set; }
        //8 bytes
        public string Signature2 { get; set; }
        //4 bytes
        public uint Version { get; set; }
        //4 bytes
        public uint Unknow { get; set; }
        //4 bytes
        public uint Offset { get; set; }
    }

    public class AfaHdr2
    {
        //4 bytes
        public string Signature { get; set; }
        //4 bytes
        public uint TocLength { get; set; }
        //4 bytes
        public uint OriginalTocLength { get; set; }
        //4 bytes
        public uint EntryCount { get; set; }
    }

    public class AfaEntryInfo : FileInfoModel
    {
        public AFAENTRY1 AfaEntry1 { get; set; }
        public AFAENTRY2 AfaEntry2 { get; set; }

        public AfaEntryInfo()
        {
            AfaEntry1 = new AFAENTRY1();
            AfaEntry2 = new AFAENTRY2();
        }
    }

    public class AFAENTRY1
    {
        //4 bytes
        public uint FilenameLength { get; set; }
        //4 bytes
        public uint FilenameLengthPadded { get; set; }
    }

    public class AFAENTRY2
    {
        //4 bytes
        public uint Unknow1 { get; set; }
        //4 bytes
        public uint Unknow2 { get; set; }
        //4 bytes
        public uint Unknow3 { get; set; }
        //4 bytes
        public uint Offset { get; set; }
        //4 bytes
        public uint Length { get; set; }
    }
}
