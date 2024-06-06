using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using static Vocabulary.MainWindow;

namespace Vocabulary
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private string vocabularyFilePath;
        private MainWindow mainWindow;
        private string newVocabularyFilePath;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsWindow(string vocabularyFilePath, MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.vocabularyFilePath = System.IO.Path.GetFullPath(vocabularyFilePath);

            VocabularyFilePath = this.vocabularyFilePath;

            LoadSettingsFromJson();

            DataContext = this;
        }

        public string VocabularyFilePath
        {
            get { return vocabularyFilePath; }
            set
            {
                if (vocabularyFilePath != value)
                {
                    vocabularyFilePath = value;
                    OnPropertyChanged(nameof(VocabularyFilePath));
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ChangeDictionaryFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Текстові файли (*.txt)|*.txt|Усі файли (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(selectedFilePath);
                if (fileExtension.ToLower() == ".txt")
                {
                    newVocabularyFilePath = selectedFilePath;
                    VocabularyFilePath = selectedFilePath;
                }
                else
                {
                    MessageBox.Show("Оберіть файл з розширенням .txt", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MergeDictionaryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Текстові файли (*.txt)|*.txt|Усі файли (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                MergeDictionaries(selectedFilePath);
            }
        }

        private void MergeDictionaries(string mergeFilePath)
        {
            try
            {
                if (!File.Exists(mergeFilePath))
                {
                    MessageBox.Show("Обраний файл не існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var mergeVocabulary = new Dictionary<string, TermEntry>();
                string[] lines = File.ReadAllLines(mergeFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("+") && line.EndsWith("+"))
                    {
                        string[] parts = line.Substring(1, line.Length - 2).Split(':');
                        if (parts.Length == 2)
                        {
                            string term = parts[0];
                            string description = parts[1];
                            mergeVocabulary[term] = new TermEntry { Description = description, Comment = null };
                        }
                        else if (parts.Length == 1)
                        {
                            string comment = parts[0];
                            mergeVocabulary[comment] = new TermEntry { Description = null, Comment = comment };
                        }
                    }
                }

                if (mergeVocabulary.Count == 0)
                {
                    MessageBox.Show("Файл-словник порожній або не містить валідних термінів.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var duplicateCount = 0;
                var newTerms = new Dictionary<string, TermEntry>();

                foreach (var kvp in mergeVocabulary)
                {
                    if (mainWindow.vocabulary.ContainsKey(kvp.Key))
                    {
                        duplicateCount++;
                    }
                    else
                    {
                        newTerms[kvp.Key] = kvp.Value;
                    }
                }

                if (newTerms.Count == 0)
                {
                    MessageBox.Show("Всі терміни з обраного словника вже існують.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var sb = new StringBuilder();
                foreach (var kvp in mainWindow.vocabulary)
                {
                    if (kvp.Value.Description != null)
                    {
                        sb.AppendLine($"+{kvp.Key}:{kvp.Value.Description}+");
                    }
                    else if (kvp.Value.Comment != null)
                    {
                        sb.AppendLine($"+{kvp.Value.Comment}+");
                    }
                }

                sb.AppendLine($"+додано з файлу \"{Path.GetFileName(mergeFilePath)}\"+");

                foreach (var kvp in newTerms)
                {
                    if (kvp.Value.Description != null)
                    {
                        sb.AppendLine($"+{kvp.Key}:{kvp.Value.Description}+");
                    }
                    else if (kvp.Value.Comment != null)
                    {
                        sb.AppendLine($"+{kvp.Value.Comment}+");
                    }
                    mainWindow.vocabulary[kvp.Key] = kvp.Value;
                }

                File.WriteAllText(mainWindow.VocabularyFilePath, sb.ToString(), Encoding.UTF8);
                mainWindow.UpdateTermItems();

                if (duplicateCount > 0)
                {
                    MessageBox.Show($"Знайдено {duplicateCount} однакових термінів, які не було додано.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка об'єднання словників: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.VocabularyFilePath = string.IsNullOrEmpty(newVocabularyFilePath) ? vocabularyFilePath : newVocabularyFilePath;
            mainWindow.LoadVocabularyFromFile(mainWindow.VocabularyFilePath);
            mainWindow.UpdateTermItems();
            SaveSettingsToJson();
            Close();
        }

        private void SaveSettingsToJson()
        {
            var settings = new UserSettings
            {
                VocabularyFilePath = VocabularyFilePath
            };

            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText("settings.json", json);
        }

        private void LoadSettingsFromJson()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    string json = File.ReadAllText("settings.json");
                    var settings = JsonConvert.DeserializeObject<UserSettings>(json);

                    VocabularyFilePath = settings.VocabularyFilePath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження налаштувань: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UserSettings
    {
        public string VocabularyFilePath { get; set; }
    }
}