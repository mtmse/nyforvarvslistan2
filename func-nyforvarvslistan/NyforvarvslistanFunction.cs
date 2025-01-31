using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Net.Http;
using func_nyforvarvslistan.Models;
using func_nyforvarvslistan;
using Newtonsoft.Json.Linq;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.IO.Compression;

public static class NyforvarvslistanFunction
{
    static string _clientId = "a3656140-53e9-4014-8587-1ebc02a3d958";
    static string _clientSecret = "ZFL8Q~Gqy84Xgkkh4eqrVAeHXiWYDaUr6mRFYchg";
    static string _tenantId = "59413b88-a6c3-4dd7-ab9f-2470536e50f5";
    static string _resourceUrl = "https://mtm-prod.operations.dynamics.com";
    static string _baseODataEndpoint = "https://mtm-prod.operations.dynamics.com/data/";

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
    private static DateTime? lastRunDate = null;

    [FunctionName("NyforvarvslistanFunction")]
    public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("NyforvarvslistanFunction HTTP trigger function processed a request.");


        Dictionary<int, string> pssDict = new Dictionary<int, string>
        {
            { 21, "Interfolierad bok." },
            { 22, "Bredvidbok i ficka." },
            { 23, "Bok med inklistrad punktskrift." },
            { 25, "Punktskriftsbok för lästräning." },
            { 26, "Punktskriftsbok med bilder och storstil." },
            { 27, "Punktskriftsbok, bredvidbok, lätt att läsa." },
        };

        var booksProduction = await GetProductionTitles(pssDict);

        log.LogInformation($"Found {booksProduction.Count} books in production.");

        SetBackMinervaLastRun(log);
        Task.Delay(300).Wait();
        var generatedFiles = CreateLists(log, booksProduction);

