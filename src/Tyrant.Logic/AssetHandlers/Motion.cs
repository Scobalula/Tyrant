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
using System.Runtime.InteropServices;
using PhilLibX;
using PhilLibX.Mathematics;
using PhilLibX.IO;

namespace Tyrant.Logic
{
    /// <summary>
    /// Data Decompressor Alias (for Vector3/Quat Decompressors)
    /// </summary>
    using DataDecompressorList = Dictionary<uint, Action<BinaryReader, Animation.Bone, int[], float[], long>>;

    /// <summary>
    /// Structs and Logic for Extracting Motion Files
    /// </summary>
    public static class Motion
    {
        /// <summary>
        /// Animation Data Precense Flags
        /// </summary>
        internal enum DataPrecenseFlags : ushort
        {
            Translations = 1 << 0,
            Rotations    = 1 << 1,
            Scales       = 1 << 2,
        };

        /// <summary>
        /// Resident Evil 7 Motion Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MotionHeaderRE7
        {
            public uint Version;
            public uint Magic;
            public long Padding;
            public long BoneBaseDataPointer;
            public long BoneDataPointer;
            public long UnkPointer;
            public long UnkPointer1;
            public long UnkPointer2;
            public long UnkPointer3;
            public long UnkPointer4;
            public long UnkPointer5;
            public long NamePointer;
            public float FrameCount;
            public float UnkFloat;
            public float UnkFloat1;
            public float UnkFloat2;
            public ushort BoneCount;
            public ushort BoneDataCount;
            public byte UnkPointer2Count;
            public byte UnkPointer3Count;
            public ushort Unk;
            public ushort UnkPointerCount;
            public ushort Unk1;
        }

        /// <summary>
        /// Resident Evil 2 Motion Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MotionHeaderRE2
        {
            public uint Version;
            public uint Magic;
            public long Padding;
            public long BoneBaseDataPointer;
            public long BoneDataPointer;
            public long UnkPointer;
            public long UnkPointer1;
            public long UnkPointer2;
            public long UnkPointer3;
            public long UnkPointer4;
            public long UnkPointer5;
            public long NamePointer;
            public float FrameCount;
            public float UnkFloat;
            public float UnkFloat1;
            public float UnkFloat2;
            public ushort BoneCount;
            public ushort BoneDataCount;
            public byte UnkPointer2Count;
            public byte UnkPointer3Count;
            public ushort Unk;
            public ushort UnkPointerCount;
            public ushort Unk1;
        }

        /// <summary>
        /// Resident Evil 7 Bone Base Data
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BoneBaseDataRE7
        {
            public long NamePointer;
            public long UnkPointer;
            public long UnkPointer1;
            public long UnkPointer2; // These 3 seem to point to other bones
            public Vector4 Translation;
            public Quaternion Rotation;
            public uint Index;
            public uint Hash; // MurMur3
            public long Padding2;
        }

        /// <summary>
        /// Resident Evil 7 Bone Data
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BoneDataRE7
        {
            public ushort BoneIndex;
            public DataPrecenseFlags Flags;
            public uint BoneHash; // MurMur3;
            public long KeysPointer;
        }

        /// <summary>
        /// Resident Evil 2 Bone Data
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BoneDataRE2
        {
            public ushort BoneIndex;
            public DataPrecenseFlags Flags;
            public uint BoneHash; // MurMur3;
            public float Unk; // Always 1.0?
            public int Padding;
            public long KeysPointer;
        }

        /// <summary>
        /// Resident Evil 3 Bone Data
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BoneDataRE3
        {
            public ushort BoneIndex;
            public DataPrecenseFlags Flags;
            public uint BoneHash; // MurMur3;
            public int KeysPointer;
        }

        /// <summary>
        /// Resident Evil 7 Key Data (per channel per bone)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct KeyDataRE7
        {
            public uint Flags;
            public int KeyCount;
            public int Unk;
            public float MaxFrame;
            public long FramesPointer;
            public long DataPointer;
            public long UnpackDataPointer;
        }

        /// <summary>
        /// Resident Evil 3 Key Data (per channel per bone)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct KeyDataRE3
        {
            public uint Flags;
            public int KeyCount;
            public int FramesPointer;
            public int DataPointer;
            public int UnpackDataPointer;
        }

