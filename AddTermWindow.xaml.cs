using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Vocabulary
{
    public partial class AddTermWindow : Window
    {
        private readonly Dictionary<string, string> existingVocabulary;

        public string TermName { get; private set; }
        public string Description { get; private set; }

        public AddTermWindow(Dictionary<string, string> vocabulary)
        {
            InitializeComponent();
            existingVocabulary = vocabulary;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            TermName = termNameTextBox.Text.Trim();
            Description = descriptionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(TermName))
            {
                MessageBox.Show("Назва терміну не може бути пустою.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                MessageBox.Show("Опис терміну не може бути пустим.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string normalizedNewTerm = NormalizeString(TermName);
            string normalizedNewDescription = NormalizeString(Description);

            foreach (var kvp in existingVocabulary)
            {
                if (NormalizeString(kvp.Key) == normalizedNewTerm && NormalizeString(kvp.Value) == normalizedNewDescription)
                {
                    MessageBox.Show("Термін з такою назвою та описом вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, @"\s+|\p{P}", "").ToLower();
        }
    }
}