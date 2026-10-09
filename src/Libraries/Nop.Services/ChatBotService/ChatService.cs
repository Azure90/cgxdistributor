using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Nop.Services.Catalog;
using Nop.Data;
using ExCSS;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Globalization;
using Nop.Core.Domain.ProductEmbeddings;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using System.Net;
using System.Text.RegularExpressions;
using StackExchange.Redis;
using Org.BouncyCastle.Asn1.Crmf;

namespace Nop.Services.ChatBotService
{
    public class ChatService : IChatService
    {


        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;


        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        // private readonly HttpClient _httpClient = new();
        private readonly HashSet<string> _visitedUrls = new();
        private readonly string _baseDomain;
        private readonly string _outputFolder;
        private readonly IWebHostEnvironment _env;
        private readonly HashSet<string> _visited = new();
        private readonly ILocalizationService _localizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _openAiApiKey = "your-openai-api-key";

        // ======== CONFIG (tweak if needed) ========
        private const string DefaultEmbeddingModel = "text-embedding-3-small";
        private const int MaxTokensPerChunk = 7500; // ~8191 max for the model; keep a safe headroom
        private const int OverlapTokens = 150;      // slight overlap for better retrieval
        private const int MaxCharsPerChunk = MaxTokensPerChunk * 4; // 1 token ≈ ~4 chars (safe heuristic)
        private const int OverlapChars = OverlapTokens * 4;
        // ==========================================