        /// <summary>
        /// Packed 16-Bit Vector 3
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PackedVector3
        {
            public ushort X;
            public ushort Y;
            public ushort Z;
        }

        /// <summary>
        /// Vector 3 Decompressors, used by Translations and Scales
        /// </summary>
        private static DataDecompressorList Vector3Decompressors = new DataDecompressorList()
        {
            { 0x00000, LoadVector3sFull },
            { 0x20000, LoadVector3s5BitA },
            { 0x30000, LoadVector3s10BitA },
            { 0x40000, LoadVector3s10BitA },
            { 0x70000, LoadVector3s21BitA },
            { 0x31000, LoadVector3sXAxis },
            { 0x32000, LoadVector3sYAxis },
            { 0x33000, LoadVector3sZAxis },
            { 0x21000, LoadVector3sXAxis16Bit },
            { 0x22000, LoadVector3sYAxis16Bit },
            { 0x23000, LoadVector3sZAxis16Bit },
        };

        /// <summary>
        /// Vector 3 Decompressors, used by Translations and Scales for RE3
        /// </summary>
        private static DataDecompressorList Vector3DecompressorsRE3 = new DataDecompressorList()
        {
            { 0x00000,             LoadVector3sFull            },
            { 0x20000,             LoadVector3s5BitB           },
            { 0x30000,             LoadVector3s5BitB           },
            { 0x40000,             LoadVector3s10BitB          },
            { 0x80000,             LoadVector3s21BitB          },
            { 0x21000,             LoadVector3sXAxis16Bit      },
            { 0x22000,             LoadVector3sYAxis16Bit      },
            { 0x23000,             LoadVector3sZAxis16Bit      },
            { 0x24000,             LoadVector3sXYZAxis16Bit    },
            { 0x41000,             LoadVector3sXAxis           },
            { 0x42000,             LoadVector3sYAxis           },
            { 0x43000,             LoadVector3sZAxis           },
            { 0x44000,             LoadVector3sXYZAxis         },
        };

        /// <summary>
        /// Quaternion Decompressors, used by Rotations
        /// </summary>
        private static DataDecompressorList QuaternionDecompressors = new DataDecompressorList()
        {
            { 0x00000, LoadQuaternionsFull },
            { 0xB0000, LoadQuaternions3Component },
            { 0xC0000, LoadQuaternions3Component },
            { 0x30000, LoadQuaternions10Bit },
            { 0x40000, LoadQuaternions10Bit },
            { 0x50000, LoadQuaternions16Bit },
            { 0x70000, LoadQuaternions21Bit },
            { 0x21000, LoadQuaternionsXAxis16Bit },
            { 0x22000, LoadQuaternionsYAxis16Bit },
            { 0x23000, LoadQuaternionsZAxis16Bit },
            { 0x31000, LoadQuaternionsXAxis },
            { 0x41000, LoadQuaternionsXAxis },
            { 0x32000, LoadQuaternionsYAxis },
            { 0x42000, LoadQuaternionsYAxis },
            { 0x33000, LoadQuaternionsZAxis },
            { 0x43000, LoadQuaternionsZAxis },
        };

        /// <summary>
        /// Quaternion Decompressors, used by Rotations
        /// </summary>
        private static DataDecompressorList QuaternionDecompressorsRE3 = new DataDecompressorList()
        {
            { 0x00000, LoadQuaternionsFull },
            { 0xB0000, LoadQuaternions3Component },
            { 0xC0000, LoadQuaternions3Component },
            { 0x20000, LoadQuaternions5Bit },
            { 0x30000, LoadQuaternions8Bit },
            { 0x40000, LoadQuaternions10Bit },
            { 0x50000, LoadQuaternions13Bit },
            { 0x60000, LoadQuaternions16Bit },
            { 0x70000, LoadQuaternions18Bit },
            { 0x80000, LoadQuaternions21Bit },
            { 0x21000, LoadQuaternionsXAxis16Bit },
            { 0x22000, LoadQuaternionsYAxis16Bit },
            { 0x23000, LoadQuaternionsZAxis16Bit },
            { 0x31000, LoadQuaternionsXAxis },
            { 0x41000, LoadQuaternionsXAxis },
            { 0x32000, LoadQuaternionsYAxis },
            { 0x42000, LoadQuaternionsYAxis },
            { 0x33000, LoadQuaternionsZAxis },
            { 0x43000, LoadQuaternionsZAxis },
        };

