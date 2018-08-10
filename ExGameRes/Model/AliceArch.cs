using System;
using System.IO;
using System.Text;

namespace ExGameRes.Model
{
    public class AliceArch
    {
        public uint Version { get; }
        public uint DataOffset { get; }
        public uint EntryCount { get; }
        public uint TocLength { get; }
        public uint OriginalTocLength { get; }
        public Byte[] InfoData { get; set; }
        private AFAHDR1 afaHeader1 { get; set; }
        private AFAHDR2 afaHeader2 { get; set; }

        public AliceArch(Stream stream)
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
        }

        public static Byte[] ExtracAliceArch(AliceArch aliceArch)
        {
            uint outTocBuffLength = aliceArch.OriginalTocLength;
            return Helper.Decompress(aliceArch.InfoData, ref outTocBuffLength);
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

    public class AliceArchEntryInfo
    {
        public string Filename { get; }
        public uint Offset { get; }
        public uint Length { get; }
        private AFAENTRY1 afaEntry1 { get; set; }
        private AFAENTRY2 afaEntry2 { get; set; }

        public AliceArchEntryInfo(uint Version, BinaryReader br)
        {
            afaEntry1 = new AFAENTRY1();
            afaEntry2 = new AFAENTRY2();
            afaEntry1.FilenameLength = br.ReadUInt32();
            afaEntry1.FilenameLengthPadded = br.ReadUInt32();
            Filename = Encoding.Default.GetString(br.ReadBytes((int)afaEntry1.FilenameLength));
            if (afaEntry1.FilenameLengthPadded > afaEntry1.FilenameLength)
                br.ReadBytes((int)(afaEntry1.FilenameLengthPadded - afaEntry1.FilenameLength));

            afaEntry2.Unknow1 = br.ReadUInt32();
            afaEntry2.Unknow2 = br.ReadUInt32();
            if (Version == 1) afaEntry2.Unknow3 = br.ReadUInt32();
            Offset = afaEntry2.Offset = br.ReadUInt32();
            Length = afaEntry2.Length = br.ReadUInt32();
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
