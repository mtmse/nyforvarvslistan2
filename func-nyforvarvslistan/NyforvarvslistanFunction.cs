using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using func_nyforvarvslistan;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;

public static class NyforvarvslistanFunction
{
    private static readonly string elasticUsername = Environment.GetEnvironmentVariable("ElasticUser");
    private static readonly string elasticPassword = Environment.GetEnvironmentVariable("ElasticPassword");
    private static readonly string defaultIndex = Environment.GetEnvironmentVariable("DefaultIndex");
    private static readonly string elasticUrl = Environment.GetEnvironmentVariable("ElasticUrl");
    public static string rawResponse { get; private set; }
    private static readonly ConnectionSettings ConnectionSettings = new ConnectionSettings(new Uri(elasticUrl))
        .DefaultIndex(defaultIndex)
        .BasicAuthentication(elasticUsername, elasticPassword)
        .DisableDirectStreaming()
        .OnRequestCompleted(details =>
        {
            rawResponse = System.Text.Encoding.UTF8.GetString(details.ResponseBodyInBytes);
        });

    private static readonly ElasticClient Client = new ElasticClient(ConnectionSettings);

    [FunctionName("NyforvarvslistanFunction")]
    public static void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation($"Function triggered at: {DateTime.Now}");

        var startDate = Dates.StartOfPreviousMonth.ToElasticsearchFormat();
        var endDate = Dates.EndOfPreviousMonth.ToElasticsearchFormat();
        // var targetMonth = DateTime.Now.AddMonths(-2); // Subtracting 2 months from the current month
        // var startDate = new DateTime(targetMonth.Year, targetMonth.Month, 1); // Start of the target month
        // var endDate = startDate.AddMonths(1).AddDays(-1).ToElasticsearchFormat(); // End of the target month

        var response = Client.Search<ElasticSearchResponse>(s => s
            .Size(1000)
            .Query(q => q
                .DateRange(r => r
                    .Field("PublishedDate")
                    .GreaterThanOrEquals(startDate)
                    .LessThanOrEquals(endDate)
                )
            )
        );
        var deserializedResponse = JsonConvert.DeserializeObject<ElasticSearchResponse>(rawResponse);
        var books = deserializedResponse.Hits.hits.Select(hit =>
        {
            var source = hit._source;
            var publicationInfo = PublicationInfoExtractor.Extract(source.SearchResultItem.Volume);

            if (!source.SearchResultItem.UnderProduction && !source.SearchResultItem.Remark.Contains("Kurslitteratur"))
            {
                return new Book
                {
                    Authors = source.SearchResultItem.Author ?? new List<Author>(),
                    Narrator = source.SearchResultItem.Narrator ?? new List<Narrator>(),
                    Translator = source.SearchResultItem.Translator ?? new List<Translator>(),
                    PublisherName = source.SearchResultItem.Publisher?.Select(p => new Publisher { Name = p.Name }).ToList() ?? new List<Publisher>(),
                    Title = source.SearchResultItem.Title,
                    CoverHref = source.SearchResultItem.CoverHref,
                    Description = source.SearchResultItem.Description,
                    LibraryId = source.SearchResultItem.LibraryId,
                    Category = getCategoryBasedOnClassification(source.Classification),
                    AgeGroup = (source.SearchResultItem.AgeGroup == "Adult" || source.SearchResultItem.AgeGroup == "General") ? "Adult" : "Juvenile",
                    Language = source.SearchResultItem.Language,
                    LibrisId = source.SearchResultItem.LibrisId,
                    Format = source.SearchResultItem.Format,
                    City = publicationInfo?.City,
                    PublishingCompany = publicationInfo?.PublishingCompany,
                    PublishedYear = publicationInfo?.PublishedYear,
                    Extent = source.SearchResultItem.Extent
                };
            }
            return null;
        }).Where(book => book != null)
          .ToList();

        var talkingBooks = books.Where(b => b.Format == "Talbok" || b.Format == "Talbok med text").ToList();
        var brailleBooks = books.Where(b => b.Format == "Punktskriftsbok").ToList();

        //var bookHtmlGenerator = new HtmlGenerator();
        //var epubGenerator = new EpubGenerator();
        //var pdfGenerator = new PdfGenerator();
        //var docxGenerator = new DocxGenerator();
        var xmlGenerator = new XmlGenerator();