        /// <summary>
        /// Loads Vector3s with all components
        /// </summary>
        private static void LoadVector3sFull(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<Vector3>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = data[i].X;
                var y = data[i].Y;
                var z = data[i].Z;

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads 5Bit Vector3s
        /// </summary>
        private static void LoadVector3s5BitA(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * ((data[i] >> 00) & 0x1F) / 31.0f) + unpackData[4];
                var y = (unpackData[1] * ((data[i] >> 05) & 0x1F) / 31.0f) + unpackData[5];
                var z = (unpackData[2] * ((data[i] >> 10) & 0x1F) / 31.0f) + unpackData[6];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads 5Bit Vector3s
        /// </summary>
        private static void LoadVector3s5BitB(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * ((data[i] >> 00) & 0x1F) / 31.0f) + unpackData[3];
                var y = (unpackData[1] * ((data[i] >> 05) & 0x1F) / 31.0f) + unpackData[4];
                var z = (unpackData[2] * ((data[i] >> 10) & 0x1F) / 31.0f) + unpackData[5];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads 10Bit Vector3s
        /// </summary>
        private static void LoadVector3s10BitA(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<uint>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[0] * (((data[i] >> 00) & 0x3FF) * (1.0f / 0x3FF)) + unpackData[4];
                var y = unpackData[1] * (((data[i] >> 10) & 0x3FF) * (1.0f / 0x3FF)) + unpackData[5];
                var z = unpackData[2] * (((data[i] >> 20) & 0x3FF) * (1.0f / 0x3FF)) + unpackData[6];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads 10Bit Vector3s
        /// </summary>
        private static void LoadVector3s10BitB(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<uint>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[0] * (((data[i] >> 00) & 0x3FF) * (1.0f / 0x3FF)) + unpackData[3];
                var y = unpackData[1] * (((data[i] >> 10) & 0x3FF) * (1.0f / 0x3FF)) + unpackData[4];
                var z = unpackData[2] * (((data[i] >> 20) & 0x3FF) * (1.0f / 0x3FF)) + unpackData[5];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads 21Bit Vector3s
        /// </summary>
        private static void LoadVector3s21BitA(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ulong>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * ((data[i] >> 00) & 0x1FFFFF) / 2097151.0f) + unpackData[4];
                var y = (unpackData[1] * ((data[i] >> 21) & 0x1FFFFF) / 2097151.0f) + unpackData[5];
                var z = (unpackData[2] * ((data[i] >> 42) & 0x1FFFFF) / 2097151.0f) + unpackData[6];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads 21Bit Vector3s
        /// </summary>
        private static void LoadVector3s21BitB(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ulong>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * ((data[i] >> 00) & 0x1FFFFF) / 2097151.0f) + unpackData[3];
                var y = (unpackData[1] * ((data[i] >> 21) & 0x1FFFFF) / 2097151.0f) + unpackData[4];
                var z = (unpackData[2] * ((data[i] >> 42) & 0x1FFFFF) / 2097151.0f) + unpackData[5];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on X Axis
        /// </summary>
        private static void LoadVector3sXAxis(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<float>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = data[i];
                var y = unpackData[1];
                var z = unpackData[2];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on Y Axis
        /// </summary>
        private static void LoadVector3sYAxis(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<float>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[0];
                var y = data[i];
                var z = unpackData[2];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on Z Axis
        /// </summary>
        private static void LoadVector3sZAxis(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<float>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[0];
                var y = unpackData[1];
                var z = data[i];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on Z Axis
        /// </summary>
        private static void LoadVector3sXYZAxis(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<float>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = data[i];
                var y = data[i];
                var z = data[i];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on X Axis
        /// </summary>
        private static void LoadVector3sXAxis16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[0] * (data[i] / 65535.0f) + unpackData[1];
                var y = unpackData[2];
                var z = unpackData[3];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on Y Axis
        /// </summary>
        private static void LoadVector3sYAxis16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[1];
                var y = unpackData[0] * (data[i] / 65535.0f) + unpackData[2];
                var z = unpackData[3];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on Y Axis
        /// </summary>
        private static void LoadVector3sZAxis16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[1];
                var y = unpackData[2];
                var z = unpackData[0] * (data[i] / 65535.0f) + unpackData[3];

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Vector3s with 1 Component on Y Axis
        /// </summary>
        private static void LoadVector3sXYZAxis16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var val = unpackData[0] * (data[i] / 65535.0f) + unpackData[3];

                var x = val;
                var y = val;
                var z = val;

                bone.Translations[frames[i]] = new Vector3(x, y, z);
            }
        }

        /// <summary>
        /// Loads Quaternions with all components
        /// </summary>
        private static void LoadQuaternionsFull(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<Quaternion>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = data[i].X;
                var y = data[i].Y;
                var z = data[i].Z;
                var w = data[i].W;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);

            }
        }

        /// <summary>
        /// Loads Quaternions with 3 Components
        /// </summary>
        private static void LoadQuaternions3Component(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<Vector3>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = data[i].X;
                var y = data[i].Y;
                var z = data[i].Z;
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);

            }
        }

        /// <summary>
        /// Loads 10Bit Quaternions
        /// </summary>
        private static void LoadQuaternions5Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * ((data[i] >> 00) & 0x1F) * (1.0f / 0x1F)) + unpackData[4];
                var y = (unpackData[1] * ((data[i] >> 05) & 0x1F) * (1.0f / 0x1F)) + unpackData[5];
                var z = (unpackData[2] * ((data[i] >> 10) & 0x1F) * (1.0f / 0x1F)) + unpackData[6];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads 10Bit Quaternions
        /// </summary>
        private static void LoadQuaternions8Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            reader.BaseStream.Position = dataPointer;

