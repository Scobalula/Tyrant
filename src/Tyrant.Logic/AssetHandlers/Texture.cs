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
using PhilLibX.Imaging;
using PhilLibX.IO;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Tyrant.Logic
{
    /// <summary>
    /// Structs and Logic for Extracting Texture Files
    /// </summary>
    public static class Texture
    {
        /// <summary>
        /// Resident Evil 7 Texture Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TextureHeaderRE7
        {
            public int Magic;
            public int Version;
            public ushort Width;
            public short Height;
            public ushort Depth;
            public byte MipMapCount;
            public byte Unknown1;
            public ScratchImage.DXGIFormat Format;
            public uint Unknown2;
            public long Padding;
        }

        /// <summary>
        /// Resident Evil 7 Mip Map
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MipMapRE7
        {
            public long Offset;
            public int Unkown;  // (Mip Width * 2) + (Mip Height * 2)
            public int Size;
        }

        /// <summary>
        /// Converts the given Texture
        /// </summary>
        public static ScratchImage Convert(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return Convert(stream);
            }
        }

        /// <summary>
        /// Converts the given Texture
        /// </summary>
        public static ScratchImage Convert(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                // Only grab the highest mip
                var header = reader.ReadStruct<TextureHeaderRE7>();
                var mip = reader.ReadArray<MipMapRE7>(header.MipMapCount).OrderBy(x => x.Size).ToArray()[header.MipMapCount - 1];

                reader.BaseStream.Position = mip.Offset;

                // Switch SRGB
                switch(header.Format)
                {
                    case ScratchImage.DXGIFormat.BC7UNORMSRGB: header.Format = ScratchImage.DXGIFormat.BC7UNORM; break;
                    case ScratchImage.DXGIFormat.BC3UNORMSRGB: header.Format = ScratchImage.DXGIFormat.BC3UNORM; break;
                    case ScratchImage.DXGIFormat.BC2UNORMSRGB: header.Format = ScratchImage.DXGIFormat.BC2UNORM; break;
                    case ScratchImage.DXGIFormat.BC1UNORMSRGB: header.Format = ScratchImage.DXGIFormat.BC1UNORM; break;
                }

                return new ScratchImage(new ScratchImage.TexMetadata()
                {
                    Width      = header.Width,
                    Height     = header.Height,
                    Depth      = 1,
                    ArraySize  = 1,
                    MiscFlags  = ScratchImage.TexMiscFlags.NONE,
                    MiscFlags2 = ScratchImage.TexMiscFlags2.NONE,
                    Dimension  = ScratchImage.TexDimension.TEXTURE2D,
                    MipLevels  = 1,
                    Format     = header.Format
                }, reader.ReadBytes(mip.Size));
            }
        }
    }
}
