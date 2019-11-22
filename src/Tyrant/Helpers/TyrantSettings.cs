using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tyrant
{
    public class TyrantSettings
    {
        /// <summary>
        /// Setting Values
        /// </summary>
        private Dictionary<string, string> Values = new Dictionary<string, string>();

        /// <summary>
        /// Gets the setting with the given name, if not found, returns defaultVal
        /// </summary>
        public string this[string key, string defaultVal]
        {
            get
            {
                if (Values.TryGetValue(key, out var val))
                {
                    return val;
                }
                else
                {
                    Values[key] = defaultVal;
                    return defaultVal;
                }
            }
        }

        /// <summary>
        /// Sets the setting with the given name
        /// </summary>
        public string this[string key]
        {
            set
            {
                Values[key] = value;
            }
        }

        /// <summary>
        /// Initializes an instance of the Settings Class
        /// </summary>
        public TyrantSettings() { }

        /// <summary>
        /// Initializes an instance of the Settings Class and loads the settings
        /// </summary>
        /// <param name="fileName">File Name</param>
        public TyrantSettings(string fileName)
        {
            Load(fileName);
        }

        /// <summary>
        /// Loads Settings from a file
        /// </summary>
        /// <param name="fileName">File Name</param>
        public void Load(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    Save(fileName);
                }
                else
                {
                    using (var reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
                    {
                        if (reader.ReadUInt32() == 0x47464354)
                        {
                            int count = reader.ReadInt32();

                            for (int i = 0; i < count; i++)
                                Values[reader.ReadString()] = reader.ReadString();
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Saves all settings to a file
        /// </summary>
        /// <param name="fileName">File Name</param>
        public void Save(string fileName)
        {
            try
            {
                using (var writer = new BinaryWriter(new FileStream(fileName, FileMode.Create)))
                {
                    writer.Write(0x47464354);
                    writer.Write(Values.Count);

                    foreach (var value in Values)
                    {
                        writer.Write(value.Key);
                        writer.Write(value.Value);
                    }
                }
            }
            catch
            {
                return;
            }
        }
    }
}
