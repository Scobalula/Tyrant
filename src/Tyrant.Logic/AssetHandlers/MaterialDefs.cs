using PhilLibX;
using PhilLibX.Cryptography.Hash;
using PhilLibX.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tyrant.Logic
{
    /// <summary>
    /// A class to handle loading a Material Defs file
    /// </summary>
    public static class MaterialDefs
    {
        /// <summary>
        /// Resident Evil 7 Material Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MaterialHeaderRE7
        {
            public uint Magic;
            public ushort Version;
            public ushort MaterialCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Padding;
        }

        /// <summary>
        /// Resident Evil 7 Material Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MaterialEntryRE7
        {
            public long NamePointer;
            public uint Hash; // MurMur3
            public uint Unk01;
            public uint Unk02;
            public int SettingsBufferSize;
            public int SettingsInfoCount;
            public int TextureCount;
            public int Unk03;
            public int Unk04;
            public long SettingsInfoPointer;
            public long TexturesPointer;
            public long SettingsBufferPointer;
            public long ShaderNamePointer;
        }

        /// <summary>
        /// Resident Evil 2 Material Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MaterialEntryRE2
        {
            public long NamePointer;
            public uint Hash; // MurMur3
            public int SettingsBufferSize;
            public int SettingsInfoCount;
            public int TextureCount;
            public int Unk03;
            public int Unk04;
            public long SettingsInfoPointer;
            public long TexturesPointer;
            public long SettingsBufferPointer;
            public long ShaderNamePointer;
        }

        /// <summary>
        /// Resident Evil 7 Material Texture Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MaterialTextureEntryRE7
        {
            public long TypePointer;
            public uint TypeHash;
            public uint UnkHash;
            public long TextureNamePointer;
        }

        /// <summary>
        /// Resident Evil 7 Material Texture Entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MaterialSettingsInfoRE7
        {
            public long NamePointer;
            public uint NameHash;
            public uint UnkHash;
            public int DataCount;
            public int DataOffset; // Relative to Material.SettingsBufferPointer
        }

        /// <summary>
        /// Converts the given material file
        /// </summary>
        public static Dictionary<string, Model.Material> ConvertRE7(BinaryReader reader)
        {
            var results = new Dictionary<string, Model.Material>();

            {
                reader.BaseStream.Position = 0;
                var header = reader.ReadStruct<MaterialHeaderRE7>();
                var materials = reader.ReadArray<MaterialEntryRE7>(header.MaterialCount);

                foreach (var material in materials)
                {
                    var result = new Model.Material(reader.ReadUTF16NullTerminatedString(material.NamePointer));

                    foreach (var texture in reader.ReadArray<MaterialTextureEntryRE7>(material.TexturesPointer, material.TextureCount))
                        result.Images[reader.ReadUTF16NullTerminatedString(texture.TypePointer)] = reader.ReadUTF16NullTerminatedString(texture.TextureNamePointer).ToLower();
                    foreach (var setting in reader.ReadArray<MaterialSettingsInfoRE7>(material.SettingsInfoPointer, material.SettingsInfoCount))
                        result.Settings[reader.ReadUTF16NullTerminatedString(setting.NamePointer)] = reader.ReadArray<float>(material.SettingsBufferPointer + setting.DataOffset, setting.DataCount);

                    results[result.Name] = result;
                }
            }

            return results;
        }

        /// <summary>
        /// Converts the given material file
        /// </summary>
        public static Dictionary<string, Model.Material> ConvertRE2(BinaryReader reader)
        {
            var results = new Dictionary<string, Model.Material>();

            {
                reader.BaseStream.Position = 0;
                var header = reader.ReadStruct<MaterialHeaderRE7>();
                var materials = reader.ReadArray<MaterialEntryRE2>(header.MaterialCount);

                foreach (var material in materials)
                {
                    var result = new Model.Material(reader.ReadUTF16NullTerminatedString(material.NamePointer));

                    foreach (var texture in reader.ReadArray<MaterialTextureEntryRE7>(material.TexturesPointer, material.TextureCount))
                        result.Images[reader.ReadUTF16NullTerminatedString(texture.TypePointer)] = reader.ReadUTF16NullTerminatedString(texture.TextureNamePointer).ToLower();
                    foreach (var setting in reader.ReadArray<MaterialSettingsInfoRE7>(material.SettingsInfoPointer, material.SettingsInfoCount))
                        result.Settings[reader.ReadUTF16NullTerminatedString(setting.NamePointer)] = reader.ReadArray<float>(material.SettingsBufferPointer + setting.DataOffset, setting.DataCount);

                    results[result.Name] = result;
                }
            }

            return results;
        }

        /// <summary>
        /// Converts the given material file
        /// </summary>
        public static Dictionary<string, Model.Material> Convert(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return Convert(stream);
            }
        }

        /// <summary>
        /// Converts the given material file
        /// </summary>
        public static Dictionary<string, Model.Material> Convert(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                reader.BaseStream.Position = 28;

                // Check size of buffer
                if(reader.ReadUInt32() != 0)
                {
                    return ConvertRE2(reader);
                }
                else
                {
                    return ConvertRE7(reader);
                }
            }
        }
    }
}
