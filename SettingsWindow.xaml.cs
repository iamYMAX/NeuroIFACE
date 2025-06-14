using System.Windows;
using System.Xml.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks; // For Task
using System.Net; // For WebUtility
using System.Windows.Media;

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
        private bool _isUpdatingSlidersProgrammatically = false;

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel; // ViewModel is now set

            // Initialize translation timers
            _debounceTimerRu = new Timer(DebounceTimerRuCallback, null, Timeout.Infinite, Timeout.Infinite);
            _debounceTimerEn = new Timer(DebounceTimerEnCallback, null, Timeout.Infinite, Timeout.Infinite);

            // Initialize sliders from ViewModel's current background color
            UpdateSlidersFromViewModelColor();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("SettingsWindow.CloseButton_Click: Closing Settings window.");
            this.Close(); // Закрытие окна
        }

        private void RussianTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (RussianTextBox.IsFocused && !_isTranslatingEn) // Only translate if this box has focus and the other isn't currently translating to this one
            {
                _debounceTimerRu.Change(DebounceMilliseconds, Timeout.Infinite);
            }
        }

        // ColorButton_Click removed

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
            System.Diagnostics.Debug.WriteLine($"SettingsWindow.AddPhrase_Click: Adding phrase Ru='{russianPhrase}', En='{englishPhrase}'");

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
            System.Diagnostics.Debug.WriteLine($"SettingsWindow.AddPhraseToXml: Adding Ru='{russian}', En='{english}' to Data.xml");
            XDocument doc;
            XElement appDataElement;
            XElement phrasesElement;
            string filePath = "Data.xml";

            if (System.IO.File.Exists(filePath))
            {
                doc = XDocument.Load(filePath);
                appDataElement = doc.Root;
                // Ensure root is AppData
                if (appDataElement == null || appDataElement.Name != "AppData")
                {
                    // If root is not AppData (e.g. old format or empty file), create new structure
                    // For simplicity, we'll recreate and try to preserve old phrases if they were at root
                    XElement oldPhrases = null;
                    if (appDataElement != null && appDataElement.Name == "Phrases")
                    {
                        oldPhrases = new XElement(appDataElement); // clone old root if it was Phrases
                    }
                    appDataElement = new XElement("AppData");
                    doc = new XDocument(appDataElement);
                    if (oldPhrases != null)
                    {
                        appDataElement.Add(oldPhrases); // Add old phrases under new AppData
                        phrasesElement = appDataElement.Element("Phrases");
                    }
                }
            }
            else
            {
                // File does not exist, create new structure
                appDataElement = new XElement("AppData");
                doc = new XDocument(appDataElement);
            }

            phrasesElement = appDataElement.Element("Phrases");
            if (phrasesElement == null)
            {
                phrasesElement = new XElement("Phrases");
                appDataElement.Add(phrasesElement);
            }

            phrasesElement.Add(new XElement("Translete",
                new XElement("Ru", russian),
                new XElement("En", english)));

            // Ensure Settings element exists if it's not already there, to maintain consistency with MainViewModel's save logic
            if (appDataElement.Element("Settings") == null)
            {
                appDataElement.Add(new XElement("Settings"));
            }

            // Ensure order: Phrases first, then Settings
            var currentPhrases = appDataElement.Element("Phrases");
            var currentSettings = appDataElement.Element("Settings");

            if (currentPhrases != null) currentPhrases.Remove();
            if (currentSettings != null) currentSettings.Remove();

            if (currentPhrases != null) appDataElement.Add(currentPhrases);
            if (currentSettings != null) appDataElement.Add(currentSettings);


            doc.Save(filePath);
        }

        private void RgbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingSlidersProgrammatically || _viewModel == null || RedSlider == null || GreenSlider == null || BlueSlider == null || ColorPreviewRectangle == null)
            {
                return; // Or wait until all are initialized if called during setup
            }

            byte r = (byte)RedSlider.Value;
            byte g = (byte)GreenSlider.Value;
            byte b = (byte)BlueSlider.Value;

            Color selectedColor = Color.FromArgb(255, r, g, b);
            ColorPreviewRectangle.Fill = new SolidColorBrush(selectedColor);

            string hexColor = $"#FF{r:X2}{g:X2}{b:X2}";
            _viewModel.MainWindowBackgroundColor = hexColor;
        }

        private void ManagePhrasesButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("SettingsWindow.ManagePhrasesButton_Click: Opening PhraseManagerWindow.");
            PhraseManagerWindow phraseManagerWindow = new PhraseManagerWindow();
            phraseManagerWindow.Owner = this; // Optional: to make it behave more like a child of this window
            phraseManagerWindow.ShowDialog();
            System.Diagnostics.Debug.WriteLine("SettingsWindow.ManagePhrasesButton_Click: PhraseManagerWindow closed.");
            // After PhraseManagerWindow is closed, you might want to refresh things
            // in SettingsWindow or MainViewModel if changes in PhraseManagerWindow
            // could affect them. For now, RandomPhraseModel will pick up changes
            // from Data.xml upon next phrase generation or app restart.
            // If MainViewModel's RandomPhraseModel instance needs to be refreshed immediately,
            // that would require more direct communication or eventing.
        }

        private void UpdateSlidersFromViewModelColor()
        {
            if (_viewModel == null || RedSlider == null || GreenSlider == null || BlueSlider == null || ColorPreviewRectangle == null) return;

            _isUpdatingSlidersProgrammatically = true;
            try
            {
                string hexColor = _viewModel.MainWindowBackgroundColor;
                if (!string.IsNullOrEmpty(hexColor) && hexColor.StartsWith("#"))
                {
                    if (hexColor.Length == 9) // #AARRGGBB
                    {
                        // We ignore the ViewModel's Alpha, sliders control RGB, window opacity controls overall alpha.
                        byte r = byte.Parse(hexColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                        byte g = byte.Parse(hexColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                        byte b = byte.Parse(hexColor.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);

                        RedSlider.Value = r;
                        GreenSlider.Value = g;
                        BlueSlider.Value = b;

                        Color previewColor = Color.FromArgb(255, r, g, b);
                        ColorPreviewRectangle.Fill = new SolidColorBrush(previewColor);
                    }
                    else if (hexColor.Length == 7) // #RRGGBB
                    {
                        byte r = byte.Parse(hexColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                        byte g = byte.Parse(hexColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                        byte b = byte.Parse(hexColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);

                        RedSlider.Value = r;
                        GreenSlider.Value = g;
                        BlueSlider.Value = b;

                        Color previewColor = Color.FromArgb(255, r, g, b);
                        ColorPreviewRectangle.Fill = new SolidColorBrush(previewColor);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing ViewModel color for sliders: {ex.Message}");
                // Optionally set sliders to a default, e.g., black
                RedSlider.Value = 0;
                GreenSlider.Value = 0;
                BlueSlider.Value = 0;
                ColorPreviewRectangle.Fill = new SolidColorBrush(Color.FromArgb(255,0,0,0));
            }
            finally
            {
                _isUpdatingSlidersProgrammatically = false;
            }
        }
    }
}
