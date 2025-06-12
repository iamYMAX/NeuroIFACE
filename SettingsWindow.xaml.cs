using System.Windows;
using System.Xml.Linq;

namespace NeuroIFACE
{
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрытие окна
        }
        private void AddPhrase_Click(object sender, RoutedEventArgs e)
        {
            string russianPhrase = RussianTextBox.Text;
            string englishPhrase = EnglishTextBox.Text;

            if (!string.IsNullOrEmpty(russianPhrase) && !string.IsNullOrEmpty(englishPhrase))
            {
                AddPhraseToXml(russianPhrase, englishPhrase);
                MessageBox.Show("Фраза добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RussianTextBox.Clear();
                EnglishTextBox.Clear();
            }
            else
            {
                MessageBox.Show("Пожалуйста, заполните оба поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPhraseToXml(string russian, string english)
        {
            XDocument doc = XDocument.Load("Data.xml");
            doc.Root.Add(new XElement("Translete",
                new XElement("Ru", russian),
                new XElement("En", english)));
            doc.Save("Data.xml");
        }
    }
}
