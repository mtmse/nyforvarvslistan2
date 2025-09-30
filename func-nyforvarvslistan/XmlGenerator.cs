using func_nyforvarvslistan.Models;
using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
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

        private string GetAuthorLastName(Author fullName)
        {
            var names = fullName.Name.Split(' ');
            return names.Length > 1 ? names[names.Length - 1] : fullName.Name;
        }

        private string FormatName(string fullName)
        {
            string returnString = null;

            var fullnames = fullName.Split(';');
            if (fullnames.Length > 1)
            {
                foreach (var name in fullnames)
                {
                    var firstandlast = name.Split(',');
                    if (firstandlast.Length > 1)
                    {
                        if (returnString == null)
                        {
                            returnString = firstandlast[1].Trim() + " " + firstandlast[0].Trim();
                        }
                        else
                        {
                            returnString = returnString + " och " + firstandlast[1].Trim() + " " + firstandlast[0].Trim();
                        }
                    }
                    else
                    {
                        returnString = name;
                    }
                }
            }
            else {
                var names = fullName.Split(',');
                if (names.Length > 1)
                {
                    returnString = names[1].Trim() + " " + names[0].Trim();
                }
                else
                {
                    returnString = fullName;
                }
            }
            return returnString;
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

            if (book.Author != null)
            {
                string formattedAuthors = null;
                List<string> primaryContributors = new List<string>();
                List<string> secondaryContributors = new List<string>();

                foreach (Author author in book.Author)
                {
                    string authorName = FormatName(author.Name);

                    if (author.IsPrimaryContributor)
                    {
                        primaryContributors.Add(authorName);
                    }
                    else
                    {
                        secondaryContributors.Add(authorName);
                    }
                }

                List<string> allAuthors = new List<string>();
                allAuthors.AddRange(primaryContributors);
                allAuthors.AddRange(secondaryContributors);

                formattedAuthors = string.Join(", ", allAuthors.Take(allAuthors.Count - 1)) +
                    (allAuthors.Count > 1 ? " och " : "") +
                    allAuthors.LastOrDefault();

                authorAndPublishingDetails += $"av {formattedAuthors}.";
            }

            if (!string.IsNullOrEmpty(book.PublishingCompany))
            {
                authorAndPublishingDetails += $" {book.PublishingCompany}, {book.PublishedYear}.";
            }
            else if (book.PublishedYear !> 0)
            {
                authorAndPublishingDetails += book.PublishedYear + ". ";
            }

            if (book.Translator != null && book.Translator != "")
            {
                authorAndPublishingDetails += $" Översatt av {string.Join(" och ", FormatName(book.Translator))}.";
            }

            if (book.Narrator != null && book.Narrator != "")
            {
                string[] modifiedNarrators;

                if (book.Narrator == "Ylva" || book.Narrator == "William")
                { modifiedNarrators = "talsyntes"
                        .Split(',');
                }
                else { modifiedNarrators = book.Narrator.Split(','); }


                var narratorsString = string.Join(", ", modifiedNarrators);
                if (narratorsString.Equals("talsyntes")) {
                    authorAndPublishingDetails += $" Inläst med {narratorsString}.";
                } else
                {
                    authorAndPublishingDetails += $" Inläst av {FormatName(narratorsString)}.";
                }
                
            }

            if (!string.IsNullOrEmpty(authorAndPublishingDetails))
            {
                detailsElements.Add(new XElement(ns + "p", authorAndPublishingDetails));
            }

            if (book.PlayTime != null || book.NoPagesPS != null || book.NoPagesXML != null || book.NoVolumes != null)
            {
                string formattedExtent = "";
                if (book.LibraryId.StartsWith("P"))
                {
                    if (book.SubType != null && book.SubType != "")
                    {
                        formattedExtent = book.NoPagesPS + " blad.";
                    }
                    else {
                    string volumes = book.NoVolumes + " vol.";
                    string pages = ", " + book.NoPagesPS + " s.";

                    formattedExtent = volumes + pages;
                    }
                }
                else
                {
                    try
                    {
                        string playTime = null;

                        if (book.PlayTime != "0" && book.PlayTime != null && book.PlayTime != "")
                        {
                            string[] parts = book.PlayTime.Split(':');

                            int hours = int.Parse(parts[0]);
                            int minutes = int.Parse(parts[1]);
                            int seconds = int.Parse(parts[2]);
                            if (hours > 0 )
                            {
                                playTime = $"{hours} tim., {minutes} min. ";
                            } else
                            {
                                playTime = $"{minutes} min. ";
                            }
                        }

                        string pages = null;

                        if (book.NoPagesXML > 0)
                        {
                            pages = book.NoPagesXML + " sidor.";
                        }

                        if (playTime != null && pages != null)
                        {
                            formattedExtent = playTime + pages;
                        }
                        if (playTime != null && pages == null)
                        {
                            formattedExtent = playTime;
                        }
                        if (playTime == null && pages != null)
                        {
                            formattedExtent = pages;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Failed for string: " + book.PlayTime);
                    }
                }

                detailsElements.Add(
                    new XElement(ns + "p",
                        formattedExtent + " ",
                        new XElement(ns + "span",
                            new XAttribute("class", "medietyp"),
                            book.Format + ".")
                    )
                );
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

        private static string GetGroupKey(Book b)
        {
            if (b.SubType != "" && b.SubType != null)
                return "SubType";

            return (b.AgeGroup == "Adult") ? "Adult" : "Juvenile";
        }

        public IEnumerable<XElement> GenerateSwedishSectionXml(IEnumerable<Book> languageGroup, string toLinkOrNotToLink)
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
            var groupedByAgeGroup = languageGroup.GroupBy(b => GetGroupKey(b)).OrderBy(g =>
            {
                // Sort order:
                //   "SubType"  -> 0  (highest priority)
                //   "Adult"    -> 1
                //   "NonAdult" -> 2
                switch (g.Key)
                {
                    case "Adult":
                        return 0;
                    case "Juvenile":
                        return 1;
                    default:
                        return 2; // "NonAdult"
                }
            });

            foreach (var ageGroup in groupedByAgeGroup)
            {
                var ageGroupLevel = new XElement(ns + "level1", new XAttribute("class", "part"), new XElement(ns + "h1", $"Böcker för {TranslateToSwedish(ageGroup.Key)}"));
                XElement specialGroupLevel = null;
                XElement fackGroupLevel = null;
                if (ageGroup.Key == "SubType")
                {
                    specialGroupLevel = new XElement(ns + "level1", new XAttribute("class", "part"), new XElement(ns + "h1", "Specialproduktioner"));
                }

                if (ageGroup.Key == "Juvenile")
                {
                    fackGroupLevel = new XElement(ns + "level2", new XAttribute("class", "chapter"), new XElement(ns + "h2", "Faktaböcker"));
                } else
                {
                    fackGroupLevel = new XElement(ns + "level2", new XAttribute("class", "chapter"), new XElement(ns + "h2", "Facklitteratur"));
                }

                var groupedByCategory = ageGroup
                    .GroupBy(b => b.Category.Equals("Skönlitteratur", StringComparison.OrdinalIgnoreCase)
                                  ? "Skönlitteratur"
                                  : "Facklitteratur")
                    .OrderBy(g => CategoryOrder.IndexOf(g.Key));
                bool hasFacklitteratur = groupedByCategory.Any(g => g.Key != "Skönlitteratur");

                if (ageGroup.Key == "SubType")
                {
                    var groupedByPss = ageGroup
                        .GroupBy(b => b.SubType)
                        .OrderBy(g => CategoryOrder.IndexOf(g.Key));
                    foreach (var pssGroup in groupedByPss)
                    {
                        var pssLevel = new XElement(ns + "level2", new XElement(ns + "h2", pssGroup.Key));
                        var orderedBooks = pssGroup.OrderBy(book =>
                        {
                            var primaryAuthor = book.Author.FirstOrDefault(author => author.IsPrimaryContributor);

                            return primaryAuthor != null ? GetAuthorLastName(primaryAuthor) : "";
                        });
                        foreach (var book in orderedBooks)
                        {
                            var bookDetails = GenerateBookDetailsXml(book, 3, toLinkOrNotToLink);
                            pssLevel.Add(bookDetails);
                        }
                        specialGroupLevel.Add(pssLevel);
                    }
                }
                else {
                    
                    foreach (var categoryGroup in groupedByCategory)
                    {
                        if (categoryGroup.Key == "Skönlitteratur")
                        {

                            var categoryLevel = new XElement(ns + "level2", new XAttribute("class", "chapter"), new XElement(ns + "h2", categoryGroup.Key));
                            var orderedBooks = categoryGroup.OrderBy(book =>
                            {
                                var primaryAuthor = book.Author.FirstOrDefault(author => author.IsPrimaryContributor);

                                return primaryAuthor != null ? GetAuthorLastName(primaryAuthor) : "";
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

                        else if (categoryGroup.Key == "Facklitteratur")
                        {
                            // Create the <h3> element with the category name
                            // var h3Element = new XElement(ns + "h3", categoryGroup.Key);

                            // Order the books based on the primary author's last name
                            var orderedBooks = categoryGroup.OrderBy(book =>
                            {
                                var primaryAuthor = book.Author.FirstOrDefault(author => author.IsPrimaryContributor);

                                return primaryAuthor != null ? GetAuthorLastName(primaryAuthor) : "";
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
                }
                if (hasFacklitteratur)
                {
                    ageGroupLevel.Add(fackGroupLevel);
                }

                if (ageGroup.Key == "SubType")
                {
                    ageGroupLevel = specialGroupLevel;
                }

                yield return ageGroupLevel;
            }
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
        public byte[] GenerateXmlContent(List<Book> books, string filePath)
        {
            // Throw an error early if the input list is null.
            if (books == null)
            {
                throw new ArgumentNullException(nameof(books), $"The books list passed to GenerateXmlContent is null. FilePath: {filePath}");
            }

            // Optionally, if an empty list should be treated as an error:
            if (!books.Any())
            {
                throw new Exception($"The books list is empty in GenerateXmlContent. FilePath: {filePath}");
            }

            // Use the first book's LibraryId to get the format.
            var bookFormat = books.FirstOrDefault()?.LibraryId; // If books is empty, FirstOrDefault() returns null.
                                                                // Note: if bookFormat is null, you might want to decide what to do.
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
                new XElement(ns + "meta", new XAttribute("name", "dc:Creator"), new XAttribute("content", "MTM")),
                new XElement(ns + "meta", new XAttribute("name", "dc:Date"), new XAttribute("content", DateTime.Now.ToString("yyyy-MM-dd")))
            );
            root.Add(head);

            var bookElement = new XElement(ns + "book");
            var frontmatter = new XElement(ns + "frontmatter");

            frontmatter.Add(new XElement(ns + "doctitle", title));

            XElement introLevel1 = null;
            if (filePath.Contains("punkt"))
            {
                introLevel1 = new XElement(ns + "level1", new XAttribute("class", "part"),
                    new XElement(ns + "h1", "Inledning"),
                    new XElement(ns + "p", "Listan är uppdelad i 4 delar; Böcker för vuxna, Böcker för barn och ungdom, Specialproduktioner samt Böcker på andra språk än svenska. Dessa avsnitt ligger på rubriknivå 1."),
                    new XElement(ns + "p", "Böcker för vuxna är uppdelad i avsnitten Skönlitteratur och Facklitteratur. Böcker för barn och ungdom är uppdelad i avsnitten Skönlitteratur och Faktaböcker. Dessa avsnitt ligger på rubriknivå 2."),
                    new XElement(ns + "p", "Specialproduktioner är uppdelad i avsnitt efter olika typer av specialproduktioner. Dessa avsnitt ligger på rubriknivå 2."),
                    new XElement(ns + "p", "Böcker på andra språk än svenska är uppdelad i avsnitten Böcker för vuxna och Böcker för barn och ungdom. Dessa avsnitt ligger också på rubriknivå 2."),
                    new XElement(ns + "p", "Listan saknar indelning i olika ämneskategorier under avsnitten Facklitteratur och Faktaböcker."),
                    new XElement(ns + "p", $"Listan omfattar {books.Count()} titlar.")
                );
            }
            else
            {
                introLevel1 = new XElement(ns + "level1", new XAttribute("class", "part"),
                    new XElement(ns + "h1", "Inledning"),
                    new XElement(ns + "p", "Listan är uppdelad i 3 delar; Böcker för vuxna, Böcker för barn och ungdom och Böcker på andra språk än svenska. Dessa avsnitt ligger på rubriknivå 1."),
                    new XElement(ns + "p", "Böcker för vuxna är uppdelad i avsnitten Skönlitteratur och Facklitteratur. Böcker för barn och ungdom är uppdelad i avsnitten Skönlitteratur och Faktaböcker. Dessa avsnitt ligger på rubriknivå 2."),
                    new XElement(ns + "p", "Böcker på andra språk än svenska är uppdelad i avsnitten Böcker för vuxna och Böcker för barn och ungdom. Dessa avsnitt ligger också på rubriknivå 2."),
                    new XElement(ns + "p", "Listan saknar indelning i olika ämneskategorier under avsnitten Facklitteratur och Faktaböcker."),
                    new XElement(ns + "p", $"Listan omfattar {books.Count()} titlar.")
                );
            }
            frontmatter.Add(introLevel1);
            bookElement.Add(frontmatter);

            var bodymatter = new XElement(ns + "bodymatter");
            // Make sure that 'books' (the list) is not null before grouping.
            var groupedByLanguage = books.GroupBy(b => b.Language == "Svenska" ? "Swedish" : "Non-Swedish")
                                         .OrderBy(g => g.Key == "Swedish" ? 0 : 1);
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

            XmlWriterSettings settingsXml = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };

            using (var memoryStream = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(memoryStream, settingsXml))
                {
                    doc.Save(writer);
                }
                return memoryStream.ToArray();
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

            var section = new XElement(ns + "level1", new XAttribute("class", "part"), new XElement(ns + "h1", "Böcker på andra språk än svenska"));

            var groupedByAgeGroup = languageGroup.GroupBy(b => b.AgeGroup).OrderBy(g => g.Key == "Adult" ? 0 : 1);
            foreach (var ageGroup in groupedByAgeGroup)
            {
                var ageGroupLevel = new XElement(ns + "level2", new XAttribute("class", "chapter"), new XElement(ns + "h2", $"Böcker för {TranslateToSwedish(ageGroup.Key)}"));
                section.Add(ageGroupLevel);
                var orderedBooks = ageGroup.OrderBy(book =>
                {
                    var primaryAuthor = book.Author.FirstOrDefault(author => author.IsPrimaryContributor);

                    return primaryAuthor != null ? GetAuthorLastName(primaryAuthor) : "";
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

        public void SaveToFile(List<Book> books, string filePath)
        {
            var bookFormat = books.FirstOrDefault()?.LibraryId;
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
                new XElement(ns + "meta", new XAttribute("name", "dc:Creator"), new XAttribute("content", "MTM")),
                new XElement(ns + "meta", new XAttribute("name", "dc:Date"), new XAttribute("content", DateTime.Now.ToString("yyyy-MM-dd")))
            );
            root.Add(head);

            var bookElement = new XElement(ns + "book");
            var frontmatter = new XElement(ns + "frontmatter");

            frontmatter.Add(new XElement(ns + "doctitle", title));

            XElement introLevel1 = null;

            if (filePath.Contains("punkt"))
            {
                introLevel1 = new XElement(ns + "level1", new XAttribute("class", "part"),
                new XElement(ns + "h1", "Inledning"),
                new XElement(ns + "p", "Listan är uppdelad i 4 delar; Böcker för vuxna, Böcker för barn och ungdom, Specialproduktioner samt Böcker på andra språk än svenska. Dessa avsnitt ligger på rubriknivå 1."),
                new XElement(ns + "p", "Böcker för vuxna är uppdelad i avsnitten Skönlitteratur och Facklitteratur. Böcker för barn och ungdom är uppdelad i avsnitten Skönlitteratur och Faktaböcker. Dessa avsnitt ligger på rubriknivå 2."),
                new XElement(ns + "p", "Specialproduktioner är uppdelad i avsnitt efter olika typer av specialproduktioner. Dessa avsnitt ligger på rubriknivå 2."),
                new XElement(ns + "p", "Böcker på andra språk än svenska är uppdelad i avsnitten Böcker för vuxna och Böcker för barn och ungdom. Dessa avsnitt ligger också på rubriknivå 2."),
                new XElement(ns + "p", "Listan saknar indelning i olika ämneskategorier under avsnitten Facklitteratur och Faktaböcker."),
                new XElement(ns + "p", $"Listan omfattar {books.Count()} titlar.")
                );
            }
            else
            {
                introLevel1 = new XElement(ns + "level1", new XAttribute("class", "part"),
                    new XElement(ns + "h1", "Inledning"),
                    new XElement(ns + "p", "Listan är uppdelad i 3 delar; Böcker för vuxna, Böcker för barn och ungdom och Böcker på andra språk än svenska. Dessa avsnitt ligger på rubriknivå 1."),
                    new XElement(ns + "p", "Böcker för vuxna är uppdelad i avsnitten Skönlitteratur och Facklitteratur. Böcker för barn och ungdom är uppdelad i avsnitten Skönlitteratur och Faktaböcker. Dessa avsnitt ligger på rubriknivå 2."),
                    new XElement(ns + "p", "Böcker på andra språk än svenska är uppdelad i avsnitten Böcker för vuxna och Böcker för barn och ungdom. Dessa avsnitt ligger också på rubriknivå 2."),
                    new XElement(ns + "p", "Listan saknar indelning i olika ämneskategorier under avsnitten Facklitteratur och Faktaböcker."),
                    new XElement(ns + "p", $"Listan omfattar {books.Count()} titlar.")
                );
            }

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
    }
}
