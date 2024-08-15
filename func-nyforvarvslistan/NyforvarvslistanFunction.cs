using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using func_nyforvarvslistan;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class NyforvarvslistanFunction
{
    private static readonly string elasticUsername = Environment.GetEnvironmentVariable("ElasticUser");
    private static readonly string elasticPassword = Environment.GetEnvironmentVariable("ElasticPassword");
    private static readonly string defaultIndex = Environment.GetEnvironmentVariable("DefaultIndex");
    private static readonly string elasticUrl = Environment.GetEnvironmentVariable("ElasticUrl");

    private static string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=funcmerkurprod;AccountKey=cxMb/qKxtVIEJNofq89YjnFGc/R3EI0C/ECj1xJYU4vMeAH95o8KWuLbp3/1KO3B6UggOxRzWKwa+AStvYC6dw==;EndpointSuffix=core.windows.net";
    private static string blobContainerName = "MinervaLastRunTimestamp";
    private static string blobFileName = "MinervaLastRunTimestamp";
    private static string tableName = "MinervaLastRunTimestamp";

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
    public static void Run([TimerTrigger("0 0 7 * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
    {
        try
        {
            if (DateTime.UtcNow.Day == 15)
            {
                SetBackMinervaLastRun(log);
                Task.Delay(30000).Wait();
                CreateLists(log);
            }
            else
            {
                SetBackMinervaLastRun(log);
            }
        }
        catch (RequestFailedException ex)
        {
            log.LogError($"Request failed with error: {ex.Message}");
        }
        catch (Exception ex)
        {
            log.LogError($"An error occurred: {ex.Message}");
        }
    }

    private static async Task SetBackMinervaLastRun(ILogger log)
    {
        log.LogInformation("Running SetBackMinervaLastRun method.");
        try
        {
        // Step 2: Update the timestamp in Table Storage
        var tableClient = new TableClient(storageConnectionString, tableName);

        string partitionKey = "Minerva";
        string rowKey = "Timestamp";

        var entity = await tableClient.GetEntityAsync<Azure.Data.Tables.TableEntity>(partitionKey, rowKey);
        var tableEntity = entity.Value;

        DateTime newTimestamp = DateTime.UtcNow.AddHours(-24);
        string formattedTimestamp = newTimestamp.ToString("yyyy-MM-ddTHH:mm:sszzz");


            if (tableEntity.ContainsKey("last_successful_run"))
        {
            tableEntity["last_successful_run"] = formattedTimestamp;
        }
        else
        {
            tableEntity.Add("last_successful_run", formattedTimestamp);
        }

        await tableClient.UpdateEntityAsync(tableEntity, tableEntity.ETag, TableUpdateMode.Replace);
        log.LogInformation("Table Storage last_successful_run timestamp updated successfully.");
        }
        catch (RequestFailedException ex)
        {
            log.LogError($"Request to Table Storage failed: {ex.Message}");
            throw; // Rethrow the exception to be caught by the outer catch block if needed
        }
        catch (Exception ex)
        {
            log.LogError($"An error occurred while updating the Table Storage: {ex.Message}");
            throw; // Rethrow the exception to be caught by the outer catch block if needed
        }
    }
    public static void CreateLists(ILogger log)
    {
        log.LogInformation($"Function triggered at: {DateTime.Now}");
        // var startDate = Dates.StartOfPreviousMonth.ToElasticsearchFormat();
        // var endDate = Dates.EndOfPreviousMonth.ToElasticsearchFormat();
        var targetMonth = DateTime.Now.AddMonths(-1); // Subtracting 2 months from the current month
        var startDate = new DateTime(targetMonth.Year, targetMonth.Month, 1); // Start of the target month
        var endDate = startDate.AddMonths(1).AddDays(-1); // End of the target month
        string startDateFormatted = startDate.ToString("yyyy-MM-dd");
        string endDateFormatted = endDate.ToString("yyyy-MM-dd");

        var response = Client.Search<ElasticSearchResponse>(s => s
            .Size(1000)
            .Query(q => q
                .DateRange(r => r
                    .Field("x-mtm-manufactured")
                    .GreaterThanOrEquals(startDateFormatted)
                    .LessThanOrEquals(endDateFormatted)
                )
            )
        );
        var deserializedResponse = JsonConvert.DeserializeObject<ElasticSearchResponse>(rawResponse);
        var books = deserializedResponse.Hits.hits.Select(hit =>
        {
            var source = hit._source;
            var publicationInfo = PublicationInfoExtractor.Extract(source.SearchResultItem.Volume);

            if (!source.SearchResultItem.UnderProduction && source.SearchResultItem.Remark != null && !source.SearchResultItem.Remark.Contains("Kurslitteratur") && !source.SearchResultItem.Title.Contains("Nya punktskriftsböcker") && !source.SearchResultItem.Title.Contains("Nya talböcker"))
            {
                return new Book
                {
                    Authors = source.SearchResultItem.Author ?? new List<Author>(),
                    Narrator = source.SearchResultItem.Narrator ?? new List<Narrator>(),
                    Translator = source.Translator?.Select(p => new Translator { Name = p.Name }).ToList() ?? new List<Translator>(),
                    PublisherName = source.SearchResultItem.Publisher?.Select(p => new Publisher { Name = p.Name }).ToList() ?? new List<Publisher>(),
                    Title = source.SearchResultItem.Title,
                    CoverHref = source.SearchResultItem.CoverHref,
                    Description = source.SearchResultItem.Description,
                    LibraryId = source.SearchResultItem.LibraryId,
                    Category = getCategoryBasedOnClassification(source.Classification),
                    PublicationCategory = source.PublicationCategories,
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

        foreach (var book in books)
        {
            if (book.PublicationCategory.FirstOrDefault() == "Fiction")
            {
                book.Category = "Sk�nlitteratur";
            }
        }

        var talkingBooks = books.Where(b => b.Format == "Talbok" || b.Format == "Talbok med text").ToList();
        var brailleBooks = books.Where(b => b.Format == "Punktskriftsbok").ToList();

        Console.WriteLine($"Found {talkingBooks.Count} talking books and {brailleBooks.Count} braille books.");


        var bookHtmlGenerator = new HtmlGenerator();
        var epubGenerator = new EpubGenerator();
        //var pdfGenerator = new PdfGenerator();
        //var docxGenerator = new DocxGenerator();
        var xmlGenerator = new XmlGenerator();
        if (Environment.GetEnvironmentVariable("WEBSITE_CONTENTSHARE") != null)
        {
            if (talkingBooks.Any())
            {
                string tempPath = Path.GetTempPath();

                xmlGenerator.SaveToFile(talkingBooks, Path.Combine(tempPath, "nyf-tb-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml"));
                xmlGenerator.SaveToFile(talkingBooks, Path.Combine(tempPath, "nyf-tb-no-links-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml"));
                xmlGenerator.SaveToFile(talkingBooks, Path.Combine(tempPath, "nyf-tb-no-links-swedishonly-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml"));

                // Additional processing, such as uploading to Blob Storage, goes here
            }

            if (brailleBooks.Any())
            {
                string tempPath = Path.GetTempPath();

                xmlGenerator.SaveToFile(brailleBooks, Path.Combine(tempPath, "nyf-punkt-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml"));
                xmlGenerator.SaveToFile(brailleBooks, Path.Combine(tempPath, "nyf-punkt-no-links-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml"));
                xmlGenerator.SaveToFile(brailleBooks, Path.Combine(tempPath, "nyf-punkt-no-links-swedishonly-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml"));

                // Additional processing, such as uploading to Blob Storage, goes here
            }
        } else {

        if (talkingBooks.Any())
        {
            xmlGenerator.SaveToFile(talkingBooks, "nyf-" + "tb-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml");
            xmlGenerator.SaveToFile(talkingBooks, "nyf-" + "tb-" + "no-links-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml");
            xmlGenerator.SaveToFile(talkingBooks, "nyf-" + "tb-" + "no-links-swedishonly-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml");
            //    string talkingBookHtml = bookHtmlGenerator.GenerateHtml(talkingBooks);
            //    File.WriteAllText("talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".html", talkingBookHtml);
            //    epubGenerator.GenerateEpub(talkingBookHtml, "talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".epub");
            //    pdfGenerator.GeneratePdf(talkingBookHtml, "talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".pdf");
            //    docxGenerator.GenerateDocx("talkingBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".docx", books);
        }

        if (brailleBooks.Any())
        {
            xmlGenerator.SaveToFile(brailleBooks, "nyf-" + "punkt-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml");
            xmlGenerator.SaveToFile(brailleBooks, "nyf-" + "punkt-" + "no-links-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml");
            xmlGenerator.SaveToFile(brailleBooks, "nyf-" + "punkt-" + "no-links-swedishonly-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml");
            //    string brailleBookHtml = bookHtmlGenerator.GenerateHtml(brailleBooks);
            //    File.WriteAllText("brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".html", brailleBookHtml);
            //    epubGenerator.GenerateEpub(brailleBookHtml, "brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".epub");
            //    pdfGenerator.GeneratePdf(brailleBookHtml, "brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".pdf");
            //    docxGenerator.GenerateDocx("brailleBook-" + Dates.GetCurrentYear(Dates.StartOfPreviousMonth) + "-" + Dates.GetMonthNameInSwedish(Dates.StartOfPreviousMonth) + ".docx", books);
        }
    }
        List<string> generatedFiles = new List<string>();
        if (Environment.GetEnvironmentVariable("WEBSITE_CONTENTSHARE") != null)
        {
            if (talkingBooks.Any())
            {
                string tempPath = Path.GetTempPath();

                string talkingBooksFile = Path.Combine(tempPath, $"nyf-tb-{DateTime.Now:yyyy-MM}.xml");
                generatedFiles.Add(talkingBooksFile);

                string talkingBooksNoLinksFile = Path.Combine(tempPath, $"nyf-tb-no-links-{DateTime.Now:yyyy-MM}.xml");
                generatedFiles.Add(talkingBooksNoLinksFile);

                string talkingBooksSwedishOnlyFile = Path.Combine(tempPath, $"nyf-tb-no-links-swedishonly-{DateTime.Now:yyyy-MM}.xml");
                generatedFiles.Add(talkingBooksSwedishOnlyFile);
            }

            if (brailleBooks.Any())
            {
                string tempPath = Path.GetTempPath();

                string brailleBooksFile = Path.Combine(tempPath, $"nyf-punkt-{DateTime.Now:yyyy-MM}.xml");
                generatedFiles.Add(brailleBooksFile);

                string brailleBooksNoLinksFile = Path.Combine(tempPath, $"nyf-punkt-no-links-{DateTime.Now:yyyy-MM}.xml");
                generatedFiles.Add(brailleBooksNoLinksFile);

                string brailleBooksSwedishOnlyFile = Path.Combine(tempPath, $"nyf-punkt-no-links-swedishonly-{DateTime.Now:yyyy-MM}.xml");
                generatedFiles.Add(brailleBooksSwedishOnlyFile);
            }
        }
        else
        {
            if (talkingBooks.Any())
            {
                string talkingBooksFile = "nyf-tb-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml";
                generatedFiles.Add(talkingBooksFile);

                string talkingBooksNoLinksFile = "nyf-tb-no-links-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml";
                generatedFiles.Add(talkingBooksNoLinksFile);

                string talkingBooksSwedishOnlyFile = "nyf-tb-no-links-swedishonly-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml";
                generatedFiles.Add(talkingBooksSwedishOnlyFile);
            }

            if (brailleBooks.Any())
            {
                string brailleBooksFile = "nyf-punkt-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml";
                generatedFiles.Add(brailleBooksFile);

                string brailleBooksNoLinksFile = "nyf-punkt-no-links-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml";
                generatedFiles.Add(brailleBooksNoLinksFile);

                string brailleBooksSwedishOnlyFile = "nyf-punkt-no-links-swedishonly-" + DateTime.Now.Year + "-" + DateTime.Now.Month.ToString("00") + ".xml";
                generatedFiles.Add(brailleBooksSwedishOnlyFile);
            }
        }



        if (generatedFiles.Any())
        {
            SendEmailWithAttachments(generatedFiles.ToArray(), "erik.johansson@mtm.se");
            // SendEmailWithAttachments(generatedFiles.ToArray(), "otto.ewald@mtm.se");
        }
    }

    public static async void SendEmailWithAttachments(string[] filePaths, string emailAddress)
    {
        var client = new MailjetClient("8fc6ccd381fcb4ec47dc2980c44a99de", "e3f4b681f0dad6c3aa27fa3702d71449");

        var message = new JObject
        {
            { "From", new JObject { { "Email", "erik.johansson@mtm.se" }, { "Name", "Erik Johansson" } } },
            { "To", new JArray { new JObject { { "Email", emailAddress }, { "Name", "Otto Ewald" } } } },
            { "Subject", "Genererade xml-filer" },
            { "TextPart", "H�r kommer filerna." }
        };

        var attachments = new JArray();
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                var fileContent = Convert.ToBase64String(File.ReadAllBytes(filePath));
                var attachment = new JObject
            {
                { "ContentType", "application/xml" },
                { "Filename", Path.GetFileName(filePath) },
                { "Base64Content", fileContent }
            };
                attachments.Add(attachment);
            }
        }

        message["Attachments"] = attachments;

        var request = new MailjetRequest
        {
            Resource = SendV31.Resource
        }
        .Property(Send.Messages, new JArray { message });

        var response = await client.PostAsync(request);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Email sent successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to send email. Status code: {response.StatusCode}, Error: {response.GetErrorMessage()}");
            Console.WriteLine(response.GetData());
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

        if (category == null)  // Use Dewey, and match the Dewey classification to an SAB one, if no SAB classification was found
        {
            string fileContent = null;
            string containerName = "nyforvarvslistan";
            string blobName = "Dewey_SAB.txt";
            SABDeweyMapper deweyMapper = null;

            if (Environment.GetEnvironmentVariable("WEBSITE_CONTENTSHARE") != null)
            {
                // Running in Azure, read from Blob Storage
                try
                {
                    BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    BlobClient blobClient = containerClient.GetBlobClient(blobName);

                    if (blobClient.Exists())
                    {
                        BlobDownloadInfo download = blobClient.Download();
                        using (var reader = new StreamReader(download.Content, true))
                        {
                            fileContent = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception, log error, or rethrow as necessary
                    throw new InvalidOperationException("Error reading Dewey_SAB.txt from Blob Storage", ex);
                }

                if (fileContent != null)
                {
                    deweyMapper = new SABDeweyMapper(fileContent);
                    // Continue processing with deweyMapper
                }
            }
            else
            {
                // Running locally
                string filePath = Path.Combine(Environment.CurrentDirectory, "Dewey_SAB.txt");
                deweyMapper = new SABDeweyMapper(filePath, true);
                // Continue processing with deweyMapper
            }

            // Process classifications using deweyMapper
            foreach (var classification in classifications)
            {
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


        return category ?? "Allm�nt och blandat";
    }
}
