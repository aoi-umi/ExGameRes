using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model.AliceSoft
{
    public class AldArch
    {
        public List<AldEntryInfo> EntryList { get; private set; }
        public AldArch(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                InitAld(fs);
            }
        }
        public AldArch(Stream stream)
        {
            InitAld(stream);
        }
        private void InitAld(Stream stream)
        {
            stream.Position = 0;
            EntryList = new List<AldEntryInfo>();
            using (BinaryReader br = new BinaryReader(stream))
            {
                var headerBytes = new Byte[] { 0 }.Concat(br.ReadBytes(3)).ToArray();
                var headerSize = BitConverter.ToInt32(headerBytes, 0);
                int maxFileCount = headerSize / 3;
                for (var i = 0; i < maxFileCount; i++)
                {
                    stream.Position = (i + 1) * 3;
                    uint address = BitConverter.ToUInt32(new Byte[] { 0 }.Concat(br.ReadBytes(3)).ToArray(), 0);
                    if (address == 0)
                        break;
                    var aldEntryInfo = new AldEntryInfo();
                    stream.Position = address;
                    uint headerLength = br.ReadUInt32();
                    aldEntryInfo.Length = br.ReadUInt32();

                    stream.Position = address;
                    var fileHeader = br.ReadBytes((int)headerLength);
                    aldEntryInfo.Offset = address + headerLength;

                    var namePos = address + 16;
                    //文件尾
                    if (namePos >= stream.Length)
                        break;
                    stream.Position = namePos;
                    var nameBytes = br.ReadBytes(16);
                    aldEntryInfo.Filename = Helper.BytesToString(nameBytes);
                    EntryList.Add(aldEntryInfo);
                }
            }
        }
    }

    public class AldEntryInfo : FileInfoModel
    {
    }
}
