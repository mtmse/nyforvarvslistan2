﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Reflection.Metadata.BlobBuilder;

namespace func_nyforvarvslistan
{
    public class XmlGenerator
    {
        public XNamespace ns = XNamespace.Get("http://www.daisy.org/z3986/2005/dtbook/");
        private static readonly List<string> CategoryOrder = new List<string>
            {
            "Skönlitteratur",
            "Bok- och biblioteksväsen",
            "Allmänt och blandat",
            "Religion",
            "Filosofi och psykologi",
            "Uppfostran och undervisning",
            "Språkvetenskap",
            "Litteraturvetenskap",
            "Konst, musik, teater, film, fotografi",
            "Arkeologi",
            "Historia",
            "Biografi med genealogi",
            "Etnografi, socialantropologi och etnologi",
            "Geografi och lokalhistoria",
            "Samhälls- och rättsvetenskap",
            "Teknik, industri och kommunikationer",
            "Ekonomi och näringsväsen",
            "Idrott, lek och spel",
            "Militärväsen",
            "Matematik",
            "Naturvetenskap",
            "Medicin",
            "Musikalier",
            "Musikinspelningar",
            "Tidningar",
            "Kategori saknas"
            };

        private string GetAuthorLastName(string fullName)
        {
            var names = fullName.Split(' ');
            return names.Length > 1 ? names[names.Length - 1] : fullName;
        }

        public XElement GenerateBookDetailsXml(Book book, int level, string toLinkOrNotToLink)
        {
            var detailsElements = new List<XElement>();
            if (toLinkOrNotToLink == "no-links") 
            {
                detailsElements.Add(new XElement(ns + $"h{level}", book.Title));
            }
            else
            {
                detailsElements.Add(new XElement(ns + $"h{level}",
                    new XElement(ns + "a", new XAttribute("href", $"https://www.legimus.se/bok/?librisId={book.LibrisId}"), book.Title)));
            }
            var authorAndPublishingDetails = "";

            if (book.Authors != null && book.Authors.Any(a => !string.IsNullOrEmpty(a.Name)))
            {
                authorAndPublishingDetails += $"av {string.Join(", ", book.Authors.Select(a => a.Name))}. ";
            }

            if (!string.IsNullOrEmpty(book.PublishingCompany))
            {
                authorAndPublishingDetails += $"{book.PublishingCompany}, {book.PublishedYear}. ";
            }
            else if (!string.IsNullOrEmpty(book.PublishedYear))
            {
                authorAndPublishingDetails += book.PublishedYear + ". ";
            }

            if (book.Translator != null && book.Translator.Any(a => !string.IsNullOrEmpty(a.Name)))
            {
                authorAndPublishingDetails += $"Översatt av {string.Join(", ", book.Translator.Select(a => a.Name))}. ";
            }

            if (book.Narrator != null && book.Narrator.Any(a => !string.IsNullOrEmpty(a.Name)))
            {
                authorAndPublishingDetails += $"Inläst av {string.Join(", ", book.Narrator.Select(a => a.Name))}.";
            }

            if (book.Extent != null && book.Extent.Any())
            {
                string extentString = string.Join(". ", book.Extent.Where(s => !string.IsNullOrEmpty(s)));
                if (!string.IsNullOrEmpty(extentString))
                {
                    detailsElements.Add(new XElement(ns + "p", extentString));
                }
            }

            if (!string.IsNullOrEmpty(authorAndPublishingDetails))
            {
                detailsElements.Add(new XElement(ns + "p", authorAndPublishingDetails));
            }

            if (!string.IsNullOrEmpty(book.Description))
            {
                detailsElements.Add(new XElement(ns + "p", book.Description));
            }

            if (toLinkOrNotToLink == "no-links")
            {
                detailsElements.Add(new XElement(ns + "p", new XElement(ns + "p", $"MediaNr: {book.LibraryId}")));
            }
            else
            {
                detailsElements.Add(new XElement(ns + "p", $"MediaNr: ", new XElement(ns + "a", new XAttribute("href", $"https://www.legimus.se/bok/?librisId={book.LibrisId}"), book.LibraryId)));
            }

            return new XElement(ns + $"level{level}", detailsElements);
        }

