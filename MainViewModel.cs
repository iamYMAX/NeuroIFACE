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
            System.Diagnostics.Debug.WriteLine("MainViewModel: Constructor called.");
            _phraseModel = new RandomPhraseModel();
            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => UpdatePhrase();

            LoadAppSettings(); // Load settings, or save current defaults.

            UpdateTimerInterval(); // Call after SliderValue is potentially loaded.
            System.Diagnostics.Debug.WriteLine("MainViewModel: Constructor finished.");
        }

        private void LoadAppSettings()
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: Attempting to load application settings...");
            try
            {
                if (!File.Exists(_filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"MainViewModel.LoadAppSettings: {_filePath} not found. Calling SaveAppSettings() to create defaults.");
                    SaveAppSettings(); // This will create Data.xml with default settings and empty/default phrases
                    System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: SaveAppSettings() called, returning.");
                    return;
                }

                XDocument doc = XDocument.Load(_filePath);
                XElement appDataRoot = doc.Root;

                // If Data.xml is empty or doesn't have a root, or root is not AppData
                if (appDataRoot == null || appDataRoot.Name != "AppData")
                {
                    XElement phrasesToPreserve = null;
                    if (appDataRoot != null && appDataRoot.Name == "Phrases")
                    {
                        // Old format: root is <Phrases>
                        System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: Old format detected (Phrases as root). Migrating to AppData structure.");
                        phrasesToPreserve = new XElement(appDataRoot); // Clone the old <Phrases> root
                    }
                    else if (appDataRoot != null && appDataRoot.Name == "AppData" && appDataRoot.Element("Phrases") != null)
                    {
                        // This case should ideally not be hit if AppData is root, but as a safeguard
                        System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: AppData root found, but will ensure Phrases are preserved if SaveAppSettings is called.");
                        phrasesToPreserve = new XElement(appDataRoot.Element("Phrases"));
                    }
                    else if (appDataRoot != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"MainViewModel.LoadAppSettings: Unrecognized root element '{appDataRoot.Name}'. Proceeding to SaveAppSettings.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"MainViewModel.LoadAppSettings: File {_filePath} seems to be empty or corrupted. Proceeding to SaveAppSettings.");
                    }

                    System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: Calling SaveAppSettings due to incorrect/old XML structure.");
                    SaveAppSettings(phrasesToPreserve);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: AppData root found.");
                XElement settingsElement = appDataRoot.Element("Settings");
                if (settingsElement == null)
                {
                    System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: <Settings> element not found under <AppData>. Calling SaveAppSettings to create it.");
                    SaveAppSettings(appDataRoot.Element("Phrases")); // Preserve existing phrases if any
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: <Settings> element found. Loading values.");
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
                System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: Settings loaded and properties updated.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainViewModel.LoadAppSettings: Exception: {ex.Message}. Applying and saving defaults.");
                // In case of any exception during loading (e.g., file corruption),
                // try to save current (default or last known good) settings,
                // preserving phrases if possible by trying to read them one last time,
                // or just save settings with empty phrases.
                XElement phrases = null;
                try
                {
                    if(File.Exists(_filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"MainViewModel.LoadAppSettings (Exception Handler): Attempting to read {_filePath} to preserve phrases.");
                        XDocument tempDoc = XDocument.Load(_filePath); // Potential re-throw if file is severely corrupted.
                        if (tempDoc.Root != null && tempDoc.Root.Name == "AppData" && tempDoc.Root.Element("Phrases") != null)
                        {
                            phrases = new XElement(tempDoc.Root.Element("Phrases"));
                            System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings (Exception Handler): Preserved Phrases from AppData/Phrases.");
                        }
                        else if (tempDoc.Root != null && tempDoc.Root.Name == "Phrases")
                        {
                            phrases = new XElement(tempDoc.Root);
                            System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings (Exception Handler): Preserved Phrases from old format (root is Phrases).");
                        }
                    }
                }
                catch (Exception e) // Catch exceptions specifically from trying to preserve phrases
                {
                    System.Diagnostics.Debug.WriteLine($"MainViewModel.LoadAppSettings (Exception Handler): Exception while trying to preserve phrases: {e.Message}");
                }
                SaveAppSettings(phrases); // Save defaults, potentially with preserved phrases
            }
            System.Diagnostics.Debug.WriteLine("MainViewModel.LoadAppSettings: Finished.");
        }

        private void SaveAppSettings(XElement phrasesElementToPreserve = null)
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: Attempting to save application settings...");
            if (phrasesElementToPreserve != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: A phrasesElementToPreserve was provided.");
            }
            try
            {
                XDocument doc;
                XElement appDataRoot;

                // Try to load existing document to preserve unknown elements if any (though not strictly required by current spec)
                if (File.Exists(_filePath))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"MainViewModel.SaveAppSettings: Loading existing {_filePath} to preserve structure/other data.");
                        doc = XDocument.Load(_filePath);
                        appDataRoot = doc.Root;
                        if (appDataRoot == null || appDataRoot.Name != "AppData")
                        {
                            System.Diagnostics.Debug.WriteLine($"MainViewModel.SaveAppSettings: Existing root is not AppData (or null). Recreating AppData root. Old root was: {appDataRoot?.Name}");
                            XElement oldPhrases = phrasesElementToPreserve;
                            if (appDataRoot != null && appDataRoot.Name == "Phrases" && phrasesElementToPreserve == null)
                            {
                                System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: Preserving old <Phrases> root content.");
                                oldPhrases = new XElement(appDataRoot);
                            }
                            appDataRoot = new XElement("AppData");
                            doc = new XDocument(appDataRoot);
                            if (oldPhrases != null)
                            {
                                appDataRoot.Add(oldPhrases);
                            }
                        }
                    }
                    catch (Exception ex) // Catch issues like corrupted XML
                    {
                        System.Diagnostics.Debug.WriteLine($"MainViewModel.SaveAppSettings: Exception loading existing XML: {ex.Message}. Creating new document structure.");
                        appDataRoot = new XElement("AppData");
                        doc = new XDocument(appDataRoot);
                        if (phrasesElementToPreserve != null)
                        {
                            System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings (Exception Handler): Adding provided phrasesElementToPreserve to new AppData root.");
                            appDataRoot.Add(phrasesElementToPreserve);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"MainViewModel.SaveAppSettings: {_filePath} does not exist. Creating new document structure.");
                    appDataRoot = new XElement("AppData");
                    doc = new XDocument(appDataRoot);
                    if (phrasesElementToPreserve != null)
                    {
                         System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings (File Not Exist): Adding provided phrasesElementToPreserve to new AppData root.");
                         appDataRoot.Add(phrasesElementToPreserve);
                    }
                }

                XElement phrasesCurrent = appDataRoot.Element("Phrases");
                if (phrasesCurrent == null)
                {
                    System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: <Phrases> element not found. Creating one.");
                    if (phrasesElementToPreserve != null && phrasesElementToPreserve.Name == "Phrases")
                    {
                         // This case should have been handled by adding phrasesElementToPreserve to appDataRoot already.
                         // If appDataRoot.Add(phrasesElementToPreserve) was called and phrasesElementToPreserve was <Phrases>, this path shouldn't be hit often.
                         // However, if phrasesElementToPreserve was something else, or if it was null, we need a <Phrases> element.
                         if (appDataRoot.Element("Phrases") == null) // Double check
                         {
                            System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: Adding passed phrasesElementToPreserve as the <Phrases> element.");
                            appDataRoot.Add(new XElement(phrasesElementToPreserve)); // Ensure it's a new XElement if it came from another doc
                         }
                         phrasesCurrent = appDataRoot.Element("Phrases");
                    }

                    if (phrasesCurrent == null) // If still null (e.g. phrasesElementToPreserve was not <Phrases> or was null)
                    {
                        System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: Adding new empty <Phrases> element.");
                        phrasesCurrent = new XElement("Phrases");
                        appDataRoot.Add(phrasesCurrent);
                    }
                }

                XElement settingsElement = appDataRoot.Element("Settings");
                if (settingsElement == null)
                {
                    settingsElement = new XElement("Settings");
                    // appDataRoot.Add(settingsElement); // Will be added later in order
                }
                settingsElement.SetElementValue("MainWindowBackgroundColor", _mainWindowBackgroundColor);
                settingsElement.SetElementValue("MainWindowOpacity", _mainWindowOpacity.ToString(CultureInfo.InvariantCulture));
                settingsElement.SetElementValue("FontSize", _fontSize.ToString(CultureInfo.InvariantCulture));
                settingsElement.SetElementValue("SliderValue", _sliderValue.ToString(CultureInfo.InvariantCulture));
                // The debug line below was part of the duplicated block, it's removed as the logic is now consolidated.
                // System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: Ensuring Settings element exists and updating values.");
                // The duplicated block that caused CS0128 is removed.

                System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: Enforcing <Phrases> then <Settings> order.");
                phrasesCurrent.Remove(); // Remove phrases to re-add in order

                // settingsElement here is the one that was either found or created and then populated with values.
                // If it was part of appDataRoot, it needs to be removed before re-adding to ensure order.
                // If it was created new and not yet added, then it doesn't need removal.
                // The simplest way to ensure it's correctly ordered is to remove if it exists, then add.
                XElement existingSettingsInTree = appDataRoot.Element("Settings");
                if (existingSettingsInTree != null)
                {
                    existingSettingsInTree.Remove();
                }
                // Now, appDataRoot does not contain "Settings".
                // We add the settingsElement that has all the correct values.
                // If settingsElement was initially null and created, it was not added to appDataRoot yet by the first block.
                // If settingsElement was found via appDataRoot.Element("Settings"), it is the same as existingSettingsInTree.
                // The important part is that 'settingsElement' variable holds the element with all values set.

                appDataRoot.Add(phrasesCurrent); // Add Phrases first
                appDataRoot.Add(settingsElement); // Add Settings (which has all values) second

                doc.Save(_filePath);
                System.Diagnostics.Debug.WriteLine($"MainViewModel.SaveAppSettings: Settings saved to {_filePath}.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainViewModel.SaveAppSettings: Exception: {ex.Message}");
            }
            System.Diagnostics.Debug.WriteLine("MainViewModel.SaveAppSettings: Finished.");
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
