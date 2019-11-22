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
using Tyrant.Logic;
using ByteSizeLib;

namespace Tyrant
{
    /// <summary>
    /// A class to hold an Asset that is displayed
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// Gets or Sets the name of the asset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets the asset type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or Sets the display info
        /// </summary>
        public string DisplaySize
        {
            get
            {
                return ByteSize.FromBytes(PackageEntry.CompressedSize).ToString();
            }
        }

        /// <summary>
        /// Gets or Sets the package entry
        /// </summary>
        public Package.Entry PackageEntry { get; set; }

        /// <summary>
        /// Creates a new Asset for UI
        /// </summary>
        public Asset(string name, string type, Package.Entry entry)
        {
            Name = name;
            Type = type;
            PackageEntry = entry;
        }
    }
}
