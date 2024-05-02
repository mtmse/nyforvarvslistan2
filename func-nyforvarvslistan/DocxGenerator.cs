using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Linq;
using Xceed.Document.NET;
using Xceed.Words.NET;
using Nest;
using System.Globalization;

namespace func_nyforvarvslistan
{
    public class DocxGenerator
    {
        public void GenerateDocx(string path, List<Book> books)
        {
            using (DocX document = DocX.Create(path))
            {
                var formatting = new Formatting();
                formatting.Language = new CultureInfo("sv-SE");
                var groupedByCategory = books.GroupBy(b => b.Category);
                var groupedByLanguage = books.GroupBy(b => b.Language == "Svenska" ? "Swedish" : "Non-Swedish").OrderBy(g => g.Key == "Swedish" ? 0 : 1);
                document.InsertParagraph("Nya talböcker augusti 2023")
                            .FontSize(30)
                            .Bold()
                            .SpacingAfter(30)
                            .Font(new Xceed.Document.NET.Font("Arial"))
                            .Culture(new CultureInfo("sv-SE"));
                document.InsertParagraph("Inledning")
                        .FontSize(20)
                        .Bold()
                        .SpacingAfter(20)
                        .Font(new Xceed.Document.NET.Font("Arial"))
                        .Culture(new CultureInfo("sv-SE"));
                document.InsertParagraph("Listan är uppdelad i 3 delar; Böcker för vuxna, Böcker för Barn och Böcker på andra språk än svenska, vilka ligger på nivå 1. Böcker för vuxna och Böcker för barn är uppdelade mellan Skönlitteratur och Faktaböcker respektive Faktaböcker. Dessa avsnitt ligger på nivå 2. Böcker på andra språk än svenska är uppdelade mellan Böcker för vuxna och Böcker för barn. Boktitlarna ligger på nivå 3 i avsnitten Skönlitteratur och Böcker på andra språk än svenska, medan de ligger på nivå 4 i avsnitten Faktaböcker och Faktaböcker. På Nivå 3 i avsnitten Faktaböcker och Faktaböcker finns de olika fackavdelningarna.")
                        .FontSize(13.5)
                        .SpacingAfter(20)
                        .Font(new Xceed.Document.NET.Font("Arial"))
                        .Culture(new CultureInfo("sv-SE"));
                document.InsertParagraph($"Listan omfattar {books.Count()} titlar.")
                        .FontSize(13.5)
                        .SpacingAfter(30)
                        .Font(new Xceed.Document.NET.Font("Arial"))
                        .Culture(new CultureInfo("sv-SE"));

                foreach (var languageGroup in groupedByLanguage)
                {
                    //Paragrafer för svenska och icke-svenska böcker
                    foreach (var categoryGroup in groupedByCategory)
                    {
                        document.InsertParagraph(categoryGroup.Key)
                                .FontSize(20)
                                .Bold()
                                .SpacingAfter(30)
                                .Font(new Xceed.Document.NET.Font("Arial"))
                                .Culture(new CultureInfo("sv-SE"));

                        foreach (var book in categoryGroup)
                        {
                            var titlePara = document.InsertParagraph(book.Title).FontSize(13.5).Bold().SpacingAfter(10).Font(new Xceed.Document.NET.Font("Arial")).Culture(new CultureInfo("sv-SE")).KeepLinesTogether();
                            titlePara.KeepWithNextParagraph();
                            var paraBy = document.InsertParagraph();
                            paraBy.Append("Av ").Bold().FontSize(14).Font(new Xceed.Document.NET.Font("Arial")).SpacingAfter(10).Culture(new CultureInfo("sv-SE")).Append(string.Join(", ", book.Authors.Select(a => a.Name))).FontSize(14).Font(new Xceed.Document.NET.Font("Arial")).SpacingAfter(10).Culture(new CultureInfo("sv-SE"));
                            paraBy.KeepLinesTogether();
                            paraBy.KeepWithNextParagraph();
                            var paraDesc = document.InsertParagraph();
                            paraDesc.Append("Beskrivning: ").FontSize(14).Font(new Xceed.Document.NET.Font("Arial")).SpacingAfter(20).Culture(new CultureInfo("sv-SE")).Bold().Append(book.Description).FontSize(14).Font(new Xceed.Document.NET.Font("Arial")).SpacingAfter(20).Culture(new CultureInfo("sv-SE"));
                            paraDesc.KeepLinesTogether();
                        }
                    }
                }
                
                document.Save();
            }
        }
    }
}
