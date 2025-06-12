using System.Windows;
using System.Xml.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks; // For Task
using System.Net; // For WebUtility

namespace NeuroIFACE
{
    // Helper classes for JSON deserialization
    public class TranslationResponseData
    {
        public string translatedText { get; set; }
    }

    public class MyMemoryApiResponse
    {
        public TranslationResponseData responseData { get; set; }
        public int responseStatus { get; set; }
    }

    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;
        private static readonly HttpClient httpClient = new HttpClient();
        private Timer _debounceTimerRu;
        private Timer _debounceTimerEn;
        private bool _isTranslatingRu = false; // Flags to prevent re-entrant translation calls
        private bool _isTranslatingEn = false;
        private const int DebounceMilliseconds = 500;

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            _debounceTimerRu = new Timer(DebounceTimerRuCallback, null, Timeout.Infinite, Timeout.Infinite);
            _debounceTimerEn = new Timer(DebounceTimerEnCallback, null, Timeout.Infinite, Timeout.Infinite);
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрытие окна
        }

        private void RussianTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (RussianTextBox.IsFocused && !_isTranslatingEn) // Only translate if this box has focus and the other isn't currently translating to this one
            {
                _debounceTimerRu.Change(DebounceMilliseconds, Timeout.Infinite);
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button clickedButton)
            {
                if (clickedButton.Tag is string colorHex)
                {
                    _viewModel.MainWindowBackgroundColor = colorHex;
                }
            }
        }

        private void EnglishTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (EnglishTextBox.IsFocused && !_isTranslatingRu) // Only translate if this box has focus and the other isn't currently translating to this one
            {
                _debounceTimerEn.Change(DebounceMilliseconds, Timeout.Infinite);
            }
        }

        private async void DebounceTimerRuCallback(object state)
        {
            string textToTranslate = null;
            await Dispatcher.InvokeAsync(() => // Ensure we are on the UI thread to access TextBox.Text
            {
                textToTranslate = RussianTextBox.Text;
            });

            if (!string.IsNullOrWhiteSpace(textToTranslate))
            {
                await TranslateAndSetText(textToTranslate, "ru|en", EnglishTextBox, true);
            }
        }

        private async void DebounceTimerEnCallback(object state)
        {
            string textToTranslate = null;
            await Dispatcher.InvokeAsync(() => // Ensure we are on the UI thread to access TextBox.Text
            {
                textToTranslate = EnglishTextBox.Text;
            });

            if (!string.IsNullOrWhiteSpace(textToTranslate))
            {
                await TranslateAndSetText(textToTranslate, "en|ru", RussianTextBox, false);
            }
        }

        private async Task TranslateAndSetText(string text, string langPair, System.Windows.Controls.TextBox targetTextBox, bool isSourceRu)
        {
            if (isSourceRu) _isTranslatingRu = true; else _isTranslatingEn = true;

            try
            {
                await Dispatcher.InvokeAsync(() => targetTextBox.IsEnabled = false); // Disable target textbox during translation

                string encodedText = WebUtility.UrlEncode(text);
                string apiUrl = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={langPair}";

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode(); // Throw if not success

                string jsonResponse = await response.Content.ReadAsStringAsync();
                MyMemoryApiResponse apiResponse = JsonSerializer.Deserialize<MyMemoryApiResponse>(jsonResponse);

                if (apiResponse != null && apiResponse.responseStatus == 200 && apiResponse.responseData != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // Only update if the source textbox still has focus
                        // This prevents updating the textbox if the user tabbed away quickly
                        if ((isSourceRu && RussianTextBox.IsFocused) || (!isSourceRu && EnglishTextBox.IsFocused))
                        {
                           targetTextBox.Text = apiResponse.responseData.translatedText;
                        }
                    });
                }
                else
                {
                    // Optionally: handle cases where translation wasn't successful but status was 200
                    await Dispatcher.InvokeAsync(() => targetTextBox.Clear()); // Clear if no valid translation
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle HTTP errors (e.g., network issue, API down)
                // For now, just clear the target and log (or show a message)
                System.Diagnostics.Debug.WriteLine($"HTTP request error: {httpEx.Message}");
                await Dispatcher.InvokeAsync(() => targetTextBox.Clear());
            }
            catch (JsonException jsonEx)
            {
                // Handle JSON parsing errors
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                await Dispatcher.InvokeAsync(() => targetTextBox.Clear());
            }
            catch (Exception ex)
            {
                // Handle other unexpected errors
                System.Diagnostics.Debug.WriteLine($"Translation error: {ex.Message}");
                await Dispatcher.InvokeAsync(() => targetTextBox.Clear());
            }
            finally
            {
                await Dispatcher.InvokeAsync(() => targetTextBox.IsEnabled = true); // Re-enable target textbox
                if (isSourceRu) _isTranslatingRu = false; else _isTranslatingEn = false;
            }
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
