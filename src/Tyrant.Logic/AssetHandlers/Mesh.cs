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
using PhilLibX.Mathematics;
using PhilLibX.IO;
using PhilLibX;

namespace Tyrant.Logic
{
    public class Mesh
    {
        /// <summary>
        /// Resident Evil 7 Mesh Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MeshHeaderRE7
        {
            public uint Magic;
            public uint Version;
            public int FileSize;
            public ushort Unk;
            public ushort StringCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public long[] ModelPointers;
            public long BoneDataHeaderPointer;
            public long UnkPointer01;
            public long UnkPointer02;
            public long GeometryPointer;
            public long UnkPointer03;
            public long MaterialNamesPointer;
            public long BoneNamesPointer;
            public long UnkPointer04;
            public long StringTablePointer;
        }

        /// <summary>
        /// Resident Evil 2 Mesh Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MeshHeaderRE2
        {
            public uint Magic;
            public uint Version;
            public long FileSize;
            public ushort Unk;
            public ushort StringCount;
            public uint Padding;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public long[] ModelPointers;
            public long BoneDataHeaderPointer;
            public long UnkPointer01;
            public long UnkPointer02;
            public long UnkPointer03;
            public long GeometryPointer;
            public long UnkPointer05;
            public long MaterialNamesPointer;
            public long BoneNamesPointer;
            public long UnkPointer08;
            public long StringTablePointer;
        }

        /// <summary>
        /// Resident Evil 7 Bone Data Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BoneDataHeaderRE7
        {
            public short BoneCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
            public byte[] Padding;
            public long BoneTablePointer;
            public long MatricesPointer;
            public long UnkMatricesPointer01;
            public long UnkMatricesPointer02;
        }

        /// <summary>
        /// Resident Evil 2 Bone Data Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BoneDataHeaderRE2
        {
            public int BoneCount;
            public int SkinnedBoneCount;
            public long Padding;
            public long BoneTablePointer;
            public long MatricesPointer;
            public long UnkMatricesPointer01;
            public long UnkMatricesPointer02;
        }

        /// <summary>
        /// Resident Evil 7 Bone Data
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
        internal struct BoneDataRE7
        {
            public short BoneIndex;
            public short ParentIndex;
            public short NextSibling;
            public short FirstChild;
            public short Unk; // Sometimes == BoneIndex, Sometimes -1
        }

        /// <summary>
        /// Resident Evil 7 Model
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ModelHeaderRE7
        {
            public byte LODCount;
            public byte MaterialCount;
            public byte UVCount;
            public byte Unk01;
            public int Unk02;
        }

        /// <summary>
        /// Resident Evil 7 LOD
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct LODHeaderRE7
        {
            public ushort MeshCount;
            public byte Flags;
            public byte BoneCount;
            public float LODDistance;
            public long MeshesPointer;
        }

        /// <summary>
        /// Resident Evil 7 LOD Mesh
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct LODMeshRE7
        {
            public byte MeshIndex;
            public byte SubMeshCount;
            public byte Unk01;
            public byte Unk02;
            public int Unk03;
            public int VertexCount;
            public int FaceCount;
        }

        /// <summary>
        /// Resident Evil 7 LOD Submesh
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct LODSubMeshRE7
        {
            public int MaterialIndex;
            public int FaceCount;
            public int FaceIndex;
            public int VertexIndex;
        }

        /// <summary>
        /// Resident Evil 7 Geometry Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct GeometryHeaderRE7
        {
            public uint VertexBufferSize;
            public long UnkPointer;
            public int Padding;
            public int FaceBufferSize;
            public long UnkPointer01;
            public int Padding01;
            public long Unk01;
            public long FaceBufferOffset;
        }

        /// <summary>
        /// Resident Evil 2 Geometry Header
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct GeometryHeaderRE2
        {
            public long VertexBlocksOffset;
            public long VertexDataOffset;
            public long FaceBufferOffset;
            public int VertexBufferSize;
            public int FaceBufferSize;
            public short VertexBlockCount;
            public short UnkBlockCount; // Always = Above?
            public long Unk01;
            public int Unk02; // Some value and 0xFFFF usually
        }