        public XElement GenerateSwedishSectionXml(IEnumerable<Book> languageGroup, string toLinkOrNotToLink)
        {
            var section = new XElement(ns + "level1");
            var groupedByAgeGroup = languageGroup.GroupBy(b => b.AgeGroup).OrderBy(g => g.Key == "Adult" ? 0 : 1);
            foreach (var ageGroup in groupedByAgeGroup)
            {
                var ageGroupLevel = new XElement(ns + "level1", new XElement(ns + "h1", $"Litteratur för {TranslateToSwedish(ageGroup.Key)}"));
                var fackGroupLevel = new XElement(ns + "level2", new XElement(ns + "h2", "Faktaböcker"));
                section.Add(ageGroupLevel);

                var groupedByCategory = ageGroup.GroupBy(b => b.Category).OrderBy(g => CategoryOrder.IndexOf(g.Key));

                foreach (var categoryGroup in groupedByCategory)
                {
                    if (categoryGroup.Key == "Skönlitteratur")
                    {
                        var categoryLevel = new XElement(ns + "level2", new XElement(ns + "h2", categoryGroup.Key));
                        var orderedBooks = categoryGroup.OrderBy(book =>
                        {
                            var primaryAuthor = book.Authors.FirstOrDefault(a => a.IsPrimaryContributor);

                            return primaryAuthor != null ? GetAuthorLastName(primaryAuthor.Name) : "";
                        });
                        foreach (var book in orderedBooks)
                        {
                            var bookDetails = GenerateBookDetailsXml(book, 3, toLinkOrNotToLink);
                            var level = new XElement(ns + "level3");
                            level.Add(bookDetails);
                            categoryLevel.Add(level);
                        }
                        ageGroupLevel.Add(categoryLevel);
                    }
                    else
                    {

                        var categoryLevel = new XElement(ns + "level3", new XElement(ns + "h3", categoryGroup.Key));
                        var orderedBooks = categoryGroup.OrderBy(book =>
                        {
                            var primaryAuthor = book.Authors.FirstOrDefault(a => a.IsPrimaryContributor);

                            return primaryAuthor != null ? GetAuthorLastName(primaryAuthor.Name) : "";
                        });
                        foreach (var book in orderedBooks)
                        {
                            var bookDetails = GenerateBookDetailsXml(book, 4, toLinkOrNotToLink);
                            var level = new XElement(ns + "level4");
                            level.Add(bookDetails);
                            categoryLevel.Add(level);
                        }
                        fackGroupLevel.Add(categoryLevel);
                    }
                }
                ageGroupLevel.Add(fackGroupLevel);
            }

            return section;
        }