            for (int i = 0; i < frames.Length; i++)
            {
                var x = (unpackData[0] * (reader.ReadByte() * 0.000015259022f)) + unpackData[4];
                var y = (unpackData[1] * (reader.ReadByte() * 0.000015259022f)) + unpackData[5];
                var z = (unpackData[2] * (reader.ReadByte() * 0.000015259022f)) + unpackData[6];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads 10Bit Quaternions
        /// </summary>
        private static void LoadQuaternions10Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<uint>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * ((data[i] >> 00) & 0x3FF) / 1023.0f) + unpackData[4];
                var y = (unpackData[1] * ((data[i] >> 10) & 0x3FF) / 1023.0f) + unpackData[5];
                var z = (unpackData[2] * ((data[i] >> 20) & 0x3FF) / 1023.0f) + unpackData[6];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads 10Bit Quaternions
        /// </summary>
        private static void LoadQuaternions16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<PackedVector3>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * (data[i].X / 65535.0f)) + unpackData[4];
                var y = (unpackData[1] * (data[i].Y / 65535.0f)) + unpackData[5];
                var z = (unpackData[2] * (data[i].Z / 65535.0f)) + unpackData[6];
                var w = (float)Math.Sqrt(Math.Abs(1 - x * x - y * y - z * z));

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads 10Bit Quaternions
        /// </summary>
        private static void LoadQuaternions13Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            reader.BaseStream.Position = dataPointer;