        private readonly IRepository<ProductEmbeddings> _repository;
        private readonly ISettingService _settingService;
        public ChatService(IConfiguration configuration, IWebHostEnvironment env,
            ILocalizationService localizationService, ISettingService settingService, IHttpContextAccessor httpContextAccessor,

            IRepository<ProductEmbeddings> repository,
             IProductService productService,
            ICategoryService categoryService

            )
        {
            _repository = repository;
            _productService = productService;
            _categoryService = categoryService;
            _httpClient = new HttpClient();
            _settingService = settingService;
            //_apiKey = configuration["OpenAI:ApiKey"];
            // _apiKey = "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";
            _apiKey = _settingService.GetSetting("ChatBotApikey").Value;
            _localizationService = localizationService;

            // _apiKey = "dfsd";
            _env = env;
            // Start scraping once at the beginning for both sites
            //Task.Run(() => ScrapeSite("https://hansamed.net")).Wait();
            //Task.Run(() => ScrapeSite("https://citagenix.com")).Wait();
            _httpContextAccessor = httpContextAccessor;
            _httpClient = new HttpClient();

            // var dataFolder = Path.Combine(env.ContentRootPath, "scrapedData");
            //var jsonFiles = System.IO.Directory.GetFiles("D:\\github Projects\\citagenix\\src\\Presentation\\Nop.Web\\wwwroot\\scraped\\combined", "*.json");

            // var path = @"D:\Github Projects\citagenix\src\Presentation\Nop.Web\wwwroot\scraped\combined.json";
            // var json = System.IO.File.ReadAllText(path);

            //_websiteData = System.Text.Json.JsonSerializer.Deserialize<List<ContentEntry>>(json);

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
        // JSON Models





        //public async Task RebuildAllProductEmbeddingsAsync()
        //{
        //    var products = await _productService.SearchProductsAsync(showHidden: true, overridePublished: true);

        //    foreach (var product in products)
        //    {
        //        if (!product.Published)
        //            continue;

        //        // Get categories linked to this product
        //        var categories = await _categoryService.GetProductCategoriesByProductIdAsync(product.Id, true);

        //        // Convert categories to a readable string (comma separated)
        //        var categoryNames = new List<string>();
        //        foreach (var pc in categories)
        //        {
        //            var category = await _categoryService.GetCategoryByIdAsync(pc.CategoryId);
        //            if (category != null)
        //                categoryNames.Add(category.Name);
        //        }

        //        string categoryText = categoryNames.Any()
        //            ? $" Categories: {string.Join(", ", categoryNames)}"
        //            : "";

        //        // Build final text
        //        string textData = $"{product.Name} - {product.FullDescription ?? ""}{product.MetaKeywords ?? ""}";

        //        // Generate embedding
        //        var embeddingVector = await GenerateEmbeddingAsync(textData);
        //        string embeddingJson = JsonSerializer.Serialize(embeddingVector);

        //        // Save/Update in your DB
        //        await AddOrUpdateAsync(product.Id, textData, embeddingJson);
        //    }
        //}
        public List<string> truncatetable()
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            using (var conn = new SqlConnection(connstring))
            {
                conn.Open();
                var query = "TRUNCATE TABLE ProductEmbeddings";

                // Use Query<string> to ensure the result is a List<string>
                var result = conn.Query<string>(query);

                return result.ToList();
            }
        }
        public async Task<List<string>> CombinedTopicText()
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            using (var conn = new SqlConnection(connstring))
            {
                conn.Open();
                var query = @"
          SELECT CONCAT(Title, ' ', Body) AS CombinedTopicText
  FROM Topic
   WHERE Published = 1 
   AND (IncludeInFooterColumn1 = 1 OR IncludeInFooterColumn2 = 1 OR IncludeInFooterColumn3 = 1);
        ";



                // Use Query<string> to ensure the result is a List<string>
                var result = (await conn.QueryAsync<string>(query)).ToList();
                return result;
            }
        }
        public async Task<List<string>> CombinedFAQText()
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            using (var conn = new SqlConnection(connstring))
            {
                await conn.OpenAsync();

                var query = @"
            SELECT 
    
    STRING_AGG(CONCAT(ArticleTitle, ' ', ArticleBody), ' ') AS CombinedFAQText
FROM KnowledgeBaseArticle
WHERE Category IN (SELECT DISTINCT Category FROM KnowledgeBaseArticle)
GROUP BY Category;
        ";

                var result = (await conn.QueryAsync<string>(query)).ToList();
                return result;
            }
        }
        public async Task RebuildAllProductEmbeddingsAsync()
        {
            truncatetable();
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;

            using (var conn = new SqlConnection(connstring))
            {
                conn.Open();

                var query = @"
        SELECT 
            p.Id AS ProductId,  
            p.IsSample as IsSample,
            CONCAT(
                ISNULL(p.Name,''), ' ',
                ISNULL(p.FullDescription,''), ' ',
                ISNULL(p.MetaDescription,''), ' ',
                ISNULL(p.MetaKeywords,''), ' ',                                   
                ISNULL(kb.KnowledgeBaseData,''), ' ',
                ISNULL(childs.ChildDetails,'')
            ) AS EmbeddingText,
            CONCAT(
                ISNULL(p.Name,''), ' ',
                ISNULL(p.FullDescription,''), ' ',
                ISNULL(p.MetaDescription,''), ' ',
                ISNULL(p.MetaKeywords,''), ' ',                                   
                ISNULL(kb.KnowledgeBaseData,''), ' ',
                ISNULL(childs.ChildDetails,'')
            ) AS TextData,
            (SELECT TOP 1 Slug 
             FROM UrlRecord 
             WHERE EntityName='Product' AND EntityId=p.Id and IsActive=1
             ORDER BY Id) AS url
        FROM Product p
        OUTER APPLY (
            SELECT STUFF((
                SELECT ' ' + kb.ArticleTitle + ' ' + kb.ArticleBody
                FROM KnowledgeBaseArticle kb
                WHERE kb.Product = p.Name
                FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, ''
            ) AS KnowledgeBaseData
        ) kb
        OUTER APPLY (
            SELECT STRING_AGG(
                    CONCAT(
                        ISNULL(cp.Name,''), ' ',
                        ISNULL(cp.FullDescription,''), ' ',
                        'SKU:', ISNULL(cp.Sku,'')
                    ), ' || '
                ) AS ChildDetails
            FROM ProductAttributeCombination cac
            INNER JOIN Product cp 
                ON cp.Sku = cac.Sku
            WHERE cac.ProductId = p.Id
        ) childs
        WHERE p.Published = 1  
        AND NOT EXISTS (
            SELECT 1
            FROM ProductAttributeCombination cac
            WHERE cac.Sku = p.Sku
        )";



                // Use Query<string> to ensure the result is a List<string>
                var result = conn.Query<embeddingdata>(query).ToList();
                var CombinedFAQ = CombinedFAQText();
                var CombinedTopic = CombinedTopicText();
                var embeddingdata = _repository.Table.ToList();

                foreach (var row in result)
                {
                    var existing = embeddingdata.Any(pe => pe.ProductId == row.ProductId && pe.TextData == row.TextData);
                    if (!existing)
                    {
                        // Call OpenAI embeddings API (your wrapper service)
                        var embedding = GenerateEmbeddingAsync(row.EmbeddingText);
                        string embeddingJson = JsonSerializer.Serialize(embedding);
                        // Map to entity
                        var entity = new ProductEmbeddings
                        {
                            ProductId = row.ProductId,
                            IsSample = row.IsSample,
                            TextData = row.TextData,
                            Embedding = embeddingJson,
                            Url = row.Url
                        };
                        AddOrUpdateAsync(row.ProductId, row.IsSample, row.TextData, embeddingJson, row.Url);
                        // Insert via repository
                        // await _repository.InsertAsync(entity);
                    }
                }
                foreach (var row in CombinedFAQ.Result)
                {
                    try
                    {
                        // Call OpenAI embeddings API (your wrapper service)
                        var embedding = GenerateEmbeddingForLargeTextAsync(row);
                        string embeddingJson = JsonSerializer.Serialize(embedding);
                        // Map to entity
                        var entity = new ProductEmbeddings
                        {
                            ProductId = 0,
                            IsSample = 0,
                            TextData = "FAQ",
                            Embedding = embeddingJson,
                            Url = ""
                        };
                        AddOrUpdateAsync(0, 0, row, embeddingJson, "");
                        // Insert via repository
                        // await _repository.InsertAsync(entity);
                    }
                    catch (Exception ex)
                    {

                    }

                }
                foreach (var row in CombinedTopic.Result)
                {
                    try
                    {
                        // Call OpenAI embeddings API (your wrapper service)
                        var embedding = GenerateEmbeddingForLargeTextAsync(row);
                        string embeddingJson = JsonSerializer.Serialize(embedding);
                        // Map to entity
                        var entity = new ProductEmbeddings
                        {
                            ProductId = 0,
                            IsSample = 0,
                            TextData = "TopicPages",
                            Embedding = embeddingJson,
                            Url = ""
                        };
                        AddOrUpdateAsync(0, 0, row, embeddingJson, "");
                        // Insert via repository
                        // await _repository.InsertAsync(entity);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
        }


        /// <summary>
        /// Call OpenAI embeddings API
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync1(string text, string model = "text-embedding-3-small")
        {
            var requestBody = new
            {
                model = model,
                input = text
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Extract embedding array directly
            using var doc = JsonDocument.Parse(jsonResponse);
            var embeddingJson = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");

            var embedding = new float[embeddingJson.GetArrayLength()];
            for (int i = 0; i < embedding.Length; i++)
                embedding[i] = embeddingJson[i].GetSingle();

            return embedding; // ✅ plain float array
        }




        /// <summary>
        /// Clean small/medium text and generate a single embedding (float[]).
        /// Throws if text is too long for a single request — for big inputs use GenerateEmbeddingsForTextAsync / IngestLargeTextAsync.
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text, string model = DefaultEmbeddingModel)
        {
            var cleaned = CleanText(text);
            if (string.IsNullOrWhiteSpace(cleaned))
                return Array.Empty<float>();

            if (cleaned.Length > MaxCharsPerChunk)
                throw new InvalidOperationException("Text too large for single embedding call. Use GenerateEmbeddingsForTextAsync / IngestLargeTextAsync.");

            var requestBody = new { model, input = cleaned };
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            for (int attempt = 0; attempt < 3; attempt++)
            {
                using var res = await _httpClient.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(body);
                    var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
                    var vec = new float[arr.GetArrayLength()];
                    for (int i = 0; i < vec.Length; i++)
                        vec[i] = arr[i].GetSingle();
                    return vec; // ✅ plain float[]
                }

                // retry on 429 or 5xx
                if ((int)res.StatusCode == 429 || (int)res.StatusCode >= 500)
                {
                    await Task.Delay(250 * (int)Math.Pow(2, attempt));
                    continue;
                }

                throw new InvalidOperationException($"OpenAI error {(int)res.StatusCode}: {body}");
            }

            throw new InvalidOperationException("OpenAI embeddings request failed after retries.");
        }

        /// <summary>
        /// For big Topic/FAQ pages: clean, chunk and embed each chunk.
        /// Returns (chunkText, vector) for each chunk so you can insert rows as-is (ProductId NULL for topics/faqs).
        /// </summary>
        public async Task<float[]> GenerateEmbeddingForLargeTextAsync(
     string rawHtmlOrText,
     string model = DefaultEmbeddingModel,
     int maxTokens = MaxTokensPerChunk,
     int overlapTokens = OverlapTokens)
        {
            var cleaned = CleanText(rawHtmlOrText);
            var chunks = ChunkByApproxTokens(cleaned, maxTokens, overlapTokens).ToList();

            var allVectors = new List<float[]>();

            foreach (var c in chunks)
            {
                var vec = await GenerateEmbeddingAsync(c, model);
                allVectors.Add(vec);
            }

            if (allVectors.Count == 0)
                return Array.Empty<float>();

            // ✅ average all chunk vectors into one
            int dim = allVectors[0].Length;
            var meanVec = new float[dim];
            foreach (var v in allVectors)
                for (int i = 0; i < dim; i++)
                    meanVec[i] += v[i];

            for (int i = 0; i < dim; i++)
                meanVec[i] /= allVectors.Count;

            return meanVec;
        }

        /// <summary>
        /// One-call ingestion for large texts (Topics / FAQs):
        /// - Cleans and chunks the text
        /// - Generates embeddings
        /// - Invokes your save delegate per chunk with the exact columns you already use
        /// </summary>
        /// <param name="rawHtmlOrText">Source content (HTML or plain text)</param>
        /// <param name="url">Destination URL to store in [Url]</param>
        /// <param name="productId">Use null for topics/faqs</param>
        /// <param name="saveRowAsync">
        /// Your DB save, e.g.:
        /// async row => await _repository.InsertAsync(new ProductEmbeddings {
        ///     ProductId = row.ProductId, TextData = row.TextData, Embedding = row.EmbeddingJson, Url = row.Url, CreatedOn = row.CreatedOn
        /// })
        /// </param>


        // ==================== Utilities ====================

        /// <summary>Strip HTML, decode entities, remove control chars, normalize whitespace.</summary>
        public static string CleanText(string htmlOrText)
        {
            if (string.IsNullOrWhiteSpace(htmlOrText))
                return string.Empty;

            var s = WebUtility.HtmlDecode(htmlOrText);
            s = Regex.Replace(s, "<[^>]+>", " "); // strip tags
            s = new string(s.Where(c => c == '\r' || c == '\n' || c == '\t' || !char.IsControl(c)).ToArray());
            s = Regex.Replace(s, @"[ \t]+", " ");
            s = Regex.Replace(s, @"\n\s*\n\s*", "\n\n").Trim();
            return s;
        }

        /// <summary>
        /// Rough token-aware chunking (≈ 4 chars ≈ 1 token). Tries to end on sentence boundaries.
        /// </summary>
        public static IEnumerable<string> ChunkByApproxTokens(string text, int maxTokens, int overlapTokens)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            int maxChars = maxTokens * 4;
            int overlap = overlapTokens * 4;

            int start = 0;
            while (start < text.Length)
            {
                int len = Math.Min(maxChars, text.Length - start);
                int end = start + len;

                if (end < text.Length)
                {
                    var window = text.AsSpan(start, len).ToString();
                    int lastStop = window.LastIndexOfAny(new[] { '.', '!', '?', '\n' });
                    if (lastStop > maxChars * 0.6)
                        end = start + lastStop + 1;
                }

                var chunk = text.Substring(start, end - start).Trim();
                if (!string.IsNullOrWhiteSpace(chunk))
                    yield return chunk;

                if (end >= text.Length)
                    break;
                start = Math.Max(end - overlap, start + 1);
            }
        }









        /// <summary>
        /// Add or update embedding in database
        /// </summary>
        public async Task AddOrUpdateAsync(int productId, int IsSample, string textData, string embeddingJson, string url)
        {
            // var existing = await _repository.Table.FirstOrDefaultAsync(pe => pe.ProductId == productId);
            //   var query = _repository.Table.ToList();         


            var newEntry = new ProductEmbeddings
            {
                ProductId = productId,
                IsSample = IsSample,
                TextData = textData,
                Embedding = embeddingJson,
                CreatedOn = DateTime.UtcNow,
                Url = url
            };
            await _repository.InsertAsync(newEntry);

        }

        /// <summary>
        /// Example: return all embeddings (for testing)
        /// </summary>
        public async Task<List<ProductEmbeddings>> GetAllEmbeddingsAsync()
        {
            return await _repository.Table.ToListAsync();
        }


        private double[] ParseEmbedding(string embeddingJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(embeddingJson);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Case 1: plain JSON array
                    return root.EnumerateArray().Select(x => x.GetDouble()).ToArray();
                }
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("Result", out var result))
                {
                    // Case 2: object with "Result" array
                    return result.EnumerateArray().Select(x => x.GetDouble()).ToArray();
                }
            }
            catch
            {
                // Case 3: fallback plain string
                return embeddingJson
                    .Trim()
                    .TrimStart('[').TrimEnd(']')
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                    .ToArray();
            }

            return Array.Empty<double>();
        }
        public async Task<string> GetAnswerAsync(string userQuestion, string info)
        {
            string answer = "";
            try
            {
                // 0️⃣ Detect the user language using GPT
                var (detectedLanguage, translatedQuestion) = await DetectAndTranslateAsync(userQuestion);

                // 1️⃣ Translate user question to English for embeddings (if needed)
                var questionForEmbedding = detectedLanguage != "en"
                    ? translatedQuestion
                    : userQuestion;

                // 1️⃣ Get embedding for the user question from OpenAI
                var requestBody = new
                {
                    model = "text-embedding-3-small",
                    input = questionForEmbedding
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/embeddings",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                );

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var questionVector = doc.RootElement.GetProperty("data")[0]
                                        .GetProperty("embedding")
                                        .EnumerateArray()
                                        .Select(x => x.GetDouble())
                                        .ToArray();


                // 2️⃣ Load all embeddings from SQL
                var allEmbeddings = await _repository.Table.ToListAsync();
                if (info == "2")
                {
                    allEmbeddings = allEmbeddings.Where(x => x.ProductId == 0).ToList();
                }
                else
                {
                    allEmbeddings = allEmbeddings.Where(x => x.ProductId != 0).ToList();
                }

                // 3️⃣ Compute similarity with each product
                var topMatches = allEmbeddings
                .Select(p =>
                {
                    double[] productVector;

                    try
                    {
                        // Try parsing as JSON
                        using var doc = JsonDocument.Parse(p.Embedding);
                        var root = doc.RootElement;

                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            // Case 1: plain array
                            productVector = root.EnumerateArray()
                                .Select(x => x.GetDouble())
                                .ToArray();
                        }
                        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("Result", out var result))
                        {
                            // Case 2: object with Result array
                            productVector = result.EnumerateArray()
                                .Select(x => x.GetDouble())
                                .ToArray();
                        }
                        else
                        {
                            // Case 3: fallback to comma-separated string
                            productVector = p.Embedding
                                .Trim()
                                .TrimStart('[').TrimEnd(']')
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                                .ToArray();
                        }
                    }
                    catch
                    {
                        // Fallback: comma-separated string
                        productVector = p.Embedding
                            .Trim()
                            .TrimStart('[').TrimEnd(']')
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                            .ToArray();
                    }

                    return new
                    {
                        Product = p,
                        Similarity = CosineSimilarity(questionVector, productVector)
                    };
                })
                  .OrderByDescending(x => x.Similarity)
                  .Take(5)
                  .ToList();

                // 4️⃣ Build context text for GPT
                //  var contextText = string.Join("\n\n", topMatches.Select(x => x.Product.TextData ));
                //var contextText = string.Join("\n\n", topMatches.Select(x => $"{x.Product.TextData}\nProduct Link: {x.Product.Url}"));
                var contextText = string.Join("\n\n", topMatches.Select(x => $"{x.Product.TextData}\nProduct Link: {(x.Product.IsSample == 0 ? $"ProductDetailPage?pid={x.Product.ProductId}" : x.Product.Url)}"));

                var chatJson = "";
                // 5️⃣ Call ChatGPT with context + user question
                if (info != "2")
                {
                    var chatRequest = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[]
                        {
                            new { role = "system", content = $@"
                              You are a helpful multilanguage assistant that ONLY outputs responses in clean HTML format for a chatbot UI.
     
                                 Follow these rules carefully :
                                 1. It is mandatory respond to you to reply in only in the {detectedLanguage} language. it is restrict for you to use any other language, under any circumstance.
                                 2. You MUST always begin the response with a short, clear, step-by-step introduction that directly addresses the user’s question (for example: if the question is about early implant procedures, explain the steps of the procedure first before mentioning any products).
                                 3. After the introduction, always write: '<br><br>Recommended products:<br><br>' before listing products.
                                 4. Present the recommended products as a numbered list using <ol> and <li> tags.
                                 5. Each product must be inside its own <li> tag.
                                 * **Structure:** Organize content section-by-section with clear HTML headings. Use <h2> for main topics and <h3> for sub-topics. Choose relevant topics (e.g., Returns, Warranties, Setup, Troubleshooting) based on the user's query.
                                 * **Formatting:** Use proper HTML tags to improve readability.
                                     * <ul> and <li> for bullet points.
                                     * <ol> and <li> for numbered steps.
                                     * <strong> for emphasis on keywords.
                                     * <em> for softer emphasis.
                                 * **Content:**
                                     * Break down text into short, easy-to-read paragraphs.
                                     * Include short, helpful examples to illustrate how a process works (e.g., ""how returns work,"" ""when a restocking fee applies,"" ""a troubleshooting example"").
                                 * **Final Summary :** At the very end of the response, include a <div class='summary'> with a concise, bullet-point summary of the most important points from the guide.
                                 **Important — You MUST NOT invent or suggest any product that is not in the provided product list.   
                                 **Important — Do not create or assume product names, details, or links that are not explicitly included in the provided list.  
                                 **Important — Do not add your own suggestions or ideas beyond the provided data.
                                 **Important — Inline CSS requirement (must follow exactly):**
                                 1. **All HTML elements you output MUST include inline style attributes only.** Do NOT include a <style> tag, <link> tag, or reference external CSS. Do NOT rely on site/global styles or class-based styles.
                                 2. Use minimal, reset-like inline styles to avoid interfering with the host page. Example inline attributes to apply (these exact properties should be included on the respective elements):
                                    - <h2 style=""margin:0 0 8px 0; padding:0; font-size:18px; line-height:1.3; font-weight:600; font-family:inherit; color:inherit;"">
                                    - <h3 style=""margin:8px 0; padding:0; font-size:15px; line-height:1.3; font-weight:500; font-family:inherit; color:inherit;"">
                                    - <p style=""margin:0 0 8px 0; padding:0; font-size:14px; line-height:1.4; font-family:inherit; color:inherit;"">
                                    - <ul style=""margin:0 0 8px 20px; padding:0; font-size:14px; line-height:1.4; font-family:inherit; color:inherit;"">
                                    - <li style=""margin:4px 0; padding:0;"">
                                    - <ol style=""margin:0 0 8px 20px; padding:0; font-size:14px; line-height:1.4; font-family:inherit; color:inherit;"">
                                    - <strong style=""font-weight:600;"" />
                                    - <em style=""font-style:italic;"" />
                                    - <a style=""text-decoration:underline; cursor:pointer;"" href=""#"" />
                                    - <div class='summary' style=""margin:12px 0 0 0; padding:10px; border-radius:6px; font-size:13px; line-height:1.4; font-family:inherit; color:inherit; box-shadow:none;"">
                                 6. Use <strong>...</strong> to highlight product names.
                                 7. After the product name, insert a <br> tag, then write the product description.
                                 8. At the end of the description, insert a <br> tag followed by the product link in this exact format:
                                    <a href=""PRODUCT_URL"" target=""_blank"" style=""text-decoration: underline; color: black;"" onmouseover=""this.style.color='blue';"" onmouseout=""this.style.color='black';"">PRODUCT_NAME Product Link</a><br><br>
                                    Example:
                                    <a href=""https://marketing.citagenix.com/neoderm-adm"" target=""_blank"" style=""text-decoration: underline; color: black;"" onmouseover=""this.style.color='blue';"" onmouseout=""this.style.color='black';"">Neoderm ADM Product Link</a><br><br>
                                    Ensure the product URLs always start with https://marketing.citagenix.com/ followed by the correct path.
                                 9. Use a single <br> tag for line breaks as shown.
                                 10. Ensure the output is consistent and properly formatted, even for long or complex inputs.
                                 11. Do NOT output plain text or regular paragraphs. Only use valid HTML inside the <span class=""bubble""> tag with lists as specified.
                                 12. The introduction section is MANDATORY. Never start directly with products. If the user’s question is about a medical or dental procedure, always explain the procedure steps first in simple format, then move on to products.
                                 13. Always include the product link at the end of each description in the specified format, without missing or altering any part.
                                 14. For all questions related to medical topics, always provide an appropriate response using available product information or, if specific details are not available, provide answers based on your limited knowledge while ensuring it is helpful and accurate.
                                 15. For any question that is NOT related to medical topics, respond ONLY with the following message inside the <span class=""bubble""> tag, formatted with a <br> tag for readability:
                                 16. It is restricted to you not show any images in the answer.
                                 17. All parts of the response — including headings, product names, links, summaries, and descriptions — must be written strictly in the user's ({detectedLanguage}) language, never mix languages.
                                 <br>" + Convert.ToString(_localizationService.GetResourceAsync("Chatbot.ChatBotprompt").Result) + "<br>"
                            },
                            new { role = "user", content = $"Context:\n{contextText}\n\nQuestion: {userQuestion}" }
                        }
                    };

                    var chatResponse = await _httpClient.PostAsync(
                        "https://api.openai.com/v1/chat/completions",
                        new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json"));

                    chatResponse.EnsureSuccessStatusCode();
                    chatJson = await chatResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    var chatRequest1 = new
                    {
                        model = "gpt-3.5-turbo",
                        messages = new[]
                        {
                            new { role = "system",  content = $@"
                            You are an multilanguage expert chatbot UI content creator. Your task is to rewrite user-provided information into a user-friendly guide.
                            This guide will be displayed in a chatbot UI that shows a summary with a technical guide strictly in user's {detectedLanguage} language.

                            **Core Directives:**
                            * **Audience:** Explain all technical details in plain, simple, and jargon-free language.
                            * **Language:** The final response must be in the same language as the user's question.
                            * **Structure:** Organize content section-by-section with clear HTML headings. Use <h2> for main topics and <h3> for sub-topics. Choose relevant topics (e.g., Returns, Warranties, Setup, Troubleshooting) based on the user's query.
                            * **Formatting:** Use proper HTML tags to improve readability.
                                * <ul> and <li> for bullet points.
                                * <ol> and <li> for numbered steps.
                                * <strong> for emphasis on keywords.
                                * <em> for softer emphasis.
                            * **Content:**
                                * Break down text into short, easy-to-read paragraphs.
                                * Include short, helpful examples to illustrate how a process works (e.g., ""how returns work,"" ""when a restocking fee applies,"" ""a troubleshooting example"").
                            * **Final Summary:** At the very end of the response, include a <div class='summary'> with a concise, bullet-point summary of the most important points from the guide.

                            **Important — Inline CSS requirement (must follow exactly):**
                            1. **All HTML elements you output MUST include inline style attributes only.** Do NOT include a <style> tag, <link> tag, or reference external CSS. Do NOT rely on site/global styles or class-based styles.
                            2. Use minimal, reset-like inline styles to avoid interfering with the host page. Example inline attributes to apply (these exact properties should be included on the respective elements):
                               - <h2 style=""margin:0 0 8px 0; padding:0; font-size:18px; line-height:1.3; font-weight:600; font-family:inherit; color:inherit;"">
                               - <h3 style=""margin:8px 0; padding:0; font-size:15px; line-height:1.3; font-weight:500; font-family:inherit; color:inherit;"">
                               - <p style=""margin:0 0 8px 0; padding:0; font-size:14px; line-height:1.4; font-family:inherit; color:inherit;"">
                               - <ul style=""margin:0 0 8px 20px; padding:0; font-size:14px; line-height:1.4; font-family:inherit; color:inherit;"">
                               - <li style=""margin:4px 0; padding:0;"">
                               - <ol style=""margin:0 0 8px 20px; padding:0; font-size:14px; line-height:1.4; font-family:inherit; color:inherit;"">
                               - <strong style=""font-weight:600;"" />
                               - <em style=""font-style:italic;"" />
                               - <a style=""text-decoration:underline; cursor:pointer;"" href=""#"" />
                               - <div class='summary' style=""margin:12px 0 0 0; padding:10px; border-radius:6px; font-size:13px; line-height:1.4; font-family:inherit; color:inherit; box-shadow:none;"">
                            3. When generating the HTML, apply the inline styles exactly (or semantically equivalent) on every element you emit (h2, h3, p, ul, ol, li, strong, em, a, pre, code, table, tr, td, div.summary, etc.).
                            4. Avoid relying on classes or IDs for visual styling. You may include semantic class names for identification but **visual style must be inline**.
                            5. Keep the HTML accessible and simple — no scripts, no inline event handlers (onclick), and no external resources.
                            6. If the provided context lacks information required to answer the user's question, reply exactly with: <p style=""margin:0; padding:0;"">" +Convert.ToString(_localizationService.GetResourceAsync("Chatbot.ChatBotprompt").Result)   +" </p>" +
                            "" +
                            "7. It is restricted to you not show any images in the answer."
                            },
                            new { role = "user", content = $"Context:\n{contextText}\n\nQuestion: {userQuestion}" }
                        }
                    };

                    var chatResponse = await _httpClient.PostAsync(
                        "https://api.openai.com/v1/chat/completions",
                        new StringContent(JsonSerializer.Serialize(chatRequest1), Encoding.UTF8, "application/json"));

                    chatResponse.EnsureSuccessStatusCode();
                    chatJson = await chatResponse.Content.ReadAsStringAsync();
                }


                using var chatDoc = JsonDocument.Parse(chatJson);
                answer = chatDoc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error in GetAnswerAsync: {ex.Message}");
                answer = Convert.ToString(_localizationService.GetResourceAsync("Chatbot.ChatBotprompt").Result);
            }

            return answer ?? Convert.ToString(_localizationService.GetResourceAsync("Chatbot.ChatBotprompt").Result);
        }

        public async Task<(string detectedLanguage, string translatedText)> DetectAndTranslateAsync(string text)
        {
            try
            {
                // 1️⃣ Ask GPT to detect language and translate if necessary
                var request = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new
                        {
                            role = "system", @content =
                          "You are a translation engine. Detect the input language and also translate it into English. " +
                          "Respond strictly in JSON format like this: { \"language\": \"<input language>\", \"text\": \"<english text>\" }"
                        },

                        new { role = "user", content = text }
                    }
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString()
                                 .Trim();

                // 2️⃣ Parse GPT JSON response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                if (result != null && result.ContainsKey("language") && result.ContainsKey("text"))
                {
                    return (result["language"], result["text"]);
                }

                // fallback: if parsing fails, assume English
                return ("en", text);
            }
            catch
            {
                // fallback: return original text and English as default
                return ("en", text);
            }
        }

        private static double CosineSimilarity(double[] vectorA, double[] vectorB)
        {
            double dot = 0;
            double magA = 0;
            double magB = 0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dot += vectorA[i] * vectorB[i];
                magA += vectorA[i] * vectorA[i];
                magB += vectorB[i] * vectorB[i];
            }

            if (magA == 0 || magB == 0)
                return 0;

            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }
        public class embeddingdata
        {
            public int ProductId { get; set; }
            public int IsSample { get; set; }
            public string TextData { get; set; }
            public string EmbeddingText { get; set; }
            public string Url { get; set; }
            public string CombinedTopicText { get; set; }
            public string CombinedFAQText { get; set; }
        }

    }
}
