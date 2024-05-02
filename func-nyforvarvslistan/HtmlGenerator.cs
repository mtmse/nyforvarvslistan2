using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace func_nyforvarvslistan
{
    public class HtmlGenerator
    {
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
        public const string EPUB_NAMESPACE = "http://www.idpf.org/2007/ops";

        public string GenerateHeader(string title)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""sv"">
<head><meta charset=""UTF-8"" /><title>{title}</title>
<link rel=""stylesheet"" type=""text/css"" href=""https://old.legimus.se/Customer/Files/Acquisitions/default.css"" /></head>
<body><section xmlns:epub=""{EPUB_NAMESPACE}"" epub:type=""frontmatter"">
<section id=""id_1""><h1 epub:type=""title"">{title}</h1></section>";
        }

            public string GenerateBookDetails(Book book)
        {
            var details = new StringBuilder();
            details.AppendLine(EscapeXml($"<h2><a href=\"https://www.legimus.se/bok/?librisId={book.LibrisId}\">{book.Title}</a></h2>"));

            if (book.Authors != null && book.Authors.Any(a => !string.IsNullOrEmpty(a.Name)))
            {
                details.Append(EscapeXml($"<strong>av</strong> {string.Join(", ", book.Authors.Select(a => a.Name))}. "));
            }

            if (!string.IsNullOrEmpty(book.PublishingCompany))
            {
                details.Append(EscapeXml($"{book.PublishingCompany}, {book.PublishedYear}"));
            }
            else if (!string.IsNullOrEmpty(book.PublishedYear))
            {
                details.Append(book.PublishedYear);
            }
            if (!string.IsNullOrEmpty(book.Description))
            {
                details.AppendLine(EscapeXml($"<p><strong>Beskrivning:</strong> {book.Description}</p>"));
            }
            details.AppendLine($"<p><strong>Medianummer:</strong> {book.LibraryId}</p>");

            return details.ToString();
        }

        public string GenerateSwedishSection(IEnumerable<Book> languageGroup)
        {
            var sectionBuilder = new StringBuilder();
            var groupedByAgeGroup = languageGroup.GroupBy(b => b.AgeGroup).OrderBy(g => g.Key == "Adult" ? 0 : 1);

            foreach (var ageGroup in groupedByAgeGroup)
            {
                sectionBuilder.Append($"<section xmlns:epub=\"{EPUB_NAMESPACE}\" epub:type=\"bodymatter\"><section id=\"id_3\"><h1>Böcker för {TranslateToSwedish(ageGroup.Key)}</h1><section id=\"id_4\">");

                var groupedByCategory = ageGroup.GroupBy(b => b.Category).OrderBy(g => CategoryOrder.IndexOf(g.Key));
                foreach (var categoryGroup in groupedByCategory)
                {
                    sectionBuilder.Append($"<section id=\"id_5\"><h1>{categoryGroup.Key}</h1>");
                    foreach (var book in categoryGroup)
                    {
                        sectionBuilder.Append(GenerateBookDetails(book));
                    }
                    sectionBuilder.Append("</section>");
                }
                sectionBuilder.Append("</section></section></section>");
            }
            return sectionBuilder.ToString();
        }

        public string GenerateNonSwedishSection(IEnumerable<Book> languageGroup)
        {
            var sectionBuilder = new StringBuilder();
            var groupedByAgeGroup = languageGroup.GroupBy(b => b.AgeGroup).OrderBy(g => g.Key == "Adult" ? 0 : 1);
            sectionBuilder.Append($"<section xmlns:epub=\"{EPUB_NAMESPACE}\" epub:type=\"bodymatter\"><section id=\"id_3\"><h1>Böcker på andra språk än svenska</h1>");

            foreach (var ageGroup in groupedByAgeGroup)
            {
                sectionBuilder.Append($"<section xmlns:epub=\"{EPUB_NAMESPACE}\" epub:type=\"bodymatter\"><section id=\"id_3\"><h1>Böcker för {TranslateToSwedish(ageGroup.Key)}</h1></section>");
                foreach (var book in ageGroup)
                {
                    sectionBuilder.Append(GenerateBookDetails(book));
                }
                sectionBuilder.Append("</section></section>");
            }
            return sectionBuilder.ToString();
        }

        public string GenerateHtml(IEnumerable<Book> books)
        {
            var groupedByLanguage = books.GroupBy(b => b.Language == "Svenska" ? "Swedish" : "Non-Swedish").OrderBy(g => g.Key == "Swedish" ? 0 : 1);
            var bookFormat = books.FirstOrDefault()?.Format;
            string title = Dates.GetFormattedBookTitle(bookFormat, Dates.StartOfPreviousMonth);

            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append(GenerateHeader(title));
            htmlBuilder.AppendLine($"<p>Listan omfattar {books.Count()} titlar.</p></section>");



                foreach (var languageGroup in groupedByLanguage)
            {
                if (languageGroup.Key == "Swedish")
                {
                    htmlBuilder.Append(GenerateSwedishSection(languageGroup));
                }
                else
                {
                    htmlBuilder.Append(GenerateNonSwedishSection(languageGroup));
                }
            }

            htmlBuilder.Append("</body></html>");
            return htmlBuilder.ToString();
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
        public static string EscapeXml(string s)
        {
            return s.Replace("&", "&amp;");
        }



    }

}