        try
        {
            if (generatedFiles.Any())
            {
                var zipStream = new MemoryStream(); // Removed 'using' statement
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var file in generatedFiles)
                    {
                        var zipEntry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);
                        using (var entryStream = zipEntry.Open())
                        {
                            await entryStream.WriteAsync(file.Content, 0, file.Content.Length);
                        }
                    }
                }

                zipStream.Seek(0, SeekOrigin.Begin);
                string zipFileName = $"Nyforvarvslistan-{DateTime.Now:yyyy-MM}.zip";

                return new FileStreamResult(zipStream, "application/zip")
                {
                    FileDownloadName = zipFileName
                };
            }
            else
            {
                return new BadRequestObjectResult("No files were generated.");
            }
        }
        catch (Exception ex)
        {
            log.LogError($"An error occurred while creating the zip file: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    public static string Extract(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            var match = Regex.Match(input, @"Anpassad från: (.+?) : (.+?), (\d{4})\.");
            if (match.Success)
            {
                return match.Groups[2].Value.Trim();
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public static string ExtractYear(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            var match = Regex.Match(input, @"Anpassad från: (.+?) : (.+?), (\d{4})\.");
            if (match.Success)
            {
                return match.Groups[3].Value.Trim();
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    private static async Task<List<Book>> GetProductionTitles(Dictionary<int, string> pssDict)
    {
        var fromDate = Dates.StartOfPreviousMonth;
        var toDate = Dates.EndOfPreviousMonth;
        var smmmActivities = await GetSmmActivities(fromDate, toDate);

        HashSet<string> activityNumbers = smmmActivities
            .Where(activity => !string.IsNullOrEmpty(activity.ActivityNumber))
            .Select(activity => activity.ActivityNumber)
            .ToHashSet();

        var jsonResponse = await GetProductionRoutesAsync(activityNumbers);

        Console.WriteLine($"Hittade {jsonResponse.Count} produktioner.");

        HashSet<string> caseIds = jsonResponse
            .Where(route => !string.IsNullOrEmpty(route.CaseId))
            .Select(route => route.CaseId)
            .ToHashSet();

        var productionHeaders = await GetProductionHeadersAsync(caseIds);

        Console.WriteLine($"Hittade {productionHeaders.Count} ProductionHeaders.");

        HashSet<string> titleNos = productionHeaders
                .Where(header => !string.IsNullOrEmpty(header.CaseId))
                .Select(header => header.CaseId)
                .ToHashSet();

        Console.WriteLine($"Hittade {titleNos.Count} TitleNos.");

        List<InstanceDetail> instanceDetails = await GetInstanceDetailsAsync(titleNos);

        Dictionary<string, InstanceDetail> instanceDetailsDict = instanceDetails
            .Where(detail => !string.IsNullOrEmpty(detail.LibrisNumber))
            .GroupBy(detail => detail.ItemId)
            .ToDictionary(g => g.Key, g => g.Last());

        HashSet<string> titleIds = instanceDetails
            .Where(detail => !string.IsNullOrEmpty(detail.ItemId))
            .Select(detail => detail.ItemId)
            .ToHashSet();


        var duplicateLibrisNumbers = instanceDetails
            .GroupBy(detail => detail.ItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();


        List<MTMTitle> mtmTitles = await GetMTMTitlesAsync(titleIds);

        Dictionary<string, MTMTitle> mtmTitlesDict = mtmTitles
            .Where(title => !string.IsNullOrEmpty(title.TitleId))
            .ToDictionary(title => title.TitleId);

        Console.WriteLine($"Hittade {mtmTitles.Count} MTMTitles.");

        List<Book> books = instanceDetails
                .Where(instanceDetail => !string.IsNullOrEmpty(instanceDetail.ItemId))
                .Select(instanceDetail =>
                {
                    // Försök hitta motsvarande InstanceDetail baserat på TitleId
                    mtmTitlesDict.TryGetValue(instanceDetail.ItemId, out MTMTitle title);

                    return new Book
                    {
                        LibraryId = instanceDetail.DaisyNo,
                        Title = title.Title,
                        Translator = title.Translator,
                        LibrisId = instanceDetail.LibrisNumber,
                        Format = instanceDetail.ProdMediaTypeDescr,
                        Publisher = title.Publisher,
                        PublishedYear = title.PublicationYear,
                        Category = title.Category.Contains("Skön") ? "Skönlitteratur" : "Facklitteratur",
                        Classification = title.ClassificationCode,
                        Notes = title.Notes,
                        Comments = instanceDetail.Comments,
                        Language = title.Language,
                        Narrator = instanceDetail.Reader,
                        Extent = instanceDetail.ProdMediaType.ToUpperInvariant() switch
                        {
                            "PS" or "PSS" => instanceDetail.NoOfVolumes > 0 ? instanceDetail.NoOfVolumes.ToString() : null,
                            "TB" => instanceDetail.PlayTime > 0 ? instanceDetail.PlayTime.ToString() : null,
                            "TBF" or "SYTB" => instanceDetail.NoOfPagesXML > 0 ? instanceDetail.NoOfPagesXML.ToString() : null,
                            _ => null
                        },
                        SubType = pssDict.ContainsKey(instanceDetail.SubTypeId) ? pssDict[instanceDetail.SubTypeId] : null,
                        NoVolumes = instanceDetail.NoOfVolumes,
                        NoPagesXML = instanceDetail.NoOfPagesXML,
                        NoPagesPS = instanceDetail.SubTypeId > 0
                            ? (instanceDetail.NoOfSheetsPSorPSS == 0
                                ? instanceDetail.NoOfSheets
                                : instanceDetail.NoOfSheetsPSorPSS)
                            : instanceDetail.NoOfPagesPS,
                        PlayTime = instanceDetail.PlayTimeStr,
                        AgeGroup = (!string.IsNullOrEmpty(title.Category) && title.Category.EndsWith("V")) ? "Adult" : "Juvenile",
                        Description = "Testbeskrivning så länge",
                        City = null,
                        PublishingCompany = title.Publisher,
                        PSNo = instanceDetail.PSNo,

                    };
                }).Where(book => !book.Title.Contains("Nya talböcker")
                && !book.Title.Contains("Nya punktskriftsböcker")).ToList();

        Console.WriteLine($"Total Books: {books.Count}");
        return books;
    }

    private async static Task<List<SmmActivities>> GetSmmActivities(DateTime fromDate, DateTime toDate)
    {
        string token = await GetAccessTokenAsync();
        List<SmmActivities> allSmmActivities = new List<SmmActivities>();
        string requestUrl = "https://mtm-prod.operations.dynamics.com/data/IsmmActivities?$filter=ActualEndDateTime%20ge%20" + fromDate.ToString("o") + "%20and%20ActualEndDateTime%20le%20" + toDate.ToString("o") + "%20and%20OperNo%20eq%20170";
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            while (!string.IsNullOrEmpty(requestUrl))
            {
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync();
                    ODataResponse<SmmActivities> dynamicsData = JsonConvert.DeserializeObject<ODataResponse<SmmActivities>>(jsonData);

                    string formattedDate = fromDate.ToString("o");

                    allSmmActivities.AddRange(dynamicsData.Value);

                    // Uppdatera nästa länk för paginering
                    requestUrl = dynamicsData.NextLink;
                }
                else
                {
                    // Hantera fel
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-anrop misslyckades med statuskod: {response.StatusCode}\nDetaljer: {errorContent}");
                }
            }
        }

        return allSmmActivities;
    }

    public static async Task<List<InstanceDetail>> GetInstanceDetailsAsync(HashSet<string> titleIds)
    {
        List<InstanceDetail> allInstanceDetails = new List<InstanceDetail>();
        string _instanceDetailsEndpoint = "https://mtm-prod.operations.dynamics.com/data/InstanceDetails";

        // Dela upp TitleIds i batcher om nödvändigt
        int batchSize = 50; // Anpassa baserat på API-begränsningar
        List<List<string>> batches = titleIds
            .Select((id, index) => new { id, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.id).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            string odataFilter = string.Join(" or ", batch.Select(id => $"CaseId eq '{id}'"));
            string requestUrl = $"{_instanceDetailsEndpoint}?$filter={Uri.EscapeDataString(odataFilter)}&$select=ItemId,CaseId,NoOfVolumes,SubTypeBook,ProposalCreatedDateTime,ProdMediaType,Format,PictType,IsReadSlow,LastPagePS,LineSpacing,NoOfChar,IsHybridProd,PlayTimeStr,SubTypeId,NoOfPagesPS,LibrisNumber,ETNo,IsDoublePage,LengthFactorReal,NoOfPagesIBD,IntExt,IsPictDescrProduced,IsPublished,NoOfSheets,ProposalType,IsRelfPict,DaisyNo,ReadComplexity,Reader,PSNo,ISBNId,DesignerStr,NoOfPict,FileSize,IsMarkedOriginal,NoOfIllustrations,Comments,IsAuthorStatus,IsRawCopyOrdered,IsUpperCase,PlayTime,Length,IsFrontCover,Printing,ProdMediaTypeDescr,IsPictures,ReadSequence,IsEditingReady,NoOfPagesXML,NoOfSheetsPSorPSS&$top=1000";

            string token = await GetAccessTokenAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                while (!string.IsNullOrEmpty(requestUrl))
                {
                    HttpResponseMessage response = await client.GetAsync(requestUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonData = await response.Content.ReadAsStringAsync();

                        ODataResponse<InstanceDetail> instanceDetailsData = JsonConvert.DeserializeObject<ODataResponse<InstanceDetail>>(jsonData);

                        allInstanceDetails.AddRange(instanceDetailsData.Value);

                        requestUrl = instanceDetailsData.NextLink;
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Fel vid InstanceDetails API-anrop: {response.StatusCode}");
                        Console.WriteLine($"Detaljer: {errorContent}");
                        throw new Exception($"InstanceDetails API-anrop misslyckades med statuskod: {response.StatusCode}\nDetaljer: {errorContent}");
                    }
                }
            }
        }

        return allInstanceDetails;
    }

    public static async Task<List<MTMTitle>> GetMTMTitlesAsync(HashSet<string> titleNos)
    {
        List<MTMTitle> allTitles = new List<MTMTitle>();
        string _mtmTitlesEndpoint = "https://mtm-prod.operations.dynamics.com/data/MTMTitles";

        int batchSize = 5;
        List<List<string>> batches = titleNos
            .Select((id, index) => new { id, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.id).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            string odataFilter = string.Join(" or ", batch.Select(id => $"TitleId eq '{id}'"));
            string requestUrl = $"{_mtmTitlesEndpoint}?$filter={Uri.EscapeDataString(odataFilter)}&$select=TitleId,Title,Author,Translator,LibrisNumber,Publisher,PublicationYear,ClassificationCode,Category,Language,Publisher&$top=1000";

            string token = await GetAccessTokenAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                while (!string.IsNullOrEmpty(requestUrl))
                {
                    HttpResponseMessage response = await client.GetAsync(requestUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonData = await response.Content.ReadAsStringAsync();

                        ODataResponse<MTMTitle> titlesData = JsonConvert.DeserializeObject<ODataResponse<MTMTitle>>(jsonData);

                        allTitles.AddRange(titlesData.Value);

                        requestUrl = titlesData.NextLink;
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Fel vid MTMTitles API-anrop: {response.StatusCode}");
                        Console.WriteLine($"Detaljer: {errorContent}");
                        throw new Exception($"MTMTitles API-anrop misslyckades med statuskod: {response.StatusCode}\nDetaljer: {errorContent}");
                    }
                }
            }
        }
        Console.WriteLine($"Hittade {allTitles.Count} MTM-titlar.");
        return allTitles;
    }


    public static async Task<List<ProductionHeader>> GetProductionHeadersAsync(HashSet<string> caseIds)
    {
        List<ProductionHeader> allHeaders = new List<ProductionHeader>();
        string _baseODataEndpoint = "https://mtm-prod.operations.dynamics.com/data/ProductionHeaders";

        // Dela upp CaseIds i batcher om nödvändigt
        int batchSize = 50; // Anpassa baserat på API-begränsningar
        List<List<string>> batches = caseIds
            .Select((id, index) => new { id, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.id).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            // Bygg $filter med OR för varje CaseId i batchen
            string odataFilter = string.Join(" or ", batch.Select(id => $"CaseId eq '{id}'"));
            string requestUrl = $"{_baseODataEndpoint}?$filter={Uri.EscapeDataString(odataFilter)}&$select=CaseId,ProdStatus,OrderTypeId,ProdMediaType,TitleNo&$top=1000";

            using (HttpClient client = new HttpClient())
            {
                // Använd din befintliga autentiseringsmetod för att få en token
                string token = await GetAccessTokenAsync(); // Justera om nödvändigt
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                while (!string.IsNullOrEmpty(requestUrl))
                {
                    HttpResponseMessage response = await client.GetAsync(requestUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonData = await response.Content.ReadAsStringAsync();
                        ODataResponse<ProductionHeader> headersData = JsonConvert.DeserializeObject<ODataResponse<ProductionHeader>>(jsonData);

                        allHeaders.AddRange(headersData.Value);

                        // Uppdatera nästa länk för paginering
                        requestUrl = headersData.NextLink;
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-anrop misslyckades med statuskod: {response.StatusCode}\nDetaljer: {errorContent}");
                    }
                }
            }
        }

        return allHeaders.FindAll(header => header.OrderTypeId == "BS");
    }


    public static async Task<string> GetAccessTokenAsync()
    {
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(_clientId)
            .WithClientSecret(_clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_tenantId}"))
            .Build();

        string[] scopes = new string[] { $"{_resourceUrl}/.default" };
        AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        return result.AccessToken;
    }

    public static async Task<List<ProductionRoute>> GetProductionRoutesAsync(HashSet<string> activityNumbers)
    {
        if (activityNumbers == null || activityNumbers.Count == 0)
            throw new ArgumentException("activityNumbers kan inte vara null eller tom.", nameof(activityNumbers));

        string token = await GetAccessTokenAsync(); // Antag att denna metod finns och returnerar en giltig access token
        List<ProductionRoute> allRoutes = new List<ProductionRoute>();

        // Bas-URL för API:et
        string baseUrl = "https://mtm-prod.operations.dynamics.com/data/ProductionRoutes";

        // Definiera batchstorlek för att undvika för långa URL:er
        int batchSize = 50;

        // Dela upp ActivityNumbers i batchar
        var batches = activityNumbers
            .Select((activityNumber, index) => new { activityNumber, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.activityNumber).ToList())
            .ToList();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            foreach (var batch in batches)
            {
                string filter = string.Join(" or ", batch.Select(an => $"ActivityNumber eq '{an.Replace("'", "''")}'"));

                string requestUrl = $"{baseUrl}?$filter={Uri.EscapeDataString(filter)}";

                HttpResponseMessage response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    var odataResponse = JsonConvert.DeserializeObject<ODataResponse<ProductionRoute>>(jsonResult);
                    if (odataResponse != null && odataResponse.Value != null)
                    {
                        allRoutes.AddRange(odataResponse.Value);
                    }
                }
                else
                {
                    string errorResult = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Fel vid hämtning av ProductionRoutes: {response.StatusCode}, {errorResult}");
                }
            }
        }

        return allRoutes;
    }

    private static async Task SetBackMinervaLastRun(ILogger log)
    {
        log.LogInformation("Running SetBackMinervaLastRun method.");
        try
        {
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
    public static List<(string FileName, byte[] Content)> CreateLists(ILogger log, List<Book> booksProd)
    {
        log.LogInformation("Running CreateLists method.");
        log.LogInformation($"Found {booksProd.Count} books in production.");
        foreach (var book in booksProd)
        {
            var bookId = book.LibraryId;
            log.LogInformation($"Searching for book {bookId} in Elasticsearch.");
            if (book.LibraryId == null || book.LibraryId == "" || !book.LibraryId.StartsWith("C"))
            {
                bookId = book.PSNo;
            }

            var response = Client.Search<ElasticSearchResponse>(s => s
                .Index("opds-1.1.0")
                .Query(q => q
                    .Match(m => m
                        .Field("_id")
                        .Query(bookId)
                    )
                )
            );

            log.LogInformation($"Elasticsearch returned {response.Documents.Count} documents for book {bookId}.");

            var deserializedResponse = JsonConvert.DeserializeObject<ElasticSearchResponse>(rawResponse);
            var books = deserializedResponse.Hits.hits.FirstOrDefault()._source;
            if (books != null)
            {
                book.LibraryId = bookId;
                book.Description = books.SearchResultItem.Description;
                book.Language = books.SearchResultItem.Language;
                book.Author = books.SearchResultItem.Author;
                if (book.PublishingCompany == null || book.Publisher == "")
                {
                    book.PublishingCompany = Extract(books.SearchResultItem.Remark);
                }

                if (book.PublishedYear == 0)
                {
                    book.PublishedYear = int.Parse(ExtractYear(books.SearchResultItem.Remark));
                }

                if (book.Author == null || !book.Author.Any())
                {
                    List<Author> authors = new List<Author>();
                    Author author = new Author();
                    var firstEditor = books.Editors?.FirstOrDefault();
                    if (firstEditor != null)
                    {
                        author.Name = firstEditor.Name;
                    }
                    author.IsPrimaryContributor = true;
                    authors.Add(author);
                    book.Author = authors;
                }

                if (book.Format == "Punktskrift") book.Format = "Tryckt punktskrift";

                if (book.Classification == null || book.Classification == "")
                {
                    book.Classification = books.Classification.FirstOrDefault();
                }

                if(books.AgeGroup != "Adult")
                {
                    book.AgeGroup = "Juvenile";
                }
                else
                {
                    book.AgeGroup = "Adult";
                }

                if (books.PublicationCategories.FirstOrDefault().Contains("Fiction"))
                {
                    book.Category = "Skönlitteratur";
                }
                else
                {
                    book.Category = "Facklitteratur";
                }
            }
        }
        XmlGenerator xmlGenerator = new XmlGenerator();

        var generatedFiles = new List<(string FileName, byte[] Content)>();
        var talkingBooks = booksProd.Where(b => b.LibraryId.StartsWith("C")).ToList();
        var brailleBooks = booksProd.Where(b => b.LibraryId.StartsWith("P")).ToList();

        log.LogInformation($"Found {talkingBooks.Count} talking books and {brailleBooks.Count} braille books.");

        var currentDate = DateTime.Now;
        string yearMonth = $"{currentDate:yyyy-MM}";

        // Helper function to generate file names
        string GenerateFileName(string prefix, string suffix) => $"{prefix}{suffix}-{yearMonth}.xml";

        // Generate XML files for talking books
        if (talkingBooks.Any())
        {
            var tbFileNames = new List<string>
        {
            GenerateFileName("nyf-tb", ""),
            GenerateFileName("nyf-tb-no-links", "-no-links"),
            GenerateFileName("nyf-tb-no-links-swedishonly", "-no-links-swedishonly")
        };

            foreach (var fileName in tbFileNames)
            {
                byte[] xmlContent = xmlGenerator.GenerateXmlContent(talkingBooks, fileName);
                generatedFiles.Add((fileName, xmlContent));
            }
        }

        // Generate XML files for braille books
        if (brailleBooks.Any())
        {
            var pbFileNames = new List<string>
        {
            GenerateFileName("nyf-punkt", ""),
            GenerateFileName("nyf-punkt-no-links", "-no-links"),
            GenerateFileName("nyf-punkt-no-links-swedishonly", "-no-links-swedishonly")
        };

            foreach (var fileName in pbFileNames)
            {
                byte[] xmlContent = xmlGenerator.GenerateXmlContent(brailleBooks, fileName);
                generatedFiles.Add((fileName, xmlContent));
            }
        }

        return generatedFiles;
    }

    public static async void SendEmailWithAttachments(string[] filePaths, string emailAddress)
    {
        var client = new MailjetClient("8fc6ccd381fcb4ec47dc2980c44a99de", "e3f4b681f0dad6c3aa27fa3702d71449");

        var message = new JObject
        {
            { "From", new JObject { { "Email", "erik.johansson@mtm.se" }, { "Name", "Erik Johansson" } } },
            { "To", new JArray { new JObject { { "Email", emailAddress }, { "Name", "Otto Ewald" } } } },
            { "Subject", "Genererade xml-filer" },
            { "TextPart", "Här kommer filerna." }
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
        { "Q", "Ekonomi och näringsvÃ¤sen" },
        { "R", "Idrott, lek och spel" },
        { "S", "MilitärvÃ¤sen" },
        { "T", "Matematik" },
        { "U", "Naturvetenskap" },
        { "V", "Medicin" },
        { "X", "Musikalier" },
        { "Y", "Musikinspelningar" },
        { "Z", "Tidningar" }
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


        return category ?? "Allmänt och blandat";
    }
}
