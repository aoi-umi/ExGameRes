using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExGameRes.Model
{
    public class FileInfoModel
    {
        public string FilePath { get; set; }
        public string Filename { get; set; }
        public uint Offset { get; set; }
        public uint Length { get; set; }
    }
}
