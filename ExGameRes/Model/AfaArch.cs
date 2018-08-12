using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExGameRes.Model
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
        private AFAHDR1 afaHeader1 { get; set; }
        private AFAHDR2 afaHeader2 { get; set; }

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
            afaHeader1 = new AFAHDR1();
            afaHeader2 = new AFAHDR2();
            using (BinaryReader br = new BinaryReader(stream))
            {
                afaHeader1.Signature = Encoding.Default.GetString(br.ReadBytes(4));
                if (afaHeader1.Signature != Config.Signature.AFAH)
                    throw new MyException(Config.Signature.AFAH, MyException.ErrorTypeEnum.FileTypeError);
                afaHeader1.Length = br.ReadUInt32();
                afaHeader1.Signature2 = Encoding.Default.GetString(br.ReadBytes(8));
                if (afaHeader1.Signature2 != Config.Signature.AlicArch)
                    throw new MyException(Config.Signature.AlicArch, MyException.ErrorTypeEnum.FileTypeError);
                Version = afaHeader1.Version = br.ReadUInt32();
                afaHeader1.Unknow = br.ReadUInt32();
                DataOffset = afaHeader1.Offset = br.ReadUInt32();

                afaHeader2.Signature = Encoding.Default.GetString(br.ReadBytes(4));
                TocLength = afaHeader2.TocLength = br.ReadUInt32();
                OriginalTocLength = afaHeader2.OriginalTocLength = br.ReadUInt32();
                EntryCount = afaHeader2.EntryCount = br.ReadUInt32();

                InfoData = br.ReadBytes((int)afaHeader2.TocLength);
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
            var afaEntry1 = afaEntryInfo.afaEntry1;
            var afaEntry2 = afaEntryInfo.afaEntry2;
            afaEntry1.FilenameLength = br.ReadUInt32();
            afaEntry1.FilenameLengthPadded = br.ReadUInt32();
            afaEntryInfo.Filename = Encoding.Default.GetString(br.ReadBytes((int)afaEntry1.FilenameLength));
            if (afaEntry1.FilenameLengthPadded > afaEntry1.FilenameLength)
                br.ReadBytes((int)(afaEntry1.FilenameLengthPadded - afaEntry1.FilenameLength));

            afaEntry2.Unknow1 = br.ReadUInt32();
            afaEntry2.Unknow2 = br.ReadUInt32();
            if (Version == 1)
                afaEntry2.Unknow3 = br.ReadUInt32();
            afaEntryInfo.Offset = afaEntry2.Offset = br.ReadUInt32();
            afaEntryInfo.Length = afaEntry2.Length = br.ReadUInt32();
            return afaEntryInfo;
        }
    }

    public class AFAHDR1
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

    public class AFAHDR2
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

    public class AfaEntryInfo
    {
        public string Filename { get; set; }
        public uint Offset { get; set; }
        public uint Length { get; set; }
        public AFAENTRY1 afaEntry1 { get; set; }
        public AFAENTRY2 afaEntry2 { get; set; }

        public AfaEntryInfo()
        {
            afaEntry1 = new AFAENTRY1();
            afaEntry2 = new AFAENTRY2();
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
