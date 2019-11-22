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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Tyrant
{
    /// <summary>
    /// A class to hold an Asset List
    /// </summary>
    public class AssetList : ObservableCollection<Asset>
    {
        /// <summary>
        /// Gets or Sets if we are loading assets
        /// </summary>
        private bool Loading = false;

        /// <summary>
        /// Adds assets to the UI without notfying each time
        /// </summary>
        /// <param name="assets">List of Assets</param>
        public void AddAssets(IEnumerable<Asset> assets)
        {
            Loading = true;

            foreach (var asset in assets)
                Add(asset);

            Loading = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Clears all loaded assets
        /// </summary>
        public void ClearAssets()
        {
            Loading = true;
            Clear();
            Loading = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Overrides On Collection Changed to disable notifying each time the list is updated
        /// </summary>
        /// <param name="e">Args</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!Loading)
                base.OnCollectionChanged(e);
        }
    }
}
