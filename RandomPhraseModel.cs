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
            if (!File.Exists(_filePath))
            {
                CreateDefaultXmlFile();  // Если файла нет, создаем его
            }

            _phrases = LoadPhrasesFromXml(_filePath);
        }
        private void CreateDefaultXmlFile()
        {
            XDocument doc = new XDocument(
                new XElement("Phrases",
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
            );
            doc.Save(_filePath);  // Сохраняем документ в файл
        }

        private List<Tuple<string, string>> LoadPhrasesFromXml(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var phrases = doc.Root.Elements("Translete")
                .Select(x => Tuple.Create(x.Element("Ru").Value, x.Element("En").Value))
                .ToList();
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
