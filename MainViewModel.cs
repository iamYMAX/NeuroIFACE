using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.IO;
using System.Xml.Linq;
using System.Globalization;

namespace NeuroIFACE
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string _filePath = "Data.xml";
        private readonly RandomPhraseModel _phraseModel;
        private readonly DispatcherTimer _timer;
        private string _currentPhrase;
        private double _sliderValue = 1; // Default value
        private double _fontSize = 14; // Default value
        private string _mainWindowBackgroundColor = "#FF222222"; // Default: Solid Dark Gray
        private double _mainWindowOpacity = 0.8;          // Default: 80% opacity

        public string MainWindowBackgroundColor
        {
            get { return _mainWindowBackgroundColor; }
            set
            {
                if (_mainWindowBackgroundColor != value)
                {
                    _mainWindowBackgroundColor = value;
                    OnPropertyChanged();
                    SaveAppSettings();
                }
            }
        }

        public double MainWindowOpacity
        {
            get { return _mainWindowOpacity; }
            set
            {
                if (_mainWindowOpacity != value)
                {
                    _mainWindowOpacity = value;
                    OnPropertyChanged();
                    SaveAppSettings();
                }
            }
        }
        public string CurrentPhrase
        {
            get { return _currentPhrase; }
            set
            {
                _currentPhrase = value;
                OnPropertyChanged();
                // No SaveAppSettings() here as CurrentPhrase is transient
            }
        }
        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                    SaveAppSettings();
                }
            }
        }
        public double SliderValue
        {
            get { return _sliderValue; }
            set
            {
                if (_sliderValue != value)
                {
                    _sliderValue = value;
                    OnPropertyChanged();
                    UpdateTimerInterval();
                    SaveAppSettings();
                }
            }
        }

        public MainViewModel()
        {
            _phraseModel = new RandomPhraseModel();
            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => UpdatePhrase();

            LoadAppSettings(); // Load settings, or save current defaults.

            UpdateTimerInterval(); // Call after SliderValue is potentially loaded.
        }

        private void LoadAppSettings()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    SaveAppSettings();
                    return;
                }

                XDocument doc = XDocument.Load(_filePath);
                if (doc.Root == null || doc.Root.Name != "AppData")
                {
                    XElement phrasesElement = null;
                    if (doc.Root != null && doc.Root.Name == "Phrases")
                    {
                        phrasesElement = new XElement(doc.Root);
                    }
                    SaveAppSettings(phrasesElement);
                    return;
                }

                XElement settingsElement = doc.Root.Element("Settings");
                if (settingsElement == null)
                {
                    SaveAppSettings(doc.Root.Element("Phrases"));
                    return;
                }

                string bgColor = settingsElement.Element("MainWindowBackgroundColor")?.Value;
                if (!string.IsNullOrEmpty(bgColor)) _mainWindowBackgroundColor = bgColor;

                string opacityStr = settingsElement.Element("MainWindowOpacity")?.Value;
                if (double.TryParse(opacityStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double opacityVal))
                {
                    _mainWindowOpacity = opacityVal;
                }

                string fontSizeStr = settingsElement.Element("FontSize")?.Value;
                if (double.TryParse(fontSizeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double fontSizeVal))
                {
                    _fontSize = fontSizeVal;
                }

                string sliderValueStr = settingsElement.Element("SliderValue")?.Value;
                if (double.TryParse(sliderValueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double sliderVal))
                {
                    _sliderValue = sliderVal;
                }

                OnPropertyChanged(nameof(MainWindowBackgroundColor));
                OnPropertyChanged(nameof(MainWindowOpacity));
                OnPropertyChanged(nameof(FontSize));
                OnPropertyChanged(nameof(SliderValue));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}. Applying and saving defaults.");
                SaveAppSettings();
            }
        }

        private void SaveAppSettings(XElement existingPhrasesElementToPreserve = null)
        {
            try
            {
                XDocument doc;
                XElement appDataRoot;

                if (File.Exists(_filePath))
                {
                    try
                    {
                        doc = XDocument.Load(_filePath);
                        appDataRoot = doc.Root;
                        if (appDataRoot == null || appDataRoot.Name != "AppData")
                        {
                            if (existingPhrasesElementToPreserve == null && appDataRoot != null && appDataRoot.Name == "Phrases")
                            {
                                existingPhrasesElementToPreserve = new XElement(appDataRoot); // Preserve old <Phrases> root
                            }
                            appDataRoot = new XElement("AppData");
                            doc = new XDocument(appDataRoot);
                        }
                    }
                    catch
                    {
                        appDataRoot = new XElement("AppData");
                        doc = new XDocument(appDataRoot);
                    }
                }
                else
                {
                    appDataRoot = new XElement("AppData");
                    doc = new XDocument(appDataRoot);
                }

                XElement settingsElement = appDataRoot.Element("Settings");
                if (settingsElement == null)
                {
                    settingsElement = new XElement("Settings");
                    appDataRoot.Add(settingsElement);
                }

                settingsElement.SetElementValue("MainWindowBackgroundColor", _mainWindowBackgroundColor);
                settingsElement.SetElementValue("MainWindowOpacity", _mainWindowOpacity.ToString(CultureInfo.InvariantCulture));
                settingsElement.SetElementValue("FontSize", _fontSize.ToString(CultureInfo.InvariantCulture));
                settingsElement.SetElementValue("SliderValue", _sliderValue.ToString(CultureInfo.InvariantCulture));

                if (appDataRoot.Element("Phrases") == null)
                {
                    if (existingPhrasesElementToPreserve != null)
                    {
                        appDataRoot.Add(existingPhrasesElementToPreserve);
                    }
                    else
                    {
                        appDataRoot.Add(new XElement("Phrases")); // Add empty <Phrases> if none existed
                    }
                }

                var phrasesCurrent = appDataRoot.Element("Phrases");
                var settingsCurrent = appDataRoot.Element("Settings");

                // Ensure order: Phrases first, then Settings
                if (phrasesCurrent != null) phrasesCurrent.Remove();
                if (settingsCurrent != null) settingsCurrent.Remove();

                if (phrasesCurrent != null) appDataRoot.Add(phrasesCurrent);
                else if (existingPhrasesElementToPreserve != null) appDataRoot.Add(existingPhrasesElementToPreserve); // Re-add if it was the only thing
                else appDataRoot.Add(new XElement("Phrases")); // Ensure Phrases element exists

                if (settingsCurrent != null) appDataRoot.Add(settingsCurrent); // Re-add settings
                else appDataRoot.Add(new XElement("Settings", // Or create if it didn't exist but should
                    new XElement("MainWindowBackgroundColor", _mainWindowBackgroundColor),
                    new XElement("MainWindowOpacity", _mainWindowOpacity.ToString(CultureInfo.InvariantCulture)),
                    new XElement("FontSize", _fontSize.ToString(CultureInfo.InvariantCulture)),
                    new XElement("SliderValue", _sliderValue.ToString(CultureInfo.InvariantCulture))
                ));

                doc.Save(_filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void UpdateTimerInterval()
        {
            // Интервал зависит от значения слайдера (переводим из диапазона 1-25 в миллисекунды)
            _timer.Interval = TimeSpan.FromMilliseconds(1000 / SliderValue);
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        private void UpdatePhrase()
        {
            CurrentPhrase = _phraseModel.GetRandomPhrase();
        }

        public void SetSliderValue(double value)
        {
            SliderValue = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