        if (talkingBooks.Any())
        {
            xmlGenerator.SaveToFile(talkingBooks, "nyforvarvslistan-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-talbok-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".xml");
            xmlGenerator.SaveToFile(talkingBooks, "nyforvarvslistan-no-links-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-talbok-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".xml");
            xmlGenerator.SaveToFile(talkingBooks, "nyforvarvslistan-no-links-swedishonly-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-talbok-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".xml");
            //    string talkingBookHtml = bookHtmlGenerator.GenerateHtml(talkingBooks);
            //    File.WriteAllText("talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".html", talkingBookHtml);
            //    epubGenerator.GenerateEpub(talkingBookHtml, "talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".epub");
            //    pdfGenerator.GeneratePdf(talkingBookHtml, "talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".pdf");
            //    docxGenerator.GenerateDocx("talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".docx", books);
        }

        if (brailleBooks.Any())
        {
            xmlGenerator.SaveToFile(brailleBooks, "nyforvarvslistan-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-punktskrift-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".xml");
            xmlGenerator.SaveToFile(brailleBooks, "nyforvarvslistan-no-links-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-punktskrift-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".xml");
            xmlGenerator.SaveToFile(brailleBooks, "nyforvarvslistan-no-links-swedishonly-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-punktskrift-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".xml");
            //    string brailleBookHtml = bookHtmlGenerator.GenerateHtml(brailleBooks);
            //    File.WriteAllText("brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".html", brailleBookHtml);
            //    epubGenerator.GenerateEpub(brailleBookHtml, "brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".epub");
            //    pdfGenerator.GeneratePdf(brailleBookHtml, "brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".pdf");
            //    docxGenerator.GenerateDocx("brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".docx", books);
        }
    }

    private static readonly Dictionary<string, string> sabCategories = new Dictionary<string, string>
    {
        { "A", "Bok- och biblioteksväsen" },
        { "B", "Allmänt och blandat" },
        { "C", "Religion" },
        { "D", "Filosofi och psykologi" },
        { "E", "Uppfostran och undervisning" },
        { "F", "Språkvetenskap" },
        { "G", "Litteraturvetenskap" },
        { "H", "Skönlitteratur" },
        { "I", "Konst, musik, teater, film, fotografi" },
        { "J", "Arkeologi" },
        { "K", "Historia" },
        { "L", "Biografi med genealogi" },
        { "M", "Etnografi, socialantropologi och etnologi" },
        { "N", "Geografi och lokalhistoria" },
        { "O", "Samhälls- och rättsvetenskap" },
        { "P", "Teknik, industri och kommunikationer" },
        { "Q", "Ekonomi och näringsväsen" },
        { "R", "Idrott, lek och spel" },
        { "S", "Militärväsen" },
        { "T", "Matematik" },
        { "U", "Naturvetenskap" },
        { "V", "Medicin" },
        { "X", "Musikalier" },
        { "Y", "Musikinspelningar" },
        { "Ä", "Tidningar" }
    };

    public static string ToElasticsearchFormat(this DateTime date)
    {
        return date.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
    }
    private static string getCategoryBasedOnClassification(List<string> classifications)
    {
        if (classifications == null || !classifications.Any())
            return "Allmänt och blandat";

        string category = null;
        foreach (var classification in classifications)
        {
            if (string.IsNullOrEmpty(classification)) continue;

            var key = classification[0].ToString().ToUpper();
            if (classification[0] == 'u' && classification.Length > 1)
            {
                key = classification[1].ToString().ToUpper();
            }

            if (sabCategories.TryGetValue(key, out category))
            {
                break;
            }
        }

        if (category == null)  //Use Dewey, and match the Dewey classification to an SAB one, if no SAB classification was found
        {
            foreach (var classification in classifications)
            {
                SABDeweyMapper deweyMapper = new SABDeweyMapper("C:\\Users\\ERJO\\source\\repos\\func-nyforvarvslistan\\func-nyforvarvslistan\\Dewey_SAB.txt");
                var convertedClassification = deweyMapper.getSabCode(classification);
                var key = convertedClassification[0].ToString().ToUpper();
                if (convertedClassification[0] == 'u' && convertedClassification.Length > 1)
                {
                    key = convertedClassification[1].ToString().ToUpper();
                }

                if (sabCategories.TryGetValue(key, out category))
                {
                    break;
                }
            }
        }

        return category ?? "Allmänt och blandat";
    }
}