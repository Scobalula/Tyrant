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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PhilLibX;
using PhilLibX.IO;

namespace Tyrant.Logic
{
    /// <summary>
    /// Structs and Logic for Extracting Motion List Files
    /// </summary>
    public class MotionList
    {
        /// <summary>
        /// Resident Evil 7 Motion List Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MotionListHeaderRE7
        {
            public uint Version;
            public uint Magic;
            public long Padding;
            public long AssetsPointer;
            public long UnkPointer;
            public long NamePointer;
            public int AssetCount;
        }

        /// <summary>
        /// Resident Evil 2 Motion List Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MotionListHeaderRE2
        {
            public uint Version;
            public uint Magic;
            public long Padding;
            public long AssetsPointer;
            public long UnkPointer;
            public long NamePointer;
            public long UnkPointer01;
            public int AssetCount;
        }

        /// <summary>
        /// Loads Motions from a Resident Evil 7 Motion List
        /// </summary>
        private static Tuple<string, List<Tuple<string, Animation>>> ConvertRE7(BinaryReader reader)
        {
            var header = reader.ReadStruct<MotionListHeaderRE7>();
            var results = new List<Tuple<string, Animation>>();

            // Sort by offsets since they don't store sizes, we also need to remove dupes as some entries point to another
            var offsets = reader.ReadArray<long>(header.AssetsPointer, header.AssetCount).Distinct().OrderBy(x => x).ToArray();

            for (int i = 0; i < offsets.Length; i++)
            {
                // Determine if this is a motion file, since lists can contain other misc. anim data
                reader.BaseStream.Position = offsets[i];
                var version = reader.ReadUInt32();
                var magic = reader.ReadUInt32();

                if (magic != 0x20746F6D)
                    continue;

                // Use the next or end to get size
                var endOffset = i == offsets.Length - 1 ? reader.BaseStream.Length : offsets[i + 1];

                reader.BaseStream.Position = offsets[i];

                using (var stream = new MemoryStream(reader.ReadBytes((int)(endOffset - offsets[i]))))
                {
                    results.Add(Motion.Convert(stream));
                }
            }

            return new Tuple<string, List<Tuple<string, Animation>>>(reader.ReadUTF16NullTerminatedString(header.NamePointer), results);
        }

        /// <summary>
        /// Loads Motions from a Resident Evil 2 Motion List
        /// </summary>
        private static Tuple<string, List<Tuple<string, Animation>>> ConvertRE2(BinaryReader reader)
        {
            var header = reader.ReadStruct<MotionListHeaderRE2>();
            var results = new List<Tuple<string, Animation>>();

            // Sort by offsets since they don't store sizes, we also need to remove dupes as some entries point to another
            var offsets = reader.ReadArray<long>(header.AssetsPointer, header.AssetCount).Distinct().OrderBy(x => x).ToArray();

            for (int i = 0; i < offsets.Length; i++)
            {
                // Determine if this is a motion file, since lists can contain other misc. anim data
                reader.BaseStream.Position = offsets[i];
                var version = reader.ReadUInt32();
                var magic = reader.ReadUInt32();

                if (magic != 0x20746F6D)
                    continue;

                // Use the next or end to get size
                var endOffset = i == offsets.Length - 1 ? reader.BaseStream.Length : offsets[i + 1];

                reader.BaseStream.Position = offsets[i];

                using (var stream = new MemoryStream(reader.ReadBytes((int)(endOffset - offsets[i]))))
                {
                    results.Add(Motion.Convert(stream));
                }
            }

            return new Tuple<string, List<Tuple<string, Animation>>>(reader.ReadUTF16NullTerminatedString(header.NamePointer), results);
        }

        /// <summary>
        /// Converts Motions from the given Motion List
        /// </summary>
        public static Tuple<string, List<Tuple<string, Animation>>> Convert(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
                return Convert(stream);
        }

        /// <summary>
        /// Converts Motions from the given Motion List
        /// </summary>
        public static Tuple<string, List<Tuple<string, Animation>>> Convert(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var version = reader.ReadUInt32();
                reader.BaseStream.Position = 0;

                switch (version)
                {
                    case 0x3C: return ConvertRE7(reader);
                    case 0x55: return ConvertRE2(reader);
                    default: throw new Exception("Invalid Motion List Version");
                }
            }
        }
    }
}
