using System.IO;
using System.Text;

namespace PFSBuilder
{
    internal struct PfsCtor
    {
        public uint Magic;
        public uint NumOfFiles;
        public uint StrTableSize;
        public uint Padding;
        public FileEntryTable[] Entries;
        public string[] StringTable;
    }

    internal struct FileEntryTable
    {
        public ulong Offset;
        public ulong Size;
        public uint StrTableOffset;
        public uint Padding;
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var InputFiles = Directory.GetFiles(args[0]);
            var Len = InputFiles.Length;
            var StringTable = new string[Len];
            var EntryTable = new FileEntryTable[Len];

            ulong CurrentFileOffset = 0;
            uint StrTableOffset = 0;

            for (int i = 0; i < Len; i++)
            {
                var Size = (ulong)new FileInfo(InputFiles[i]).Length;
                StringTable[i] = $"{Path.GetFileName(InputFiles[i])}\0";

                EntryTable[i] = new FileEntryTable()
                {
                    Offset = CurrentFileOffset,
                    Size = Size,
                    StrTableOffset = StrTableOffset,
                    Padding = 0
                };

                CurrentFileOffset += Size;
                StrTableOffset += (uint)StringTable[i].Length;
            }

            var Pfs = new PfsCtor()
            {
                Magic = 0x30534650,
                NumOfFiles = (uint)Len,
                StrTableSize = StrTableOffset,
                Padding = 0,
                Entries = EntryTable,
                StringTable = StringTable
            };

            var Output = File.OpenWrite(args[1]);
            var Buf = new BufferedStream(Output);
            var Writer = new BinaryWriter(Buf);

            Writer.Write(Pfs.Magic);
            Writer.Write(Pfs.NumOfFiles);
            Writer.Write(Pfs.StrTableSize);
            Writer.Write(Pfs.Padding);

            foreach (var Entry in Pfs.Entries)
            {
                Writer.Write(Entry.Offset);
                Writer.Write(Entry.Size);
                Writer.Write(Entry.StrTableOffset);
                Writer.Write(Entry.Padding);
            }

            for (int i = 0; i < Pfs.StringTable.Length; i++)
            {
                Writer.Write(Encoding.ASCII.GetBytes(Pfs.StringTable[i]));
            }

            for (int i = 0; i < Len; i++)
            {
                var Read = File.OpenRead(InputFiles[i]);
                var Buf2 = new BufferedStream(Read);
                Buf2.CopyTo(Buf);
                Buf2.Close();
                Read.Close();
            }

            Buf.Close();
            Writer.Close();
            Output.Close();
        }
    }
}