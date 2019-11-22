// ------------------------------------------------------------------------
// Tyrant - RE Engine Extractor
// Copyright (C) 2018 Philip/Scobalula
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ------------------------------------------------------------------------
using PhilLibX.Compression;
using PhilLibX.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Tyrant.Logic
{
    /// <summary>
    /// A class to hold and process RE Engine Package File
    /// </summary>
    public class Package : IDisposable
    {
        /// <summary>
        /// Package File Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Entry
        {
            /// <summary>
            /// Gets or Sets the lower case hash
            /// </summary>
            public uint LowerCaseHash { get; set; }

            /// <summary>
            /// Gets or Sets the upper case hash
            /// </summary>
            public uint UpperCaseHash { get; set; }

            /// <summary>
            /// Gets or Sets the offset of the data
            /// </summary>
            public long Offset { get; set; }

            /// <summary>
            /// Gets or Sets the compressed size
            /// </summary>
            public long CompressedSize { get; set; }

            /// <summary>
            /// Gets or Sets the decompressed size
            /// </summary>
            public long DecompressedSize { get; set; }

            /// <summary>
            /// Gets or Sets the Flags
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
            public byte[] Flags;

            /// <summary>
            /// Gets or Sets the Checksum
            /// </summary>
            public long Checksum { get; set; }
        }

        /// <summary>
        /// Gets or Sets the File Reader
        /// </summary>
        private BinaryReader Reader { get; set; }

        /// <summary>
        /// Gets or Sets the Entries
        /// </summary>
        public Dictionary<uint, Entry> Entries { get; set; }

        /// <summary>
        /// Creates a new Package File and loads from the given file
        /// </summary>
        public Package(string filePath)
        {
            Reader = new BinaryReader(File.OpenRead(filePath));

            var magic    = Reader.ReadUInt32();
            var version  = Reader.ReadUInt32();
            var count    = Reader.ReadInt32();
            var checksum = Reader.ReadUInt32();

            if (magic != 0x414B504B)
                throw new Exception("Invalid Package Magic");

            Entries = new Dictionary<uint, Entry>();

            switch (version)
            {
                case 0x4:
                    {
                        var entries = Reader.ReadArray<Entry>(count);
                        foreach (var entry in entries)
                            Entries[entry.LowerCaseHash] = entry;
                        break;
                    }
                default:
                    throw new Exception("Invalid Package Version");
            }
        }

        /// <summary>
        /// Loads the data for the given entry
        /// </summary>
        public byte[] LoadEntry(Entry entry)
        {
            lock(Reader)
            {
                Reader.BaseStream.Position = entry.Offset;

                switch (entry.Flags[0] & 0x0F)
                {
                    case 0:
                        return Reader.ReadBytes((int)entry.CompressedSize);
                    case 1:
                        return ZLIB.Decompress(Reader.ReadBytes((int)entry.CompressedSize), -15);
                    case 2:
                        return ZStandard.Decompress(Reader.ReadBytes((int)entry.CompressedSize));
                    default:
                        throw new Exception("Invalid Entry Compression");
                }
            }
        }

        /// <summary>
        /// Disposes of the File Reader
        /// </summary>
        public void Dispose()
        {
            Reader?.Dispose();
        }
    }
}
