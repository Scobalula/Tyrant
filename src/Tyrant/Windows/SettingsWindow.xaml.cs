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
using System.Windows;
using System.Windows.Controls;

namespace Tyrant
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }


        public void SetUIFromSettings(TyrantSettings settings)
        {
            var grid = (Grid)Content;

            foreach (var item in grid.Children)
            {
                if (item is CheckBox checkbox)
                {
                    // Use default XAML value to determine if it this is default to on/off
                    checkbox.IsChecked = settings[checkbox.Name, checkbox.IsChecked == true ? "Yes" : "No"] == "Yes";
                }
                else if (item is TextBox box)
                {
                    if (box.Name.StartsWith("Float"))
                        box.Text = settings[box.Name.Substring(5), "1.0"];
                    else if (box.Name.StartsWith("Integer"))
                        box.Text = settings[box.Name.Substring(7), "1"];
                    else
                        box.Text = settings[box.Name, ""];
                }
            }
        }

        public void SetSettingsFromUI(TyrantSettings settings)
        {
            var grid = (Grid)Content;

            foreach (var item in grid.Children)
            {
                if (item is CheckBox checkbox)
                {
                    // Use default XAML value to determine if it this is default to on/off
                    settings[checkbox.Name] = checkbox.IsChecked == true ? "Yes" : "No";
                }
                else if(item is TextBox box)
                {
                    if (box.Name.StartsWith("Float"))
                        settings[box.Name.Substring(5)] = float.TryParse(box.Text, out var val) ? box.Text : "1.0";
                    else if (box.Name.StartsWith("Integer"))
                        settings[box.Name.Substring(7)] = int.TryParse(box.Text, out var val) ? box.Text : "1";
                    else
                        settings[box.Name] = box.Text;
                }
            }
        }
    }
}