        /// <summary>
        /// Resident Evil 2 Vertex Block
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct VertexBlockRE2
        {
            public short ID; // 0 = XYZ, 1 = Normal/Tangent, 2 = UVLayer1, 3 = UVLayer2, 4 = Weights
            public short ElementSize;
            public int Offset; // Relative to GeometryHeaderRE2.VertexDataOffset
        }

        /// <summary>
        /// Packed Padded 3-Byte 3-D Vector
        /// </summary>
        public struct PackedVector3
        {
            public sbyte X { get; set; }
            public sbyte Y { get; set; }
            public sbyte Z { get; set; }
            public sbyte W { get; set; }

            public Vector3 Unpack()
            {
                return new Vector3(X / 127.0f, Y / 127.0f, Z / 127.0f);
            }
        }

        /// <summary>
        /// Face Buffer;
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Face
        {
            public ushort FaceIndex1;
            public ushort FaceIndex2;
            public ushort FaceIndex3;
        }

        /// <summary>
        /// 4x4 Transformation Matrix
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Matrix4x4
        {
            public Vector4 X;
            public Vector4 Y;
            public Vector4 Z;
            public Vector4 W;

            public Quaternion ToQuaternion()
            {
                var result = new Quaternion(0, 0, 0, 1.0f);

                float divisor;
                float transRemain = X.X + Y.Y + Z.Z;

                if (transRemain > 0)
                {
                    divisor = (float)Math.Sqrt(transRemain + 1.0f) * 2.0f;
                    result.W = 0.25f * divisor;
                    result.X = (Y.Z - Z.Y) / divisor;
                    result.Y = (Z.X - X.Z) / divisor;
                    result.Z = (X.Y - Y.X) / divisor;
                }
                else if ((X.X > Y.Y) && (X.X > Z.Z))
                {
                    divisor = (float)Math.Sqrt(1.0f + X.X - Y.Y - Z.Z) * 2.0f;
                    result.W = (Y.Z - Z.Y) / divisor;
                    result.X = 0.25f * divisor;
                    result.Y = (Y.X + X.Y) / divisor;
                    result.Z = (Z.X + X.Z) / divisor;
                }
                else if (Y.Y > Z.Z)
                {
                    divisor = (float)Math.Sqrt(1.0f + Y.Y - X.X - Z.Z) * 2.0f;
                    result.W = (Z.X - X.Z) / divisor;
                    result.X = (Y.X + X.Y) / divisor;
                    result.Y = 0.25f * divisor;
                    result.Z = (Z.Y + Y.Z) / divisor;
                }
                else
                {
                    divisor = (float)Math.Sqrt(1.0f + Z.Z - X.X - Y.Y) * 2.0f;
                    result.W = (X.Y - Y.X) / divisor;
                    result.X = (Z.X + X.Z) / divisor;
                    result.Y = (Z.Y + Y.Z) / divisor;
                    result.Z = 0.25f * divisor;
                }

                // Return resulting vector
                return result;
            }
        }

