using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input; // Required for MouseButtonEventArgs
using System.Xml.Linq;
using Microsoft.Win32; // Required for OpenFileDialog and SaveFileDialog
using OfficeOpenXml; // Required for EPPlus

namespace NeuroIFACE
{
    public class PhraseEntry
    {
        public string Russian { get; set; }
        public string English { get; set; }
    }

    public partial class PhraseManagerWindow : Window
    {
        private ObservableCollection<PhraseEntry> _phrases;
        private readonly string _filePath = "Data.xml";

        static PhraseManagerWindow()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow: EPPlus LicenseContext set to NonCommercial.");
        }

        public PhraseManagerWindow()
        {
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow: Constructor called.");
            InitializeComponent();
            _phrases = new ObservableCollection<PhraseEntry>();
            PhrasesListView.ItemsSource = _phrases;
            LoadPhrases();
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow: Constructor finished.");
        }

        private void LoadPhrases()
        {
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.LoadPhrases: Attempting to load phrases...");
            try
            {
                if (!File.Exists(_filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.LoadPhrases: File not found: {_filePath}. No phrases loaded.");
                    return;
                }

                XDocument doc = XDocument.Load(_filePath);
                XElement appDataElement = doc.Root;

                if (appDataElement == null || appDataElement.Name != "AppData")
                {
                    MessageBox.Show("Data.xml is not in the expected AppData format. Cannot load phrases.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                XElement phrasesElement = appDataElement.Element("Phrases");
                if (phrasesElement != null)
                {
                    var loadedPhrases = phrasesElement.Elements("Translete")
                        .Select(x => new PhraseEntry
                        {
                            Russian = x.Element("Ru")?.Value,
                            English = x.Element("En")?.Value
                        })
                        .Where(p => p.Russian != null && p.English != null);

                    _phrases.Clear();
                    foreach (var phrase in loadedPhrases)
                    {
                        _phrases.Add(phrase);
                    }
                    System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.LoadPhrases: Loaded {_phrases.Count} phrases.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.LoadPhrases: <Phrases> element not found. No phrases loaded.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.LoadPhrases: Exception: {ex.Message}");
                MessageBox.Show($"Error loading phrases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.LoadPhrases: Finished.");
        }

        private void SaveChangesToXml()
        {
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.SaveChangesToXml: Attempting to save changes...");
            try
            {
                XDocument doc;
                XElement appDataElement;
                XElement phrasesElement;

                if (File.Exists(_filePath))
                {
                    doc = XDocument.Load(_filePath);
                    appDataElement = doc.Root;
                    if (appDataElement == null || appDataElement.Name != "AppData")
                    {
                        appDataElement = new XElement("AppData");
                        doc = new XDocument(appDataElement);
                    }
                }
                else
                {
                    appDataElement = new XElement("AppData");
                    doc = new XDocument(appDataElement);
                }

                phrasesElement = appDataElement.Element("Phrases");
                if (phrasesElement == null)
                {
                    phrasesElement = new XElement("Phrases");
                    if (appDataElement.Element("Settings") != null) {
                         appDataElement.Element("Settings").AddBeforeSelf(phrasesElement);
                    } else {
                        appDataElement.Add(phrasesElement);
                    }
                }

                phrasesElement.RemoveAll();

                foreach (var phraseEntry in _phrases)
                {
                    phrasesElement.Add(new XElement("Translete",
                        new XElement("Ru", phraseEntry.Russian),
                        new XElement("En", phraseEntry.English)));
                }

                XElement settingsElement = appDataElement.Element("Settings");
                if (settingsElement != null)
                {
                    settingsElement.Remove();
                    appDataElement.Add(settingsElement);
                } else {
                    appDataElement.Add(new XElement("Settings"));
                }
                doc.Save(_filePath);
                System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.SaveChangesToXml: Saved {_phrases.Count} phrases to {_filePath}.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.SaveChangesToXml: Exception: {ex.Message}");
                MessageBox.Show($"Error saving phrases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.SaveChangesToXml: Finished.");
        }


        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.DeleteButton_Click: Attempting to delete {PhrasesListView.SelectedItems.Count} items.");
            var selectedItems = PhrasesListView.SelectedItems.Cast<PhraseEntry>().ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select phrase(s) to delete.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.DeleteButton_Click: No items selected.");
                return;
            }

            foreach (var item in selectedItems)
            {
                _phrases.Remove(item);
            }
            System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.DeleteButton_Click: Removed {selectedItems.Count} items from collection.");
            SaveChangesToXml();
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.DeleteButton_Click: Finished.");
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ClearAllButton_Click: Clicked.");
            if (MessageBox.Show("Are you sure you want to delete all phrases?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ClearAllButton_Click: User confirmed deletion.");
                _phrases.Clear();
                SaveChangesToXml();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ClearAllButton_Click: User cancelled deletion.");
            }
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ClearAllButton_Click: Finished.");
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ImportButton_Click: Clicked.");
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Select an Excel file to import phrases"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.FileName;
                System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.ImportButton_Click: Selected file: {fileName}");
                try
                {
                    FileInfo file = new FileInfo(fileName);
                    using (ExcelPackage package = new ExcelPackage(file))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ImportButton_Click: No worksheet found.");
                            MessageBox.Show("No worksheet found in the Excel file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        int startRow = worksheet.Dimension?.Start.Row ?? 1;
                        int endRow = worksheet.Dimension?.End.Row ?? 0;
                        int importedCount = 0;

                        // Skip header row if present (optional, assuming headers are in row 1)
                        // You might want to make this configurable or detect headers.
                        // For this implementation, let's assume row 1 could be headers or data.
                        // If headers are "Russian" and "English", we can skip them.
                        bool firstRowIsHeader = false;
                        if (endRow >= 1) {
                            string cellA1 = worksheet.Cells[startRow, 1].Value?.ToString()?.Trim();
                            string cellB1 = worksheet.Cells[startRow, 2].Value?.ToString()?.Trim();
                            if (string.Equals(cellA1, "Russian", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(cellB1, "English", StringComparison.OrdinalIgnoreCase))
                            {
                                startRow++; // Skip header row
                                firstRowIsHeader = true;
                            }
                        }


                        for (int row = startRow; row <= endRow; row++)
                        {
                            string russian = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            string english = worksheet.Cells[row, 2].Value?.ToString()?.Trim();

                            if (!string.IsNullOrEmpty(russian) && !string.IsNullOrEmpty(english))
                            {
                                _phrases.Add(new PhraseEntry { Russian = russian, English = english });
                                importedCount++;
                            }
                        }

                        if (importedCount > 0)
                        {
                            SaveChangesToXml();
                            System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.ImportButton_Click: Imported {importedCount} phrases.");
                            MessageBox.Show($"{importedCount} phrases imported successfully.", "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (firstRowIsHeader && endRow < startRow) // Only header row existed
                        {
                            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ImportButton_Click: No data rows found below header.");
                             MessageBox.Show("No data rows found below the header.", "Import Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ImportButton_Click: No valid phrases found or file empty.");
                            MessageBox.Show("No valid phrases found in the file or file is empty.", "Import Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.ImportButton_Click: Exception: {ex.Message}");
                    MessageBox.Show($"Error importing phrases: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ImportButton_Click: File selection cancelled.");
            }
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ImportButton_Click: Finished.");
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ExportButton_Click: Clicked.");
            if (_phrases.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ExportButton_Click: No phrases to export.");
                MessageBox.Show("There are no phrases to export.", "Export Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Save phrases to an Excel file",
                FileName = "PhrasesExport.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string fileName = saveFileDialog.FileName;
                System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.ExportButton_Click: Exporting to file: {fileName}");
                try
                {
                    FileInfo file = new FileInfo(fileName);
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Phrases");

                        // Add headers
                        worksheet.Cells[1, 1].Value = "Russian";
                        worksheet.Cells[1, 2].Value = "English";

                        // Optional: Style headers
                        using (var range = worksheet.Cells[1,1,1,2])
                        {
                            range.Style.Font.Bold = true;
                        }

                        for (int i = 0; i < _phrases.Count; i++)
                        {
                            worksheet.Cells[i + 2, 1].Value = _phrases[i].Russian;
                            worksheet.Cells[i + 2, 2].Value = _phrases[i].English;
                        }

                        // Optional: Auto-fit columns
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        package.SaveAs(file);
                        System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.ExportButton_Click: Exported {_phrases.Count} phrases.");
                        MessageBox.Show("Phrases exported successfully.", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PhraseManagerWindow.ExportButton_Click: Exception: {ex.Message}");
                    MessageBox.Show($"Error exporting phrases: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ExportButton_Click: File save cancelled.");
            }
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.ExportButton_Click: Finished.");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PhraseManagerWindow.CloseButton_Click: Closing window.");
            this.Close();
        }
    }
}
