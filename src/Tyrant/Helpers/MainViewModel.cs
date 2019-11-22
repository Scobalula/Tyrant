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
using System.ComponentModel;
using System.Windows.Data;

namespace Tyrant
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region BackingVariables
        /// <summary>
        /// Gets or Sets the Filter String
        /// </summary>
        private string BackingFilterString { get; set; }

        /// <summary>
        /// Gets or Sets the Filter Strings
        /// </summary>
        private string[] FilterStrings { get; set; }
        #endregion


        public string FilterString
        {
            get
            {
                return BackingFilterString;
            }
            set
            {
                if (value != BackingFilterString)
                {
                    BackingFilterString = value;
                    FilterStrings = string.IsNullOrWhiteSpace(BackingFilterString) ? null : BackingFilterString.Split(' ');
                    AssetsView.Refresh();
                    OnPropertyChanged("FilterString");
                }
            }
        }

        /// <summary>
        /// Gets or Sets the Collection View for the Assets
        /// </summary>
        private ICollectionView AssetsView { get; set; }

        /// <summary>
        /// Gets the observable collection of assets
        /// </summary>
        public AssetList Assets { get; } = new AssetList();

        /// <summary>
        /// Property Changed Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            AssetsView = CollectionViewSource.GetDefaultView(Assets);
            AssetsView.Filter = delegate (object obj)
            {
                if(FilterStrings != null && FilterStrings.Length > 0 && obj is Asset asset)
                {
                    var assetName = asset.Name.ToLower();

                    foreach(var filterString in FilterStrings)
                    {
                        if(!string.IsNullOrWhiteSpace(filterString) && assetName.Contains(filterString.ToLower()))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return true;
            };
        }

        /// <summary>
        /// Updates the Property on Change
        /// </summary>
        /// <param name="name">Property Name</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
