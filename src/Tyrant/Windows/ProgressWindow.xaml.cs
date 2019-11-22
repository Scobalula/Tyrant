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
using System.Windows;
using System.Windows.Controls;

namespace Tyrant
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        /// <summary>
        /// Whether we cancelled or not
        /// </summary>
        private bool HasCancelled = false;

        /// <summary>
        /// Initializes Progress Window
        /// </summary>
        public ProgressWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets Cancelled to true to update current task
        /// </summary>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !HasCancelled;
        }

        /// <summary>
        /// Closes Window on Cancel click
        /// </summary>
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            HasCancelled = true;
        }

        /// <summary>
        /// Closes Progress Window on Complete
        /// </summary>
        public void Complete()
        {
            ProgressBar.Value = ProgressBar.Maximum;
            HasCancelled = true;
            Close();
        }

        public void SetProgressCount(double value)
        {
            // Invoke dispatcher to update UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ProgressBar.Maximum = value;
                ProgressBar.Value = 0;
            }));
        }

        public void SetProgressMessage(string value)
        {
            // Invoke dispatcher to update UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Message.Content = value;
            }));
        }

        /// <summary>
        /// Update Progress and checks for cancel
        /// </summary>
        public bool IncrementProgress()
        {
            // Invoke dispatcher to update UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ProgressBar.Value++;
            }));

            // Return whether we've cancelled or not
            return HasCancelled;
        }
    }
}