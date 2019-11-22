using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Tyrant
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            VersionLabel.Content = string.Format("Version: {0}", Assembly.GetExecutingAssembly().GetName().Version);
        }

        /// <summary>
        /// Opens homepage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HomePageButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Scobalula/Tyrant");
        }
    }
}