        private string TranslateToSwedish(string ageGroupKey)
        {
            switch (ageGroupKey)
            {
                case "Adult":
                    return "vuxna";
                case "Juvenile":
                    return "barn";
                default:
                    return ageGroupKey.ToLower();
            }
        }
        public void SaveToFile(IEnumerable<Book> books, string filePath)
        {
            var bookFormat = books.FirstOrDefault()?.Format;
            string title = Dates.GetFormattedBookTitle(bookFormat, Dates.StartOfPreviousMonth);
            XNamespace ns = "http://www.daisy.org/z3986/2005/dtbook/";

            var root = new XElement(ns + "dtbook",
                    new XAttribute("version", "2005-2"),
                    new XAttribute(XNamespace.Xml + "lang", "sv"));

            var head = new XElement(ns + "head",
                new XElement(ns + "meta", new XAttribute("name", "dtb:uid"), new XAttribute("content", "dummy-id-5046559668269995")),
                new XElement(ns + "meta", new XAttribute("name", "dc:Title"), new XAttribute("content", title)),
                new XElement(ns + "meta", new XAttribute("name", "dc:Language"), new XAttribute("content", "sv")),
                new XElement(ns + "meta", new XAttribute("name", "dc:Publisher"), new XAttribute("content", "MTM")),
                new XElement(ns + "meta", new XAttribute("name", "dc:Date"), new XAttribute("content", DateTime.Now.ToString("yyyy-MM-dd")))
            );
            root.Add(head);

            var bookElement = new XElement(ns + "book");
            var frontmatter = new XElement(ns + "frontmatter");

            frontmatter.Add(new XElement(ns + "doctitle", title));
            var introLevel1 = new XElement(ns + "level1",
                new XElement(ns + "h1", "Inledning"),
                new XElement(ns + "p", "Listan är uppdelad i 3 delar; Litteratur för vuxna, Litteratur för Barn och Böcker på andra språk än svenska, vilka ligger på nivå 1. Litteratur för vuxna och Litteratur för barn är uppdelade mellan Skönlitteratur, Facklitteratur. Dessa avsnitt ligger på nivå 2. Böcker på andra språk än svenska är uppdelade mellan Böcker för vuxna och Böcker för barn. Boktitlarna ligger på nivå 3 i avsnitten Skönlitteratur och Böcker på andra språk än svenska, medan de ligger på nivå 4 i avsnittet Facklitteratur. På Nivå 3 i avsnittet Facklitteratur finns de olika fackavdelningarna."),
                new XElement(ns + "p", $"Listan omfattar {books.Count()} titlar.")
            );
            frontmatter.Add(introLevel1);
            bookElement.Add(frontmatter);

            var bodymatter = new XElement(ns + "bodymatter");
            var groupedByLanguage = books.GroupBy(b => b.Language == "Svenska" ? "Swedish" : "Non-Swedish").OrderBy(g => g.Key == "Swedish" ? 0 : 1);
            var toLinkOrNotToLink = "";
            if (filePath.Contains("no-links"))
            {
                toLinkOrNotToLink = "no-links";
            }
            foreach (var languageGroup in groupedByLanguage)
            {
                if (languageGroup.Key == "Swedish")
                {
                    bodymatter.Add(GenerateSwedishSectionXml(languageGroup, toLinkOrNotToLink));
                }
                else if (!filePath.Contains("swedishonly"))
                {
                    bodymatter.Add(GenerateNonSwedishSectionXml(languageGroup, toLinkOrNotToLink));
                }
            }
            bookElement.Add(bodymatter);
            root.Add(bookElement);

            var docType = new XDocumentType("dtbook", "-//NISO//DTD dtbook 2005-2//EN", "http://www.daisy.org/z3986/2005/dtbook-2005-2.dtd", null);
            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                docType,
                root);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                doc.Save(writer);
            }
        }
        public XElement GenerateNonSwedishSectionXml(IEnumerable<Book> languageGroup, string toLinkOrNotToLink)
        {
            var section = new XElement(ns + "level1", new XElement(ns + "h1", "Litteratur på andra språk än svenska"));

            var groupedByAgeGroup = languageGroup.GroupBy(b => b.AgeGroup).OrderBy(g => g.Key == "Adult" ? 0 : 1);
            foreach (var ageGroup in groupedByAgeGroup)
            {
                var ageGroupLevel = new XElement(ns + "level2", new XElement(ns + "h2", $"Litteratur för {TranslateToSwedish(ageGroup.Key)}"));
                section.Add(ageGroupLevel);
                var orderedBooks = ageGroup.OrderBy(book =>
                {
                    var primaryAuthor = book.Authors.FirstOrDefault(a => a.IsPrimaryContributor);

                    return primaryAuthor != null ? GetAuthorLastName(primaryAuthor.Name) : "";
                });

                foreach (var book in orderedBooks)
                {
                    var bookDetails = GenerateBookDetailsXml(book, 3, toLinkOrNotToLink);
                    var level = new XElement(ns + "level3");
                    level.Add(bookDetails);
                    ageGroupLevel.Add(level);
                }
            }

            return section;
        }

    }
}
