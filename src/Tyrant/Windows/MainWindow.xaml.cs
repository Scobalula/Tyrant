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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tyrant.Logic;
using PhilLibX;
using PhilLibX.Imaging;
using Microsoft.Win32;
using PhilLibX.Cryptography.Hash;
using System.Threading;

namespace Tyrant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Gets or Sets the current loaded package
        /// </summary>
        private Package ActivePackage { get; set; }

        /// <summary>
        /// Gets the name cache
        /// </summary>
        private NameCache Cache { get; } = new NameCache();

        /// <summary>
        /// Gets the Settings
        /// </summary>
        private TyrantSettings Settings { get; } = new TyrantSettings("Settings.tcfg");

        /// <summary>
        /// Gets the Log
        /// </summary>
        private StreamWriter LogStream { get; } = new StreamWriter("Log.txt", true);

        /// <summary>
        /// Gets or Sets whether or not to end the current thread
        /// </summary>
        private bool EndThread = false;

        /// <summary>
        /// Directory prefixes 
        /// </summary>
        private string[] DirectoryPrefixes =
        {
            "natives/x64/streaming/",
            "natives/x64/",
        };

        /// <summary>
        /// Suffixes to try when resolving linked assets
        /// </summary>
        private string[] FileSuffixes =
        {
            ".mdf2.10",
            "_mat.mdf2.10",
            ".mdf2.6",
            "_mat.mdf2.6",
            ".10",
            ".11",
            ".8",
        };

        /// <summary>
        /// Supported Image Formats
        /// </summary>
        private string[] ImageFormats =
        {
            "dds",
            "png",
            "tiff",
            "jpg",
            "bmp",
            "tga",
        };

        /// <summary>
        /// Supported Model Formats
        /// </summary>
        private string[] ModelFormats =
        {
            "ma",
            "semodel",
            "obj",
            "smd",
            "ascii",
        };

        /// <summary>
        /// Supported Image Formats
        /// </summary>
        private string[] AnimationFormats =
        {
            "seanim",
        };

        /// <summary>
        /// Gets the ViewModel
        /// </summary>
        public MainViewModel ViewModel { get; } = new MainViewModel();

        /// <summary>
        /// 
        /// </summary>
        public MainWindow()
        {
            Cache.LoadFolder("TyrantCache");
            InitializeComponent();
            DataContext = ViewModel;
        }

        /// <summary>
        /// Loads a Package
        /// </summary>
        public void LoadPackage(string path)
        {
            try
            {
                ViewModel.Assets.ClearAssets();
                ActivePackage?.Dispose();
                ActivePackage = new Package(path);

                List<Asset> results = new List<Asset>(ActivePackage.Entries.Count);

                foreach(var entry in ActivePackage.Entries)
                {
                    if(Cache.Entries.TryGetValue(entry.Key, out var name))
                    {
                        string type = "Unknown";

                        if (name.Contains(".tex."))
                        {
                            if (Settings["ShowTextures", "Yes"] == "Yes")
                                type = "Texture";
                            else
                                continue;
                        }
                        else if (name.Contains(".mesh."))
                        {
                            if (Settings["ShowModels", "Yes"] == "Yes")
                                type = "Mesh";
                            else
                                continue;
                        }
                        else if (name.Contains(".motlist."))
                        {
                            if (Settings["ShowAnims", "Yes"] == "Yes")
                                type = "MotionList";
                            else
                                continue;
                        }
                        else if (name.Contains(".mot."))
                        {
                            if (Settings["ShowAnims", "Yes"] == "Yes")
                                type = "Motion";
                            else
                                continue;
                        }
                        //else if (name.Contains(".bnk.") && Settings["ShowSounds", "Yes"] == "Yes")
                        //    type = "SoundBank";
                        else if (Settings["ShowAll", "No"] == "Yes")
                            type = "Unknown";
                        else
                            continue;

                        results.Add(new Asset(name, type, entry.Value));
                    }
                    else if (Settings["ShowAll", "No"] == "Yes")
                    {
                        results.Add(new Asset("asset_" + entry.Key.ToString("x"), "Unknown", entry.Value));
                    }

                }

                ViewModel.Assets.AddAssets(results.OrderBy(x => x.Name).OrderBy(x => x.Type));
            }
            catch(Exception e)
            {
                ActivePackage?.Dispose();
                ActivePackage = null;

                MessageBox.Show(string.Format("An error occured while loading the package:\n\n{0}", e), "Tryant | Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles cleaning up on close
        /// </summary>
        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            ActivePackage?.Dispose();
            Settings.Save("Settings.tcfg");
        }

        /// <summary>
        /// Exports the Image Asset
        /// </summary>
        private string ExportImage(Package.Entry entry, string name)
        {
            var path = name.Split('.')[0];
            var result = path + ".png";

            bool requiresExport = false;

            // Determine if we should even bother loading
            foreach (var format in ImageFormats)
            {
                // PNG is default
                if (Settings["Export" + format.ToUpper(), format == "png" ? "Yes" : "No"] == "Yes")
                {
                    // Set our path to the last exported type
                    result = path + "." + format;

                    if(!File.Exists(path + "." + format))
                    {
                        requiresExport = true;
                    }
                }
            }

            if(requiresExport)
            {
                using (var image = Texture.Convert(ActivePackage.LoadEntry(entry)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    foreach (var format in ImageFormats)
                    {
                        if (Settings["Export" + format.ToUpper(), format == "png" ? "Yes" : "No"] == "Yes" && !File.Exists(path + "." + format))
                        {
                            // Everything besides DDS we need to decompress & convert
                            if (format != "dds")
                                image.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                            image.Save(path + "." + format);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Exports a Motion List Asset
        /// </summary>
        private string ExportMotionList(Package.Entry entry, string name)
        {
            var scale = float.TryParse(Settings["ModelScale", "1.0"], out var val) ? val : 1.0f;
            var motions = MotionList.Convert(ActivePackage.LoadEntry(entry));

            var path = name.Split('.')[0];
            var folder = Path.Combine(path, motions.Item1);

            var result = path;

            Directory.CreateDirectory(folder);

            foreach(var motion in motions.Item2)
            {
                var motionPath = Path.Combine(folder, motion.Item1);

                if (Settings["ExportSEAnim", "Yes"] == "Yes")
                {
                    motion.Item2.Scale(scale);
                    motion.Item2.ToSEAnim(motionPath + ".seanim");
                }
            }

            return result;
        }

        /// <summary>
        /// Exports Material Info to a .txt file
        /// </summary>
        private void ExportMaterialInfo(Model.Material material, string path)
        {
            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("# Textures");
                    foreach (var texture in material.Images)
                        writer.WriteLine("{0}:{1}", texture.Key, texture.Value);
                    writer.WriteLine("# Settings");

                    foreach (var setting in material.Settings)
                    {
                        writer.Write("{0}:", setting.Key);

                        var things = (float[])setting.Value;

                        foreach (var thing in things)
                            writer.Write("{0} ", thing);

                        writer.WriteLine();
                    }
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Exports Material Images
        /// </summary>
        private void ExportMaterialImages(Model.Material material, string folder)
        {
            var imageKeys = material.Images.Keys.ToArray();

            foreach (var key in imageKeys)
            {
                // Resolve, we try the streaming folder first, as they contain the highest quality images
                // vs the lowest ones loaded in memory
                foreach (var prefix in DirectoryPrefixes)
                {
                    var fullPath = prefix + material.Images[key];

                    foreach (var suffix in FileSuffixes)
                    {
                        if (ActivePackage.Entries.TryGetValue(MurMur3.Calculate(Encoding.Unicode.GetBytes(fullPath + suffix)), out var imageFile))
                        {
                            try
                            {
                                var imagePath = ExportImage(imageFile, Path.Combine(folder, "_images", Path.GetFileNameWithoutExtension(fullPath)));
                                Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                                material.Images[key] = Path.Combine("_images", Path.GetFileName(imagePath));
                                break;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
            }
        }

        private void ExportUnknownFile(Package.Entry entry, string name)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(name));

            File.WriteAllBytes(name, ActivePackage.LoadEntry(entry));
        }

        /// <summary>
        /// Exports a Model Asset
        /// </summary>
        private void ExportModel(Package.Entry entry, string name)
        {
            var scale = float.TryParse(Settings["ModelScale", "1.0"], out var val) ? val : 1.0f;
            var models = Mesh.Convert(ActivePackage.LoadEntry(entry));

            var path = name.Split('.')[0];

            var materials = new Dictionary<string, Model.Material>();

            // Attempt to load the materials file
            foreach (var prefix in FileSuffixes)
            {
                if (ActivePackage.Entries.TryGetValue(MurMur3.Calculate(Encoding.Unicode.GetBytes(path.Replace("exported_files\\", "") + prefix)), out var result))
                {
                    try
                    {
                        materials = MaterialDefs.Convert(ActivePackage.LoadEntry(result));
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }


            var folder = Path.GetDirectoryName(path);

            Directory.CreateDirectory(folder);

            for (int mdl = 0; mdl < models.Count; mdl++)
            {
                for (int lod = 0; lod < models[mdl].Count; lod++)
                {
                    // Must generate for formats that need it
                    models[mdl][lod].GenerateGlobalBoneData();
                    models[mdl][lod].Scale(scale);

                    foreach (var material in models[mdl][lod].Materials)
                    {
                        if (materials.TryGetValue(material.Name, out var fileMtl))
                        {
                            material.Images = fileMtl.Images;
                            material.Settings = fileMtl.Settings;

                            // Determine image keys as they change depending on shader/type
                            if (material.Images.ContainsKey("BaseMetalMap"))
                                material.DiffuseMapName = "BaseMetalMap";
                            else if (material.Images.ContainsKey("BaseMap"))
                                material.DiffuseMapName = "BaseMap";

                            if (material.Images.ContainsKey("NormalRoughnessMap"))
                                material.NormalMapName = "NormalRoughnessMap";
                            else if (material.Images.ContainsKey("NormalMap"))
                                material.NormalMapName = "NormalMap";
                        }
                    }

                    foreach (var material in models[mdl][lod].Materials)
                    {
                        if(Settings["ExportMaterialInfo", "Yes"] == "Yes")
                            ExportMaterialInfo(material, Path.Combine(folder, material.Name + ".txt"));

                        if (Settings["ExportImagesWithModel", "Yes"] == "Yes")
                            ExportMaterialImages(material, folder);
                    }

                    var result = Path.Combine(folder, string.Format("{0}_model{1}_lod{2}", Path.GetFileNameWithoutExtension(path), mdl, lod));

                    foreach(var format in ModelFormats)
                    {
                        // PNG is default
                        if (Settings["Export" + format.ToUpper(), format == "semodel" ? "Yes" : "No"] == "Yes")
                        {
                            models[mdl][lod].Save(result + "." + format);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exports the given asset
        /// </summary>
        /// <param name="asset"></param>
        private void ExportAsset(Asset asset)
        {
            try
            {
                Log(string.Format("Exporting {0}", Path.GetFileNameWithoutExtension(asset.Name)), "INFO");

                switch (asset.Type)
                {
                    case "Mesh":
                        ExportModel(asset.PackageEntry, Path.Combine("exported_files", asset.Name));
                        break;
                    case "Texture":
                        ExportImage(asset.PackageEntry, Path.Combine("exported_files", asset.Name));
                        break;
                    case "MotionList":
                        ExportMotionList(asset.PackageEntry, Path.Combine("exported_files", asset.Name));
                        break;
                    default:
                        ExportUnknownFile(asset.PackageEntry, Path.Combine("exported_files", asset.Name));
                        break;
                }

                Log(string.Format("Exported {0}", Path.GetFileNameWithoutExtension(asset.Name)), "INFO");

            }
            catch (Exception e)
            {
                Log(string.Format("Error has occured while exporting {0}: {1}", Path.GetFileNameWithoutExtension(asset.Name), e), "ERROR");
            }
        }

        /// <summary>
        /// Exports the asset on double click
        /// </summary>
        private void AssetListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is Asset asset)
            {
                ExportAssets(new List<Asset>()
                {
                    asset
                });
            }
        }

        /// <summary>
        /// Writes to the log in debug
        /// </summary>
        private void Log(string value, string messageType)
        {
            lock(LogStream)
            {
                LogStream.WriteLine("{0} [ {1} ] {2}", DateTime.Now.ToString("dd-MM-yyyy - HH:mm:ss"), messageType.PadRight(12), value);
                LogStream.Flush();
            }
        }

        /// <summary>
        /// Opens file dialog to open a package
        /// </summary>
        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Assets.ClearAssets();
            ActivePackage?.Dispose();
            ActivePackage = null;
            GC.Collect();

            var dialog = new OpenFileDialog()
            {
                Title = "Tyrant | Open File",
                Filter = "Package files (*.pak)|*.pak|All files (*.*)|*.*"
            };

            if(dialog.ShowDialog() == true)
            {
                if(Path.GetExtension(dialog.FileName).ToLower() == ".pak")
                {
                    LoadPackage(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// Opens settings window
        /// </summary>
        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow()
            {
                Owner = this
            };

            Dimmer.Visibility = Visibility.Visible;

            settings.SetUIFromSettings(Settings);
            settings.ShowDialog();
            settings.SetSettingsFromUI(Settings);

            Dimmer.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Sets the dimmer
        /// </summary>
        private void SetDimmer(Visibility visibility) => Dispatcher.BeginInvoke(new Action(() => Dimmer.Visibility = visibility));

        /// <summary>
        /// Exports the list of assets
        /// </summary>
        public void ExportAssets(List<Asset> assets)
        {
            if (ActivePackage == null)
                return;

            var progressWindow = new ProgressWindow()
            {
                Owner = this,
            };

            Dispatcher.BeginInvoke(new Action(() => progressWindow.ShowDialog()));
            progressWindow.SetProgressCount(assets.Count);

            new Thread(() =>
            {
                SetDimmer(Visibility.Visible);

                Parallel.ForEach(assets, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (asset, loop) =>
                {
                    ExportAsset(asset);

                    if (progressWindow.IncrementProgress() || EndThread)
                        loop.Break();
                });

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    GC.Collect();
                    progressWindow.Complete();
                    SetDimmer(Visibility.Hidden);
                }));
            }).Start();
        }

        /// <summary>
        /// Exports selected assets
        /// </summary>
        private void ExportSelectedClick(object sender, RoutedEventArgs e)
        {
            var assets = AssetList.Items.Cast<Asset>().ToList();

            if (assets.Count == 0)
            {
                SetDimmer(Visibility.Visible);
                MessageBox.Show("There are no assets selected to export. Select assets in the list first.", "Tyrant | Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetDimmer(Visibility.Hidden);
                return;
            }
            ExportAssets(assets);
        }

        /// <summary>
        /// Exports all loaded assets
        /// </summary>
        private void ExportAllClick(object sender, RoutedEventArgs e)
        {
            var assets = AssetList.Items.Cast<Asset>().ToList();

            if (assets.Count == 0)
            {
                MessageBox.Show("There are no assets listed to export. Load a Package file first.", "Tyrant | Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ExportAssets(assets);
        }

        /// <summary>
        /// Opens about window
        /// </summary>
        private void AboutClick(object sender, RoutedEventArgs e)
        {
            SetDimmer(Visibility.Visible);
            new AboutWindow()
            {
                Owner = this
            }.ShowDialog();
            SetDimmer(Visibility.Hidden);
        }
    }
}