        /// <summary>
        /// Loads Meshes from a Resident Evil 7 Mesh File
        /// </summary>
        private static List<List<Model>> ConvertRE7(BinaryReader reader)
        {
            var results = new List<List<Model>>(3);

            var header = reader.ReadStruct<MeshHeaderRE7>();
            var boneDataHeader = new BoneDataHeaderRE7();
            var geometryHeader = new GeometryHeaderRE7();

            if (header.BoneDataHeaderPointer > 0)
                boneDataHeader = reader.ReadStruct<BoneDataHeaderRE7>(header.BoneDataHeaderPointer);
            if (header.GeometryPointer > 0)
                geometryHeader = reader.ReadStruct<GeometryHeaderRE7>(header.GeometryPointer);

            // Parse all strings for bones, materials, etc. into lists to make it easier to pass around, etc.
            var strings = new List<string>(header.StringCount);
            foreach (var offset in reader.ReadArray<long>(header.StringTablePointer, header.StringCount))
                strings.Add(reader.ReadNullTerminatedString(offset));

            var boneNames = new List<string>(boneDataHeader.BoneCount);
            foreach (var boneNameIndex in reader.ReadArray<ushort>(header.BoneNamesPointer, boneDataHeader.BoneCount))
                boneNames.Add(strings[boneNameIndex]);

            var bones = new List<Model.Bone>(boneDataHeader.BoneCount);

            if (header.BoneDataHeaderPointer > 0)
            {
                var boneDatas = reader.ReadArray<BoneDataRE7>(boneDataHeader.BoneTablePointer, boneDataHeader.BoneCount);
                var boneMatrices = reader.ReadArray<Matrix4x4>(boneDataHeader.MatricesPointer, boneDataHeader.BoneCount);

                for (ushort i = 0; i < boneDataHeader.BoneCount; i++)
                {
                    var bone = new Model.Bone(boneNames[i], boneDatas[i].ParentIndex, new Vector3(
                            boneMatrices[i].W.X,
                            boneMatrices[i].W.Y,
                            boneMatrices[i].W.Z), boneMatrices[i].ToQuaternion());

                    bones.Add(bone);
                }
            }

            bool firstMdlProcessed = false;

            for (int mdl = 0; mdl < 3; mdl++)
            {
                if (header.ModelPointers[mdl] == 0)
                    continue;

                if (firstMdlProcessed)
                    break;

                var lods = new List<Model>();

                var modelHeader = reader.ReadStruct<ModelHeaderRE7>(header.ModelPointers[mdl]);
                var vertexSize = 20 + (modelHeader.UVCount * 4) + (boneDataHeader.BoneCount > 0 ? 16 : 0);

                reader.BaseStream.Position = header.ModelPointers[mdl] + (firstMdlProcessed ? 16 : 64);
                var materialIndices = reader.ReadArray<short>(header.MaterialNamesPointer, modelHeader.MaterialCount);

                firstMdlProcessed = true;

                var lodPointers = reader.ReadArray<long>(reader.ReadInt64(), modelHeader.LODCount);

                foreach (var lodPointer in lodPointers)
                {
                    var model = new Model()
                    {
                        Bones = bones
                    };

                    var uniqueMaterials = new List<string>(modelHeader.MaterialCount);

                    var lodHeader = reader.ReadStruct<LODHeaderRE7>(lodPointer);
                    var boneIndices = reader.ReadArray<short>(lodPointer + 16, lodHeader.BoneCount);

                    var meshPointers = reader.ReadArray<long>(lodHeader.MeshesPointer, lodHeader.MeshCount);

                    foreach (var meshPointer in meshPointers)
                    {
                        var mesh = reader.ReadStruct<LODMeshRE7>(meshPointer);
                        var subMeshes = reader.ReadArray<LODSubMeshRE7>(meshPointer + 16, mesh.SubMeshCount);
                        int verticesRead = 0;

                        for (int i = 0; i < subMeshes.Length; i++)
                        {
                            var materialName = strings[materialIndices[subMeshes[i].MaterialIndex]];

                            if (!uniqueMaterials.Contains(materialName))
                                uniqueMaterials.Add(materialName);

                            var subMesh = new Model.Mesh();

                            subMesh.MaterialIndices.Add(uniqueMaterials.IndexOf(materialName));

                            int subMeshVertexCount = 0;
                            int subMeshFaceCount = subMeshes[i].FaceCount;

                            // Since the counts aren't stored in each, we can use this to determine the counts
                            if (i != subMeshes.Length - 1)
                                subMeshVertexCount = subMeshes[i + 1].VertexIndex - subMeshes[i].VertexIndex;
                            else
                                subMeshVertexCount = mesh.VertexCount - verticesRead;


                            verticesRead += subMeshVertexCount;

                            reader.BaseStream.Position = header.GeometryPointer + 48 + (vertexSize * subMeshes[i].VertexIndex);

                            for (int v = 0; v < subMeshVertexCount; v++)
                            {
                                // Base vertex data
                                var vertex = new Model.Vertex(
                                    reader.ReadStruct<Vector3>(),
                                    reader.ReadStruct<PackedVector3>().Unpack(),
                                    reader.ReadStruct<PackedVector3>().Unpack());
                                vertex.UVs.Add(new Vector2(reader.ReadStruct<Half>(), reader.ReadStruct<Half>()));
                                // Skip unnsupported UV layers
                                reader.BaseStream.Position += 4 * (modelHeader.UVCount - 1);

                                // Check if we have bones
                                if (lodHeader.BoneCount > 0)
                                {
                                    var localBoneIndices = reader.ReadBytes(8);
                                    var weights = reader.ReadBytes(8);
                                    var weightSum = 0.0f;

                                    for (int w = 0; w < 8 && weights[w] != 0; w++)
                                    {
                                        vertex.Weights.Add(new Model.Vertex.Weight()
                                        {
                                            BoneIndex = boneIndices[localBoneIndices[w]],
                                            Influence = weights[w] / 255.0f
                                        });

                                        weightSum += vertex.Weights[w].Influence;
                                    }

                                    var multiplier = 1.0f / weightSum;

                                    foreach (var weight in vertex.Weights)
                                        weight.Influence *= multiplier;
                                }

                                subMesh.Vertices.Add(vertex);
                            }

                            switch (lodHeader.Flags)
                            {
                                case 0:
                                    reader.BaseStream.Position = geometryHeader.FaceBufferOffset + (2 * subMeshes[i].FaceIndex);

                                    for (int f = 0; f < subMeshes[i].FaceCount / 3; f++)
                                    {
                                        var v1 = reader.ReadUInt16();
                                        var v2 = reader.ReadUInt16();
                                        var v3 = reader.ReadUInt16();

                                        if (v1 != v2 && v2 != v3 && v3 != v1)
                                            subMesh.Faces.Add(new Model.Face(v1, v2, v3));
                                    }
                                    break;
                                case 1:
                                    reader.BaseStream.Position = geometryHeader.FaceBufferOffset + (4 * subMeshes[i].FaceIndex);

                                    for (int f = 0; f < subMeshes[i].FaceCount / 3; f++)
                                    {
                                        var v1 = reader.ReadInt32();
                                        var v2 = reader.ReadInt32();
                                        var v3 = reader.ReadInt32();

                                        if (v1 != v2 && v2 != v3 && v3 != v1)
                                            subMesh.Faces.Add(new Model.Face(v1, v2, v3));
                                    }
                                    break;
                            }

                            model.Meshes.Add(subMesh);
                        }
                    }

                    foreach (var materialName in uniqueMaterials)
                    {
                        model.Materials.Add(new Model.Material(materialName));
                    }

                    lods.Add(model);
                }

                results.Add(lods);
            }

            return results;
        }


