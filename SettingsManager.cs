using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Vocabulary
{
    [Serializable]
    public class AppSettings
    {
        public string VocabularyFilePath { get; set; }

        public static void SaveSettings(AppSettings settings, string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, settings);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving settings: " + ex.Message);
            }
        }

        public static AppSettings LoadSettings(string filePath)
        {
            AppSettings settings = null;
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    settings = (AppSettings)bf.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading settings: " + ex.Message);
            }
            return settings;
        }
    }
}
