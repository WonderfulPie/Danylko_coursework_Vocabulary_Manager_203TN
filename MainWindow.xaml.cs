using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;

namespace Vocabulary
{
    public partial class MainWindow : Window
    {
        public Dictionary<string, TermEntry> vocabulary = new Dictionary<string, TermEntry>();
        private string vocabularyFilePath = "vocabulary.txt";
        private ObservableCollection<Term> terms;

        public string VocabularyFilePath
        {
            get { return vocabularyFilePath; }
            set
            {
                if (vocabularyFilePath != value)
                {
                    vocabularyFilePath = value;
                    LoadVocabularyFromFile(vocabularyFilePath);
                    UpdateTermItems();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadSettingsFromJson();
            InitializeLetterList();
            terms = new ObservableCollection<Term>();
            termItemsControl.ItemsSource = terms;
            LoadVocabularyFromFile(vocabularyFilePath);
        }

        private void LoadSettingsFromJson()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    string json = File.ReadAllText("settings.json");
                    UserSettings settings = JsonConvert.DeserializeObject<UserSettings>(json);
                    if (settings != null && File.Exists(settings.VocabularyFilePath))
                    {
                        vocabularyFilePath = settings.VocabularyFilePath;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження налаштувань: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeLetterList()
        {
            var letters = new List<string> { "*" };
            letters.AddRange(new[] { "А", "Б", "В", "Г", "Ґ", "Д", "Е", "Є", "Ж", "З", "И", "І", "Ї", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ь", "Ю", "Я" });
            letterListBox.ItemsSource = letters;
        }

        public void LoadVocabularyFromFile(string filePath)
        {
            try
            {
                vocabulary.Clear();
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("+") && line.EndsWith("+"))
                    {
                        string[] parts = line.Substring(1, line.Length - 2).Split(':');
                        if (parts.Length == 2)
                        {
                            string term = parts[0];
                            string description = parts[1];
                            vocabulary[term] = new TermEntry { Description = description, Comment = null };
                        }
                        else if (parts.Length == 1)
                        {
                            string comment = parts[0];
                            vocabulary[comment] = new TermEntry { Description = null, Comment = comment };
                        }
                    }
                }
                UpdateTermItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження словника: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveVocabularyToFile(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var kvp in vocabulary)
                    {
                        if (kvp.Value.Description != null)
                        {
                            writer.WriteLine($"+{kvp.Key}:{kvp.Value.Description}+");
                        }
                        else if (kvp.Value.Comment != null)
                        {
                            writer.WriteLine($"+{kvp.Value.Comment}+");
                        }
                    }
                }
                UpdateTermCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження словника: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateTermItems()
        {
            terms.Clear();
            foreach (var kvp in vocabulary.Where(kv => kv.Value.Description != null).OrderBy(kv => kv.Key))
            {
                terms.Add(new Term { Key = kvp.Key, Value = kvp.Value.Description });
            }
            UpdateTermCount();
        }

        private void UpdateTermCount()
        {
            termCountTextBlock.Text = $"Кількість термінів: {vocabulary.Count(kv => kv.Value.Description != null)}";
        }

        private void AddTermButton_Click(object sender, RoutedEventArgs e)
        {
            // Створення словника для передачі в AddTermWindow
            var existingVocabulary = vocabulary
                .Where(kvp => kvp.Value.Description != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Description);

            // Відкриття вікна для додавання терміну
            AddTermWindow addTermWindow = new AddTermWindow(existingVocabulary);
            addTermWindow.Owner = this;
            if (addTermWindow.ShowDialog() == true)
            {
                string term = addTermWindow.TermName;
                string description = addTermWindow.Description;

                // Перевірка наявності терміну в словнику
                if (vocabulary.ContainsKey(term))
                {
                    // Виведення попередження та запиту на підтвердження
                    var result = MessageBox.Show("Термін з такою назвою вже існує. Ви впевнені, що хочете додати новий термін?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Додавання нового терміну з нумерацією
                        string newTerm = term;
                        int counter = 2;
                        while (vocabulary.ContainsKey(newTerm))
                        {
                            newTerm = $"{term}({counter})";
                            counter++;
                        }

                        vocabulary[newTerm] = new TermEntry { Description = description, Comment = null };
                        UpdateTermItems();
                        SaveVocabularyToFile(vocabularyFilePath);
                    }
                }
                else
                {
                    // Додавання нового терміну
                    vocabulary[term] = new TermEntry { Description = description, Comment = null };
                    UpdateTermItems();
                    SaveVocabularyToFile(vocabularyFilePath);
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = searchTextBox.Text.Trim();

            // Перевірка, чи введено щось у поле пошуку, ігноруючи Placeholder
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm == "Введіть текст для пошуку")
            {
                MessageBox.Show("Пошуковий запит не було введено!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            searchTerm = searchTerm.ToLower();

            var searchResults = vocabulary
                .Where(kv => kv.Key.ToLower().Contains(searchTerm) && kv.Value.Description != null)
                .OrderBy(kv => kv.Key)
                .ToList();

            terms.Clear();
            foreach (var kvp in searchResults)
            {
                terms.Add(new Term { Key = kvp.Key, Value = kvp.Value.Description });
            }

            UpdateFilteredTermCount(searchResults.Count);
        }

        private void letterListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (letterListBox.SelectedItem != null)
            {
                string selectedLetter = letterListBox.SelectedItem.ToString();
                if (selectedLetter == "*")
                {
                    UpdateTermItems();
                    UpdateFilteredTermCount(terms.Count); // Оновлення лічильника для всіх термінів
                }
                else
                {
                    var filteredResults = vocabulary
                        .Where(kv => kv.Key.StartsWith(selectedLetter, StringComparison.OrdinalIgnoreCase) && kv.Value.Description != null)
                        .OrderBy(kv => kv.Key)
                        .ToList();

                    terms.Clear();
                    foreach (var kvp in filteredResults)
                    {
                        terms.Add(new Term { Key = kvp.Key, Value = kvp.Value.Description });
                    }
                    // Оновлення лічильника з кількістю відфільтрованих термінів
                    UpdateFilteredTermCount(filteredResults.Count);
                }
            }
            else
            {
                terms.Clear();
                // Оновлення лічильника з кількістю відфільтрованих термінів
                UpdateFilteredTermCount(0);
            }
        }
        private void UpdateFilteredTermCount(int count)
        {
            termCountTextBlock.Text = $"Кількість термінів: {count}";
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(vocabularyFilePath, this);
            settingsWindow.ShowDialog();
        }

        private void DeleteTermButton_Click(object sender, RoutedEventArgs e)
        {
            if (terms.Any(t => t.IsSelected))
            {
                var termToDelete = terms.First(t => t.IsSelected);
                if (MessageBox.Show($"Ви впевнені, що хочете видалити термін '{termToDelete.Key}'?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    vocabulary.Remove(termToDelete.Key);
                    UpdateTermItems();
                    SaveVocabularyToFile(vocabularyFilePath);
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, оберіть термін для видалення.", "Термін не обрано", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TermSelected(object sender, MouseButtonEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if (stackPanel != null)
            {
                var selectedTerm = stackPanel.DataContext as Term;
                if (selectedTerm != null)
                {
                    foreach (var term in terms)
                    {
                        term.IsSelected = false;
                    }
                    selectedTerm.IsSelected = true;
                }
            }
        }

        public class TermEntry
        {
            public string Description { get; set; }
            public string Comment { get; set; }
        }

        public class Term : INotifyPropertyChanged
        {
            private bool isSelected;

            public string Key { get; set; }
            public string Value { get; set; }
            public bool IsSelected
            {
                get { return isSelected; }
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        OnPropertyChanged(nameof(IsSelected));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}