        /// <summary>
        /// Loads Meshes from a Resident Evil 2 Mesh
        /// </summary>
        private static List<List<Model>> ConvertRE2(BinaryReader reader)
        {
            var results = new List<List<Model>>(3);

            var header = reader.ReadStruct<MeshHeaderRE2>();
            var boneDataHeader = new BoneDataHeaderRE2();
            var geometryHeader = new GeometryHeaderRE2();
            var vertexBlocks = new Dictionary<short, long>();

            short[] skinnedBones = null;

            if (header.BoneDataHeaderPointer > 0)
            {
                boneDataHeader = reader.ReadStruct<BoneDataHeaderRE2>(header.BoneDataHeaderPointer);
                skinnedBones = reader.ReadArray<short>(header.BoneDataHeaderPointer + 48, boneDataHeader.SkinnedBoneCount);
            }

            if (header.GeometryPointer > 0)
            {
                geometryHeader = reader.ReadStruct<GeometryHeaderRE2>(header.GeometryPointer);
                var blocks = reader.ReadArray<VertexBlockRE2>(geometryHeader.VertexBlocksOffset, geometryHeader.VertexBlockCount);

                foreach(var block in blocks)
                {
                    vertexBlocks[block.ID] = geometryHeader.VertexDataOffset + block.Offset;
                }
            }

            // Parse all strings for bones, materials, etc. into lists to make it easier to pass around, etc.
            var strings = new List<string>(header.StringCount);
            foreach (var offset in reader.ReadArray<long>(header.StringTablePointer, header.StringCount))
                strings.Add(reader.ReadNullTerminatedString(offset));

            var boneNames = new List<string>(boneDataHeader.BoneCount);
            foreach (var boneNameIndex in reader.ReadArray<ushort>(header.BoneNamesPointer, boneDataHeader.BoneCount))
                boneNames.Add(strings[boneNameIndex]);

            var bones = new List<Model.Bone>(boneDataHeader.BoneCount);

            if (header.BoneDataHeaderPointer > 0)
            {
                var boneDatas = reader.ReadArray<BoneDataRE7>(boneDataHeader.BoneTablePointer, boneDataHeader.BoneCount);
                var boneMatrices = reader.ReadArray<Matrix4x4>(boneDataHeader.MatricesPointer, boneDataHeader.BoneCount);

                for (ushort i = 0; i < boneDataHeader.BoneCount; i++)
                {
                    var bone = new Model.Bone(boneNames[i], boneDatas[i].ParentIndex, new Vector3(
                            boneMatrices[i].W.X,
                            boneMatrices[i].W.Y,
                            boneMatrices[i].W.Z), boneMatrices[i].ToQuaternion());

                    bones.Add(bone);
                }
            }

            bool firstMdlProcessed = false;

            for (int mdl = 0; mdl < 3; mdl++)
            {
                if (header.ModelPointers[mdl] == 0)
                    continue;

                if (firstMdlProcessed)
                    break;

                var lods = new List<Model>();

                var modelHeader = reader.ReadStruct<ModelHeaderRE7>(header.ModelPointers[mdl]);

                reader.BaseStream.Position = header.ModelPointers[mdl] + (firstMdlProcessed ? 16 : 64);
                var materialIndices = reader.ReadArray<short>(header.MaterialNamesPointer, modelHeader.MaterialCount);

                firstMdlProcessed = true;

                var lodPointers = reader.ReadArray<long>(reader.ReadInt64(), modelHeader.LODCount);

                foreach (var lodPointer in lodPointers)
                {
                    var model = new Model()
                    {
                        Bones = bones
                    };

                    var uniqueMaterials = new List<string>(modelHeader.MaterialCount);

                    var lodHeader = reader.ReadStruct<LODHeaderRE7>(lodPointer);

                    var meshPointers = reader.ReadArray<long>(lodHeader.MeshesPointer, lodHeader.MeshCount);

                    foreach (var meshPointer in meshPointers)
                    {
                        var mesh = reader.ReadStruct<LODMeshRE7>(meshPointer);
                        var subMeshes = reader.ReadArray<LODSubMeshRE7>(meshPointer + 16, mesh.SubMeshCount);
                        int verticesRead = 0;

                        for (int i = 0; i < subMeshes.Length; i++)
                        {
                            var materialName = strings[materialIndices[subMeshes[i].MaterialIndex]];

                            if (!uniqueMaterials.Contains(materialName))
                                uniqueMaterials.Add(materialName);

                            int subMeshVertexCount = 0;
                            int subMeshFaceCount = subMeshes[i].FaceCount;

                            // Since the counts aren't stored in each, we can use this to determine the counts
                            if (i != subMeshes.Length - 1)
                                subMeshVertexCount = subMeshes[i + 1].VertexIndex - subMeshes[i].VertexIndex;
                            else
                                subMeshVertexCount = mesh.VertexCount - verticesRead;


                            verticesRead += subMeshVertexCount;


                            var subMesh = new Model.Mesh(subMeshVertexCount, subMeshFaceCount);

                            subMesh.MaterialIndices.Add(uniqueMaterials.IndexOf(materialName));

                            // Positions
                            if (vertexBlocks.TryGetValue(0, out var positionsOffset))
                            {
                                reader.BaseStream.Position = positionsOffset + (12 * subMeshes[i].VertexIndex);

                                for (int v = 0; v < subMeshVertexCount; v++)
                                {
                                    subMesh.Vertices.Add(new Model.Vertex(reader.ReadStruct<Vector3>()));
                                }
                            }
                            // Normals/Tangents
                            if (vertexBlocks.TryGetValue(1, out var normalsTangentsOffset))
                            {
                                reader.BaseStream.Position = normalsTangentsOffset + (8 * subMeshes[i].VertexIndex);

                                for (int v = 0; v < subMeshVertexCount; v++)
                                {
                                    subMesh.Vertices[v].Normal = reader.ReadStruct<PackedVector3>().Unpack();
                                    subMesh.Vertices[v].Tangent = reader.ReadStruct<PackedVector3>().Unpack();
                                }
                            }
                            // UVs
                            if (vertexBlocks.TryGetValue(2, out var uvsOffset))
                            {
                                reader.BaseStream.Position = uvsOffset + (4 * subMeshes[i].VertexIndex);

                                for (int v = 0; v < subMeshVertexCount; v++)
                                {
                                    subMesh.Vertices[v].UVs.Add(new Vector2(reader.ReadStruct<Half>(), reader.ReadStruct<Half>()));
                                }
                            }
                            // Weights
                            if (vertexBlocks.TryGetValue(4, out var weightsOffset))
                            {
                                reader.BaseStream.Position = weightsOffset + (16 * subMeshes[i].VertexIndex);

                                for (int v = 0; v < subMeshVertexCount; v++)
                                {
                                    var localBoneIndices = reader.ReadBytes(8);
                                    var weights = reader.ReadBytes(8);
                                    var weightSum = 0.0f;

                                    for (int w = 0; w < 8 && weights[w] != 0; w++)
                                    {
                                        subMesh.Vertices[v].Weights.Add(new Model.Vertex.Weight()
                                        {
                                            BoneIndex = skinnedBones[localBoneIndices[w]],
                                            Influence = weights[w] / 255.0f
                                        });

                                        weightSum += subMesh.Vertices[v].Weights[w].Influence;
                                    }

                                    var multiplier = 1.0f / weightSum;

                                    foreach (var weight in subMesh.Vertices[v].Weights)
                                        weight.Influence *= multiplier;
                                }
                            }

                            switch (lodHeader.Flags)
                            {
                                case 0:
                                    reader.BaseStream.Position = geometryHeader.FaceBufferOffset + (2 * subMeshes[i].FaceIndex);

                                    for (int f = 0; f < subMeshes[i].FaceCount / 3; f++)
                                    {
                                        var v1 = reader.ReadUInt16();
                                        var v2 = reader.ReadUInt16();
                                        var v3 = reader.ReadUInt16();

                                        if (v1 != v2 && v2 != v3 && v3 != v1)
                                            subMesh.Faces.Add(new Model.Face(v1, v2, v3));
                                    }
                                    break;
                                case 1:
                                    reader.BaseStream.Position = geometryHeader.FaceBufferOffset + (4 * subMeshes[i].FaceIndex);

                                    for (int f = 0; f < subMeshes[i].FaceCount / 3; f++)
                                    {
                                        var v1 = reader.ReadInt32();
                                        var v2 = reader.ReadInt32();
                                        var v3 = reader.ReadInt32();

                                        if (v1 != v2 && v2 != v3 && v3 != v1)
                                            subMesh.Faces.Add(new Model.Face(v1, v2, v3));
                                    }
                                    break;
                            }

                            model.Meshes.Add(subMesh);
                        }
                    }


                    foreach (var materialName in uniqueMaterials)
                    {
                        model.Materials.Add(new Model.Material(materialName));
                    }

                    lods.Add(model);
                }

                results.Add(lods);
            }
            return results;
        }

        /// <summary>
        /// Converts Mesh
        /// </summary>
        public static List<List<Model>> Convert(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
                return Convert(stream);
        }

        /// <summary>
        /// Converts Mesh
        /// </summary>
        public static List<List<Model>> Convert(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var magic = reader.ReadUInt32();
                var version = reader.ReadUInt16();
                reader.BaseStream.Position = 0;

                switch (version)
                {
                    case 0x2800: return ConvertRE7(reader);
                    case 0x0600: return ConvertRE2(reader);
                    default: throw new Exception("Invalid Motion Version");
                }
            }
        }
    }
}