            for (int i = 0; i < frames.Length; i++)
            {
                var data = reader.ReadBytes(5);
                ulong val = 0;
                for(int j = 0; j < 5; j++)
                    val = data[j] | (val << 8);

                var x = (unpackData[0] * ((val >> 00) & 0x1FFF) * 0.00012208521f) + unpackData[4];
                var y = (unpackData[1] * ((val >> 13) & 0x1FFF) * 0.00012208521f) + unpackData[5];
                var z = (unpackData[2] * ((val >> 26) & 0x1FFF) * 0.00012208521f) + unpackData[6];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads 18Bit Quaternions
        /// </summary>
        private static void LoadQuaternions18Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                var data = reader.ReadBytes(7);
                ulong val = 0;
                for (int j = 0; j < 7; j++)
                    val = data[j] | (val << 8);

                var x = (unpackData[0] * ((val >> 00) & 0x1FFF) * 0.00012208521f) + unpackData[4];
                var y = (unpackData[1] * ((val >> 13) & 0x1FFF) * 0.00012208521f) + unpackData[5];
                var z = (unpackData[2] * ((val >> 26) & 0x1FFF) * 0.00012208521f) + unpackData[6];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads 21Bit Quaternions
        /// </summary>
        private static void LoadQuaternions21Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ulong>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = (unpackData[0] * ((data[i] >> 00) & 0x1FFFFF) / 2097151.0f) + unpackData[4];
                var y = (unpackData[1] * ((data[i] >> 21) & 0x1FFFFF) / 2097151.0f) + unpackData[5];
                var z = (unpackData[2] * ((data[i] >> 42) & 0x1FFFFF) / 2097151.0f) + unpackData[6];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads Quaternions with 1 Component on X Axis
        /// </summary>
        private static void LoadQuaternionsXAxis(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<float>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = data[i];
                var y = 0.0f;
                var z = 0.0f;
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads Quaternions with 1 Component on Y Axis
        /// </summary>
        private static void LoadQuaternionsYAxis(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<float>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = 0.0f;
                var y = data[i];
                var z = 0.0f;
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads Quaternions with 1 Component on Z Axis
        /// </summary>
        private static void LoadQuaternionsZAxis(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<float>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = 0.0f;
                var y = 0.0f;
                var z = data[i];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }


        /// <summary>
        /// Loads Quaternions with 1 Component on X Axis
        /// </summary>
        private static void LoadQuaternionsXAxis16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = unpackData[0] * (data[i] / 65535.0f) + unpackData[1];
                var y = 0.0f;
                var z = 0.0f;
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads Quaternions with 1 Component on Y Axis
        /// </summary>
        private static void LoadQuaternionsYAxis16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = 0.0f;
                var y = unpackData[0] * (data[i] / 65535.0f) + unpackData[1];
                var z = 0.0f;
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads Quaternions with 1 Component on Z Axis
        /// </summary>
        private static void LoadQuaternionsZAxis16Bit(BinaryReader reader, Animation.Bone bone, int[] frames, float[] unpackData, long dataPointer)
        {
            var data = reader.ReadArray<ushort>(dataPointer, frames.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var x = 0.0f;
                var y = 0.0f;
                var z = unpackData[0] * (data[i] / 65535.0f) + unpackData[1];
                var w = 1.0f - (x * x + y * y + z * z);

                if (w > 0.0f)
                    w = (float)Math.Sqrt(w);
                else
                    w = 0.0f;

                bone.Rotations[frames[i]] = new Quaternion(x, y, z, w);
            }
        }

        /// <summary>
        /// Loads Frame Indices at the given offset of the given type
        /// </summary>
        private static int[] LoadFrames(BinaryReader reader, long framesPointer, int frameCount, uint frameDataType)
        {

            if(frameCount < 2)
            {
                return new int[]
                {
                    0
                };
            }

            var results = new int[frameCount];

            reader.BaseStream.Position = framesPointer;

            switch (frameDataType)
            {
                case 0x200000:
                    {
                        for (int i = 0; i < frameCount; i++)
                            results[i] = reader.ReadByte();
                        break;
                    }
                case 0x400000:
                    {
                        for (int i = 0; i < frameCount; i++)
                            results[i] = reader.ReadUInt16();
                        break;
                    }
                case 0x500000:
                    {
                        for (int i = 0; i < frameCount; i++)
                            results[i] = reader.ReadInt32();
                        break;
                    }
                default:
                    {
                        for (int i = 0; i < frameCount; i++)
                            results[i] = (int)reader.ReadSingle();
                        break;
                    }
            }

            return results;
        }

        /// <summary>
        /// Converts an RE7 Motion File to a SEAnim
        /// </summary>
        private static Tuple<string, Animation> ConvertRE7(BinaryReader reader)
        {
            var animation = new Animation(Animation.DataType.Absolute);

            var header = reader.ReadStruct<MotionHeaderRE7>();

            var bones = new Dictionary<uint, Animation.Bone>(header.BoneCount);

            // Check for bone base data (this is required, so if it doesn't exit, quit)
            if (header.BoneBaseDataPointer > 0)
            {
                reader.BaseStream.Position = header.BoneBaseDataPointer;

                var baseDataOffset = reader.ReadInt64();
                var baseDataCount = reader.ReadInt32();

                var size = Marshal.SizeOf<BoneBaseDataRE7>();

                var boneBaseData = reader.ReadArray<BoneBaseDataRE7>(baseDataOffset, baseDataCount);

                foreach (var boneBase in boneBaseData)
                {
                    var bone = new Animation.Bone(reader.ReadUTF16NullTerminatedString(boneBase.NamePointer));

                    bone.Translations[0] = boneBase.Translation.ToVector3();
                    bone.Rotations[0] = boneBase.Rotation;
                    bones[boneBase.Hash] = bone;
                }

                if (header.BoneDataPointer > 0)
                {
                    var boneData = reader.ReadArray<BoneDataRE7>(header.BoneDataPointer, header.BoneDataCount);

                    foreach (var bone in boneData)
                    {
                        var keyDataOffset = bone.KeysPointer;

                        if (bone.Flags.HasFlag(DataPrecenseFlags.Translations))
                        {
                            var keyData = reader.ReadStruct<KeyDataRE7>(keyDataOffset);

                            float[] unpackData = null;
                            uint keyFrameDataType = keyData.Flags & 0xF00000;
                            uint compression = keyData.Flags & 0xFF000;
                            uint unkFlag = keyData.Flags & 0xFFF;

                            if (keyData.UnpackDataPointer > 0)
                                unpackData = reader.ReadArray<float>(keyData.UnpackDataPointer, 8);

                            var keyFrames = LoadFrames(reader, keyData.FramesPointer, keyData.KeyCount, keyFrameDataType);

                            if (Vector3Decompressors.TryGetValue(compression, out var decompressionMethod))
                                decompressionMethod(reader, bones[bone.BoneHash], keyFrames, unpackData, keyData.DataPointer);
                            else
                                throw new Exception(string.Format("Unknown Vector3 compression type: 0x{0:X}", compression));

                            keyDataOffset += Marshal.SizeOf<KeyDataRE7>();
                        }

                        if (bone.Flags.HasFlag(DataPrecenseFlags.Rotations))
                        {
                            var keyData = reader.ReadStruct<KeyDataRE7>(keyDataOffset);

                            float[] unpackData = null;
                            uint keyFrameDataType = keyData.Flags & 0xF00000;
                            uint compression = keyData.Flags & 0xFF000;
                            uint unkFlag = keyData.Flags & 0xFFF;

                            if (keyData.UnpackDataPointer > 0)
                                unpackData = reader.ReadArray<float>(keyData.UnpackDataPointer, 8);

                            var keyFrames = LoadFrames(reader, keyData.FramesPointer, keyData.KeyCount, keyFrameDataType);


                            if (QuaternionDecompressors.TryGetValue(compression, out var decompressionMethod))
                                decompressionMethod(reader, bones[bone.BoneHash], keyFrames, unpackData, keyData.DataPointer);
                            else
                                throw new Exception(string.Format("Unknown Quaternion compression type: 0x{0:X}", compression));

                            keyDataOffset += Marshal.SizeOf<KeyDataRE7>();
                        }

                        animation.Bones.Add(bones[bone.BoneHash]);
                    }
                }
            }

            return new Tuple<string, Animation>(reader.ReadUTF16NullTerminatedString(header.NamePointer), animation);
        }

        /// <summary>
        /// Converts an RE2 Motion File to a SEAnim
        /// </summary>
        private static Tuple<string, Animation> ConvertRE2(BinaryReader reader)
        {
            var animation = new Animation(Animation.DataType.Absolute);

            var header = reader.ReadStruct<MotionHeaderRE2>();

            var bones = new Dictionary<uint, Animation.Bone>(header.BoneCount);

            var name = reader.ReadUTF16NullTerminatedString(header.NamePointer);

            // Check for bone base data (this is required, so if it doesn't exit, quit)
            if (header.BoneBaseDataPointer > 0)
            {
                reader.BaseStream.Position = header.BoneBaseDataPointer;

                var baseDataOffset = reader.ReadInt64();
                var baseDataCount = reader.ReadInt32();

                var size = Marshal.SizeOf<BoneBaseDataRE7>();

                var boneBaseData = reader.ReadArray<BoneBaseDataRE7>(baseDataOffset, baseDataCount);

                foreach (var boneBase in boneBaseData)
                {
                    var bone = new Animation.Bone(reader.ReadUTF16NullTerminatedString(boneBase.NamePointer));

                    bone.Translations[0] = boneBase.Translation.ToVector3();
                    bone.Rotations[0] = boneBase.Rotation;
                    bones[boneBase.Hash] = bone;
                }

                if (header.BoneDataPointer > 0)
                {
                    var boneData = reader.ReadArray<BoneDataRE2>(header.BoneDataPointer, header.BoneDataCount);

                    foreach (var bone in boneData)
                    {
                        var keyDataOffset = bone.KeysPointer;

                        if (bone.Flags.HasFlag(DataPrecenseFlags.Translations))
                        {
                            var keyData = reader.ReadStruct<KeyDataRE7>(keyDataOffset);

                            float[] unpackData = null;
                            uint keyFrameDataType = keyData.Flags & 0xF00000;
                            uint compression = keyData.Flags & 0xFF000;
                            uint unkFlag = keyData.Flags & 0xFFF;

                            if (keyData.UnpackDataPointer > 0)
                                unpackData = reader.ReadArray<float>(keyData.UnpackDataPointer, 8);

                            var keyFrames = LoadFrames(reader, keyData.FramesPointer, keyData.KeyCount, keyFrameDataType);

                            if (Vector3Decompressors.TryGetValue(compression, out var decompressionMethod))
                                decompressionMethod(reader, bones[bone.BoneHash], keyFrames, unpackData, keyData.DataPointer);
                            else
                                throw new Exception(string.Format("Unknown Vector3 compression type: 0x{0:X}", compression));

                            keyDataOffset += Marshal.SizeOf<KeyDataRE7>();
                        }

                        if (bone.Flags.HasFlag(DataPrecenseFlags.Rotations))
                        {
                            var keyData = reader.ReadStruct<KeyDataRE7>(keyDataOffset);

                            float[] unpackData = null;
                            uint keyFrameDataType = keyData.Flags & 0xF00000;
                            uint compression = keyData.Flags & 0xFF000;
                            uint unkFlag = keyData.Flags & 0xFFF;

                            if (keyData.UnpackDataPointer > 0)
                                unpackData = reader.ReadArray<float>(keyData.UnpackDataPointer, 8);

                            var keyFrames = LoadFrames(reader, keyData.FramesPointer, keyData.KeyCount, keyFrameDataType);

                            if (QuaternionDecompressors.TryGetValue(compression, out var decompressionMethod))
                                decompressionMethod(reader, bones[bone.BoneHash], keyFrames, unpackData, keyData.DataPointer);
                            else
                                throw new Exception(string.Format("Unknown Quaternion compression type: 0x{0:X}", compression));

                            keyDataOffset += Marshal.SizeOf<KeyDataRE7>();
                        }

                        animation.Bones.Add(bones[bone.BoneHash]);
                    }
                }
            }

            return new Tuple<string, Animation>(reader.ReadUTF16NullTerminatedString(header.NamePointer), animation);
        }

        /// <summary>
        /// Converts an RE3 Motion File to a SEAnim
        /// </summary>
        private static Tuple<string, Animation> ConvertRE3(BinaryReader reader, Dictionary<uint, Animation.Bone> bones)
        {
            var animation = new Animation(Animation.DataType.Absolute);

            var header = reader.ReadStruct<MotionHeaderRE2>();

            var name = reader.ReadUTF16NullTerminatedString(header.NamePointer);

            var localBones = new Dictionary<uint, Animation.Bone>();

            {
                if(bones.Count <= 0)
                {
                    reader.BaseStream.Position = header.BoneBaseDataPointer;

                    var baseDataOffset = reader.ReadInt64();
                    var baseDataCount = reader.ReadInt32();

                    var boneBaseData = reader.ReadArray<BoneBaseDataRE7>(baseDataOffset, baseDataCount);

                    foreach (var boneBase in boneBaseData)
                    {
                        var bone = new Animation.Bone(reader.ReadUTF16NullTerminatedString(boneBase.NamePointer));

                        bone.Translations[0] = boneBase.Translation.ToVector3();
                        bone.Rotations[0] = boneBase.Rotation;
                        bones[boneBase.Hash] = bone;
                    }
                }

                // Add base data
                foreach(var bone in bones)
                {
                    var nbone = new Animation.Bone(bone.Value.Name);

                    nbone.Translations[0] = bone.Value.Translations[0];
                    nbone.Rotations[0] = bone.Value.Rotations[0];

                    localBones[bone.Key] = nbone;
                }

                if (header.BoneDataPointer > 0)
                {
                    var boneData = reader.ReadArray<BoneDataRE3>(header.BoneDataPointer, header.BoneDataCount);

                    foreach (var bone in boneData)
                    {
                        var keyDataOffset = bone.KeysPointer;

                        if (bone.Flags.HasFlag(DataPrecenseFlags.Translations))
                        {
                            var keyData = reader.ReadStruct<KeyDataRE3>(keyDataOffset);

                            float[] unpackData = null;
                            uint keyFrameDataType = keyData.Flags & 0xF00000;
                            uint compression      = keyData.Flags & 0xFF000;
                            uint unkFlag          = keyData.Flags & 0xFFF;

                            if (keyData.UnpackDataPointer > 0)
                                unpackData = reader.ReadArray<float>(keyData.UnpackDataPointer, 16);

                            var keyFrames = LoadFrames(reader, keyData.FramesPointer, keyData.KeyCount, keyFrameDataType);

                            if (Vector3DecompressorsRE3.TryGetValue(compression, out var decompressionMethod))
                                decompressionMethod(reader, localBones[bone.BoneHash], keyFrames, unpackData, keyData.DataPointer);
                            else
                                throw new Exception(string.Format("Unknown Vector3 compression type: 0x{0:X}", compression));

                            keyDataOffset += Marshal.SizeOf<KeyDataRE3>();
                        }

                        if (bone.Flags.HasFlag(DataPrecenseFlags.Rotations))
                        {
                            var keyData = reader.ReadStruct<KeyDataRE3>(keyDataOffset);

                            float[] unpackData = null;
                            uint keyFrameDataType = keyData.Flags & 0xF00000;
                            uint compression = keyData.Flags & 0xFF000;
                            uint unkFlag = keyData.Flags & 0xFFF;

                            if (keyData.UnpackDataPointer > 0)
                                unpackData = reader.ReadArray<float>(keyData.UnpackDataPointer, 16);

                            var keyFrames = LoadFrames(reader, keyData.FramesPointer, keyData.KeyCount, keyFrameDataType);


                            if (QuaternionDecompressorsRE3.TryGetValue(compression, out var decompressionMethod))
                                decompressionMethod(reader, localBones[bone.BoneHash], keyFrames, unpackData, keyData.DataPointer);
                            else
                                throw new Exception(string.Format("Unknown Quaternion compression type: 0x{0:X}", compression));

                            keyDataOffset += Marshal.SizeOf<KeyDataRE3>();
                        }
                    }
                }

                foreach (var bone in localBones)
                    animation.Bones.Add(bone.Value);
            }

            return new Tuple<string, Animation>(reader.ReadUTF16NullTerminatedString(header.NamePointer), animation);
        }

        /// <summary>
        /// Converts the given Motion
        /// </summary>
        public static Tuple<string, Animation> Convert(Stream stream, Dictionary<uint, Animation.Bone> bones)
        {
            using (var reader = new BinaryReader(stream))
            {
                var version = reader.ReadUInt32();
                reader.BaseStream.Position = 0;

                switch (version)
                {
                    case 0x2B: return ConvertRE7(reader);
                    case 0x41: return ConvertRE2(reader);
                    case 0x4E: return ConvertRE3(reader, bones);
                    default: throw new Exception("Invalid Motion Version");
                }
            }
        }
    }
}
