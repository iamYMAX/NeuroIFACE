using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NeuroIFACE
{
    public class RandomPhraseModel
    {
        private List<Tuple<string, string>> _phrases;
        private readonly string _filePath = "Data.xml";

        public RandomPhraseModel()
        {
            System.Diagnostics.Debug.WriteLine("RandomPhraseModel: Constructor called.");
            if (!File.Exists(_filePath))
            {
                System.Diagnostics.Debug.WriteLine($"RandomPhraseModel: {_filePath} not found. Calling CreateDefaultXmlFile().");
                CreateDefaultXmlFile();  // Если файла нет, создаем его
            }

            _phrases = LoadPhrasesFromXml(_filePath);
            System.Diagnostics.Debug.WriteLine("RandomPhraseModel: Constructor finished.");
        }
        private void CreateDefaultXmlFile()
        {
            System.Diagnostics.Debug.WriteLine("RandomPhraseModel.CreateDefaultXmlFile: Creating default Data.xml...");
            XDocument doc = new XDocument(
                new XElement("AppData", // Root element is now AppData
                    new XElement("Phrases", // Phrases element nested under AppData
                        new XElement("Translete",
                            new XElement("En", "Love"),
                            new XElement("Ru", "Любовь")),
                        new XElement("Translete",
                            new XElement("En", "Live"),
                            new XElement("Ru", "Жить")),
                        new XElement("Translete",
                            new XElement("En", "Evil"),
                            new XElement("Ru", "Зло"))
                    )
                )
            );
            doc.Save(_filePath);  // Сохраняем документ в файл
            System.Diagnostics.Debug.WriteLine("RandomPhraseModel.CreateDefaultXmlFile: Default Data.xml saved.");
        }

        private List<Tuple<string, string>> LoadPhrasesFromXml(string filePath)
        {
            System.Diagnostics.Debug.WriteLine($"RandomPhraseModel.LoadPhrasesFromXml: Loading phrases from {filePath}...");
            List<Tuple<string, string>> phrases;
            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement phrasesElement = doc.Root;

                // Check if the root is AppData and then find Phrases
                if (doc.Root.Name == "AppData")
                {
                    phrasesElement = doc.Root.Element("Phrases");
                    System.Diagnostics.Debug.WriteLine("RandomPhraseModel.LoadPhrasesFromXml: AppData root found, looking for Phrases element.");
                }
                else if (doc.Root.Name == "Phrases")
                {
                    // This is the old format, log a warning.
                    System.Diagnostics.Debug.WriteLine("RandomPhraseModel.LoadPhrasesFromXml: Warning - Data.xml is in the old format (Phrases is root).");
                    // Future enhancement: migrate this to the new structure.
                }
                else
                {
                    // Unknown XML structure
                    System.Diagnostics.Debug.WriteLine("RandomPhraseModel.LoadPhrasesFromXml: Error - Data.xml has an unrecognized structure.");
                    return new List<Tuple<string, string>>(); // Return empty list
                }

                if (phrasesElement == null)
                {
                    System.Diagnostics.Debug.WriteLine("RandomPhraseModel.LoadPhrasesFromXml: Error - <Phrases> element not found in Data.xml.");
                    return new List<Tuple<string, string>>(); // Return empty list
                }

                phrases = phrasesElement.Elements("Translete")
                    .Select(x => Tuple.Create(x.Element("Ru")?.Value, x.Element("En")?.Value))
                    .Where(p => p.Item1 != null && p.Item2 != null) // Ensure both parts of phrase exist
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"RandomPhraseModel.LoadPhrasesFromXml: Loaded {phrases.Count} phrases.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RandomPhraseModel.LoadPhrasesFromXml: Exception: {ex.Message}");
                // Depending on requirements, might re-throw, or return empty/default list
                return new List<Tuple<string, string>>();
            }
            System.Diagnostics.Debug.WriteLine("RandomPhraseModel.LoadPhrasesFromXml: Finished.");
            return phrases;
        }

        public string GetRandomPhrase()
        {
            var random = new Random();
            var randomPhrase = _phrases[random.Next(_phrases.Count)];
            return $"{randomPhrase.Item1} - {randomPhrase.Item2}";
        }
    }
}
