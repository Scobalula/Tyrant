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
    /// Structs and Logic for Extracting Wwise Banks
    /// </summary>
    public static class SoundBank
    {
        private static object ConvertWwise(BinaryReader reader)
        {
            return null;

        }

        private static object ConvertPCK(BinaryReader reader)
        {
            // Skip name and header
            reader.BaseStream.Position = 32;
            var nameSize = reader.ReadInt64();

            var name = reader.ReadUTF16NullTerminatedString();

            var entries = reader.ReadInt32();

            Directory.CreateDirectory("Files");

            reader.BaseStream.Position = 40 + nameSize;

            var tableOffset = 40 + nameSize;

            for (int i = 0; i < entries; i++)
            {
                reader.BaseStream.Position = tableOffset + i * 20;

                var entryID = reader.ReadUInt32();
                var unk = reader.ReadUInt32();
                var size = reader.ReadInt32();
                var offset = reader.ReadInt64();

                reader.BaseStream.Position = offset;

                Console.WriteLine(entryID);

                File.WriteAllBytes("Files\\" + entryID.ToString("X") + ".wav", reader.ReadBytes(size));
            }
            return null;
        }
        public static object Convert(Stream stream)
        {
            // Determine if this is a PCK or BNK file
            using (var reader = new BinaryReader(stream))
            {
                var magic = reader.ReadUInt32();

                switch(magic)
                {
                    case 0x4B504B41: return ConvertPCK(reader);
                    default: throw new Exception("Invalid Sound Bank Magic");
                }
            }
        }
    }
}
