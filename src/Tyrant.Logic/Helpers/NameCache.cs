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
using System.Globalization;
using System.IO;

namespace Tyrant.Logic
{
    /// <summary>
    /// A class to hold a Name Cache
    /// </summary>
    public class NameCache
    {
        /// <summary>
        /// Gets or Sets the Entries/Names
        /// </summary>
        public Dictionary<ulong, string> Entries { get; set; }

        /// <summary>
        /// Creates a new name cache
        /// </summary>
        public NameCache()
        {
            Entries = new Dictionary<ulong, string>();
        }

        /// <summary>
        /// Loads entries from all files in the given folder
        /// </summary>
        /// <param name="folder">Folder to load from</param>
        public void LoadFolder(string folder)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*.*"))
                {
                    if (Path.GetExtension(file) == ".tcache")
                        LoadBinary(file);
                    else if (Path.GetExtension(file) == ".tcache_ascii")
                        LoadASCII(file);
                }
            }
            catch { }
        }

        /// <summary>
        /// Loads entries from a binary file
        /// </summary>
        /// <param name="fileName">File to load from</param>
        public void LoadBinary(string fileName)
        {
            try
            {
                using (var fileReader = new BinaryReader(File.OpenRead(fileName)))
                {
                    var magic            = fileReader.ReadUInt64();
                    var compression      = fileReader.ReadUInt64();
                    var compressedSize   = fileReader.ReadInt64();
                    var decompressedSize = fileReader.ReadInt64();

                    byte[] buffer = null;

                    switch(compression)
                    {
                        case 0: buffer = fileReader.ReadBytes((int)compressedSize); break;
                        case 1: buffer = ZLIB.Decompress(fileReader.ReadBytes((int)compressedSize)); break;
                        case 2: buffer = ZStandard.Decompress(fileReader.ReadBytes((int)compressedSize)); break;
                        case 3: buffer = LZ4.Decompress(fileReader.ReadBytes((int)compressedSize), (int)decompressedSize); break;
                        default: throw new Exception("Invalid cache compression");
                    }


                    using (var reader = new BinaryReader(new MemoryStream(buffer)))
                    {
                        var count = (int)reader.ReadUInt64();

                        switch (magic)
                        {
                            // 32Bit Cache
                            case 0x3130454843414354:
                                {
                                    for(int i = 0; i < count; i++)
                                        Entries[reader.ReadUInt32()] = reader.ReadNullTerminatedString();

                                    break;
                                }
                            // 64Bit Cache
                            case 0x3230454843414354:
                                {
                                    for (int i = 0; i < count; i++)
                                        Entries[reader.ReadUInt64()] = reader.ReadNullTerminatedString();

                                    break;
                                }
                        }
                    }
                }
            }
#if DEBUG
            catch(Exception e)
            {
                Console.WriteLine("Failed to load {0}: {1}", fileName, e);
            }
#else
            catch { }
#endif
        }

        /// <summary>
        /// Loads entries from an ASCII/Plain Text file
        /// </summary>
        /// <param name="fileName">File to load from</param>
        public void LoadASCII(string fileName)
        {
            try
            {
                foreach(var line in File.ReadLines(fileName))
                {
                    var lineSplit = line.Trim().Split(',');

                    if(lineSplit.Length >= 2)
                    {
                        if (ulong.TryParse(lineSplit[0], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out ulong id))
                        {
                            Entries[id] = lineSplit[1];
                        }
                    }
                }
            }
#if DEBUG
            catch (Exception e)
            {
                Console.WriteLine("Failed to load {0}: {1}", fileName, e);
            }
#else
            catch { }
#endif
        }
    }
}
