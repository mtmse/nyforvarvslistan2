using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

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
            if (book.LibraryId == "CA69080")
            {
                Console.WriteLine("Den hittar hit i alla fall!");
            }
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
                string formattedAuthors;

                if (book.Authors.Count == 1)
                {
                    formattedAuthors = book.Authors.First().Name;
                }
                else if (book.Authors.Count == 2)
                {
                    // Two authors: "Author One och Author Two"
                    formattedAuthors = $"{book.Authors[0].Name} och {book.Authors[1].Name}";
                }
                else
                {
                    // More than two authors: "Author One, Author Two och Author Three"
                    formattedAuthors = $"{string.Join(", ", book.Authors.Take(book.Authors.Count - 1).Select(a => a.Name))} och {book.Authors.Last().Name}";
                }
                authorAndPublishingDetails += $"av {formattedAuthors}.";
            }

            if (!string.IsNullOrEmpty(book.PublishingCompany))
            {
                authorAndPublishingDetails += $" {book.PublishingCompany}, {book.PublishedYear}.";
            }
            else if (!string.IsNullOrEmpty(book.PublishedYear))
            {
                authorAndPublishingDetails += book.PublishedYear + ". ";
            }

            if (book.Translator != null && book.Translator.Any(a => !string.IsNullOrEmpty(a.Name)))
            {
                authorAndPublishingDetails += $" Översatt av {string.Join(" och ", book.Translator.Select(a => a.Name))}.";
            }

            if (book.Narrator != null && book.Narrator.Any(a => !string.IsNullOrEmpty(a.Name)))
            {
                var modifiedNarrators = book.Narrator.Select(a => {
                    if (a.Name == "Ylva" || a.Name == "William")
                    {
                        return "talsyntes";
                    }
                    else
                    {
                        return a.Name;
                    }
                }).ToList();


                var narratorsString = string.Join(", ", modifiedNarrators);
                if (narratorsString.Equals("talsyntes")) {
                    authorAndPublishingDetails += $" Inläst med {narratorsString}.";
                } else
                {
                    authorAndPublishingDetails += $" Inläst av {narratorsString}.";
                }
                
            }

            if (!string.IsNullOrEmpty(authorAndPublishingDetails))
            {
                detailsElements.Add(new XElement(ns + "p", authorAndPublishingDetails));
            }

            if (book.Extent != null && book.Extent.Any())
            {
                string extentString = string.Join(". ", book.Extent.Where(s => !string.IsNullOrEmpty(s)));
                string formattedExtent = "";
                if (book.LibraryId.StartsWith("P"))
                {
                    // Braille books: "4 volymer (290 sidor)" -> "4 vol., 290 s"
                    var volumeMatch = Regex.Match(extentString, @"(\d+)\s+volymer");
                    var pagesMatch = Regex.Match(extentString, @"(\d+)\s+sidor");

                    // Default to "1 vol." if no volume match is found
                    string volumes = volumeMatch.Success ? volumeMatch.Groups[1].Value + " vol." : "1 vol.";
                    string pages = pagesMatch.Success ? ", " + pagesMatch.Groups[1].Value + " s" : "";

                    formattedExtent = volumes + pages;
                    formattedExtent = formattedExtent.TrimEnd(')', '(');
                }
                else
                {
                    // Try to extract time duration (e.g., "6 tim." or "6 tim., 14 min.")
                    var timeMatch = Regex.Match(extentString, @"(\d+ tim\.)?(?:,? (\d+ min\.)?)?");

                    if (timeMatch.Success)
                    {
                        // Capture time duration
                        var hours = timeMatch.Groups[1].Value; // "6 tim."
                        var minutes = timeMatch.Groups[2].Value; // "14 min." (if present)

                        // Combine hours and minutes into formatted extent
                        formattedExtent = hours;
                        if (!string.IsNullOrEmpty(minutes))
                        {
                            formattedExtent += ", " + minutes;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(formattedExtent) && !book.LibraryId.StartsWith("P"))
                {
                    detailsElements.Add(
                        new XElement(ns + "p",
                            formattedExtent + " ",
                            new XElement(ns + "span",
                                new XAttribute("class", "medietyp"),
                                "Talbok.")
                        )
                    );
                }
                else if (!string.IsNullOrEmpty(formattedExtent)) {
                    detailsElements.Add(
                        new XElement(ns + "p",
                            formattedExtent + " ",
                            new XElement(ns + "span",
                                new XAttribute("class", "medietyp"),
                                "tryckt punktskrift.")
                        )
                    );
                }
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
            if (book.LibraryId == "CA68137")
            {
                foreach (XElement element in detailsElements)
                {
                    Console.WriteLine(element.ToString());
                }
            }
            if (book.LibraryId == "CA68080")
            {
                foreach (XElement element in detailsElements)
                {
                    Console.WriteLine(element.ToString());
                }
            }
            return new XElement(ns + $"level{level}", detailsElements);
        }

        public XElement GenerateSwedishSectionXml(IEnumerable<Book> languageGroup, string toLinkOrNotToLink)
        {
            /*
            foreach (var book in languageGroup)
            {
                if (book.PublicationCategory.FirstOrDefault() == "Fiction")
                {
                    book.Category = "Skönlitteratur";
                }
            }
            */

            XElement section = null;
            var groupedByAgeGroup = languageGroup.GroupBy(b => b.AgeGroup).OrderBy(g => g.Key == "Adult" ? 0 : 1);
            foreach (var ageGroup in groupedByAgeGroup)
            {
                var ageGroupLevel = new XElement(ns + "level1", new XElement(ns + "h1", $"Böcker för {TranslateToSwedish(ageGroup.Key)}"));
                var fackGroupLevel = new XElement(ns + "level2", new XElement(ns + "h2", "Facklitteratur"));
                if (section == null)
                {
                    section = ageGroupLevel;
                }
                else
                {
                    section.Add(ageGroupLevel);
                }

                var groupedByCategory = ageGroup
                    .GroupBy(b => b.Category.Equals("Skönlitteratur", StringComparison.OrdinalIgnoreCase)
                                  ? "Skönlitteratur"
                                  : "Facklitteratur")
                    .OrderBy(g => CategoryOrder.IndexOf(g.Key));

                bool hasFacklitteratur = groupedByCategory.Any(g => g.Key != "Skönlitteratur");
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
                            // var level = new XElement(ns + "level3");
                            // level.Add(bookDetails);
                            categoryLevel.Add(bookDetails);
                        }
                        ageGroupLevel.Add(categoryLevel);
                    }
                    /*else if (categoryGroup.Key != "Skönlitteratur")
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
                            //var level = new XElement(ns + "level4");
                            //level.Add(bookDetails);
                            categoryLevel.Add(bookDetails);
                        }
                        fackGroupLevel.Add(categoryLevel);
                    }*/
                    else if (categoryGroup.Key != "Skönlitteratur")
                    {
                        // Create the <h3> element with the category name
                        // var h3Element = new XElement(ns + "h3", categoryGroup.Key);

                        // Order the books based on the primary author's last name
                        var orderedBooks = categoryGroup.OrderBy(book =>
                        {
                            var primaryAuthor = book.Authors.FirstOrDefault(a => a.IsPrimaryContributor);
                            return primaryAuthor != null ? GetAuthorLastName(primaryAuthor.Name) : "";
                        });

                        // Add the <h3> element directly to fackGroupLevel
                        // fackGroupLevel.Add(h3Element);

                        // Iterate through the ordered books and add their details directly to fackGroupLevel
                        foreach (var book in orderedBooks)
                        {
                            var bookDetails = GenerateBookDetailsXml(book, 3, toLinkOrNotToLink);
                            fackGroupLevel.Add(bookDetails);
                        }
                    }
                }
                if (hasFacklitteratur)
                {
                    ageGroupLevel.Add(fackGroupLevel);
                }
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
                    return "barn och ungdom";
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
                new XElement(ns + "p", "Listan är uppdelad i 3 delar; Böcker för vuxna, Böcker för barn och ungdom och Böcker på andra språk än svenska. Dessa ligger på rubriknivå 1."),
                new XElement(ns + "p", "Böcker för vuxna är uppdelad i avsnitten Skönlitteratur och Facklitteratur. Böcker för barn och ungdom är uppdelad i avsnitten Skönlitteratur och Faktaböcker. Dessa avsnitt ligger på rubriknivå 2."),
                new XElement(ns + "p", "Böcker på andra språk än svenska är uppdelade mellan Böcker för vuxna och Böcker för barn och ungdom. Dessa avsnitt ligger också på rubriknivå 2."),
                new XElement(ns + "p", "Listan saknar indelning i olika ämneskategorier under avsnitten Facklitteratur och Faktaböcker."),
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
            /*
             * foreach (var book in languageGroup)
            {
                if (book.PublicationCategory.FirstOrDefault() == "Fiction")
                {
                    book.Category = "Skönlitteratur";
                }
            }
            */

            var section = new XElement(ns + "level1", new XElement(ns + "h1", "Böcker på andra språk än svenska"));

            var groupedByAgeGroup = languageGroup.GroupBy(b => b.AgeGroup).OrderBy(g => g.Key == "Adult" ? 0 : 1);
            foreach (var ageGroup in groupedByAgeGroup)
            {
                var ageGroupLevel = new XElement(ns + "level2", new XElement(ns + "h2", $"Böcker för {TranslateToSwedish(ageGroup.Key)}"));
                section.Add(ageGroupLevel);
                var orderedBooks = ageGroup.OrderBy(book =>
                {
                    var primaryAuthor = book.Authors.FirstOrDefault(a => a.IsPrimaryContributor);

                    return primaryAuthor != null ? GetAuthorLastName(primaryAuthor.Name) : "";
                });

                foreach (var book in orderedBooks)
                {
                    var bookDetails = GenerateBookDetailsXml(book, 3, toLinkOrNotToLink);
                    //var level = new XElement(ns + "level3");
                    //level.Add(bookDetails);
                    ageGroupLevel.Add(bookDetails);
                }
            }

            return section;
        }

    }
}
