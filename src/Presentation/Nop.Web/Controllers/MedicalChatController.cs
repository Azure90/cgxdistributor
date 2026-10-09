using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NUglify.Html;
using System.Text.RegularExpressions;
using Nop.Services.ChatBotService;
using Microsoft.AspNetCore.Hosting;
using Nop.Services.KnowledgeBaseArticle;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.Topics;
using Nop.Services.Seo;
using DocumentFormat.OpenXml.ExtendedProperties;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.ChatBotHistory;
using Nop.Core.Domain.ChatBotHistory;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using Microsoft.Data.SqlClient;
using Dapper;

using Nop.Data;

using System.Data;
using Microsoft.AspNetCore.Authorization;


namespace Nop.Web.Controllers
{
    public class MedicalChatController : BasePublicController
    {

        private static List<ChatMessage> _chatHistory = new();
        private readonly ChatService _ChatService;
        private readonly List<ScrapedContent> _allPages = new();
        private readonly HashSet<string> _visitedUrls = new();
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient = new();
        private readonly IStoreContext _storeContext;
        private readonly IKnowledgeBaseArticleService _KnowledgeBaseArticleService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        private readonly ITopicService _topicService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly Ichatbothistory _chatbothistory;
        private readonly string _apiKey;
        private readonly string _indexPath;
        //private static readonly LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public MedicalChatController(ChatService chatService, IWorkContext workContext,
            IStoreContext storeContext, ITopicService topicService, ISettingService settingService, ILocalizationService localizationService, ICustomerService customerService, IWebHostEnvironment env, IKnowledgeBaseArticleService knowledgeBaseArticleService,
            IUrlRecordService urlRecordService, Ichatbothistory chatbothistory)
        {
            _ChatService = chatService;
            _env = env;
            _KnowledgeBaseArticleService = knowledgeBaseArticleService;
            _workContext = workContext;
            _customerService = customerService;
            _topicService = topicService;
            _storeContext = storeContext;
            _urlRecordService = urlRecordService;
            _settingService = settingService;
            _localizationService = localizationService;
            _chatbothistory = chatbothistory;
            _indexPath = Path.Combine(env.ContentRootPath, "lucene_index");
            _apiKey = _settingService.GetSetting("ChatBotApikey").Value;
        }

        public class ScrapedContent
        {
            public string Url { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
        }
        public class ChatMessage
        {
            public string UserMessage { get; set; }
            public string BotResponse { get; set; }
            public string imagepath { get; set; }
            public int Numberofquestion { get; set; }
            public DateTime? Timestamp { get; set; } = DateTime.Now;
        }



        [HttpGet]
        public IActionResult Index()
        {
            _chatHistory = new List<ChatMessage>();

            return View(_chatHistory);
        }
        public ChatMessage GetChatModel()
        {
            return new ChatMessage
            {
                UserMessage = "Medical Chat",
                BotResponse = "Hello! How can we help you today?"
            };
        }
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> Getthecustomanswer(string request)
        {
            var languageid = (await _workContext.GetWorkingLanguageAsync()).Id;
            var customerrole = _workContext.GetCurrentCustomerAsync().Result;
            var GetallRoles = _customerService.GetCustomerRolesAsync(customerrole).Result.Where(d => d.Name == "CAD B2C"
               || d.Name == "USD B2C"
               || d.Name == "USD B2B INTL"
               || d.Name == "CAD B2C HS"
               || d.Name == "USD B2B ST"
               || d.Name == "USD B2B DS"
               || d.Name == "CAD B2B ST"
               || d.Name == "CAD B2B DS"
               ).FirstOrDefault();
            var data = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().ToList();
            var result4 = new List<Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle>();
            foreach (var j in data)
            {
                var splitedroles = j.CustomerRoles.Split(",");
                foreach (var k in splitedroles)
                {
                    if (k.Trim() == GetallRoles.Name)
                    {
                        result4.Add(j);
                    }
                }
            }
            //var result4 = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().Where(x => x.CustomerRoles == GetallRoles.Name).ToList();
            var result = new List<KeyValuePair<string, string>>();
            if (result4.Count > 0)
            {
                foreach (var item in result4)
                {
                    result.Add(new KeyValuePair<string, string>(item.Id.ToString(), item.ArticleTitle));
                }
                var result3 = result.Where(s => s.Value.ToLower().Contains(request.ToLower())).Select(w => w).ToList();
                return Json(result3);
            }
            return Json(0);
        }
        [IgnoreAntiforgeryToken]
        public string GetData(int id, string request)
        {
            var finaldata = "";
            try
            {
                var store = _storeContext.GetCurrentStoreAsync().Result;
                var getQuestionbyid = new Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle();
                if (id == 0)
                {
                    getQuestionbyid = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().Where(d => d.ArticleTitle.Contains(request)).FirstOrDefault();
                }
                else
                {
                    getQuestionbyid = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().Where(d => d.Id == id).FirstOrDefault();
                }
                var topicdata = _topicService.GetAllTopicsAsync(store.Id).Result
                           .Where(x => !string.IsNullOrEmpty(x.Body))
                           .ToList();

                var finalTopicdata = topicdata
                    .Where(x => Regex.Replace(x.Body, "<.*?>", string.Empty)
                                     .Contains(request, StringComparison.OrdinalIgnoreCase))
                    .ToList();


                if (finalTopicdata.Count() > 0)
                {
                    foreach (var i in finalTopicdata)
                    {

                        var urldata = _urlRecordService.GetActiveSlugAsync(i.Id, "Topic", 0).Result;
                        // Split body into words, take first 15, then join back
                        var bodyPreview = string.Join(" ", i.Body.Split(' ').Take(25));
                        var cleanedHtml = Regex.Replace(bodyPreview, "<img[^>]*>", "", RegexOptions.IgnoreCase);
                        bodyPreview = cleanedHtml.Replace("h1", "h4");

                        finaldata = finaldata + i.Title
                           + "<br>"
                           + bodyPreview
                           + "<br>..for More Information Click on the Given Link <a style=\"text-decoration: none; color: #007bff; font-weight: bold; transition: color 0.3s ease;\" href='https://citagenix.com/" + urldata + "'>"
                           + "Click here"
                           + "</a>";

                    }
                }
                if (getQuestionbyid != null)
                {
                    if (finaldata != "")
                    {
                        finaldata = finaldata + "<br>" + getQuestionbyid.ArticleBody;
                    }
                    else
                    {
                        finaldata = getQuestionbyid.ArticleBody;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            //return Json(new { PageNameId = getQuestionbyid.Category, pagesubcategory=getQuestionbyid.SubCategory.Replace(" ",""), SubtitleId = getQuestionbyid.Product.Replace(" ",""), Qid = getQuestionbyid.Id });
            return finaldata;

        }


        //[IgnoreAntiforgeryToken]
        //public string GetData(int id, string request)
        //{
        //    var finaldata = "";
        //    try
        //    {
        //        var store = _storeContext.GetCurrentStoreAsync().Result;
        //        var getQuestionbyid = new Nop.Core.Domain.KnowledgeBaseArticle.KnowledgeBaseArticle();
        //        if (id == 0)
        //        {
        //            getQuestionbyid = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().Where(d => d.ArticleTitle.Contains(request)).FirstOrDefault();
        //        }
        //        else
        //        {
        //            getQuestionbyid = _KnowledgeBaseArticleService.GetAllKnowledgeBaseArticle().Where(d => d.Id == id).FirstOrDefault();
        //        }
        //        var topicdata = _topicService.GetAllTopicsAsync(store.Id).Result
        //                   .Where(x => !string.IsNullOrEmpty(x.Body))
        //                   .ToList();

        //        var finalTopicdata = topicdata
        //            .Where(x => Regex.Replace(x.Body, "<.*?>", string.Empty)
        //                             .Contains(request, StringComparison.OrdinalIgnoreCase) )
        //            .ToList();


        //        if (finalTopicdata.Count() > 0)
        //        {
        //            foreach (var i in finalTopicdata)
        //            {

        //                var urldata = _urlRecordService.GetActiveSlugAsync(i.Id, "Topic", 0).Result;
        //                // Split body into words, take first 15, then join back
        //                var bodyPreview = string.Join(" ", i.Body.Split(' ').Take(25));
        //                var cleanedHtml = Regex.Replace(bodyPreview, "<img[^>]*>", "", RegexOptions.IgnoreCase);
        //                bodyPreview = cleanedHtml.Replace("h1", "h4");

        //                finaldata = finaldata + i.Title
        //                   + "<br>"
        //                   + bodyPreview
        //                   + "<br>..for More Information Click on the Given Link <a style=\"text-decoration: none; color: #007bff; font-weight: bold; transition: color 0.3s ease;\" href='https://citagenix.com/" + urldata + "'>"
        //                   + "Click here"
        //                   + "</a>";

        //            }
        //        }
        //        if (getQuestionbyid != null)
        //        {
        //            if (finaldata != "")
        //            {
        //                finaldata = finaldata + "<br>" + getQuestionbyid.ArticleBody;
        //            }
        //            else
        //            {
        //                finaldata = getQuestionbyid.ArticleBody;
        //            }
        //        }
        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //    //return Json(new { PageNameId = getQuestionbyid.Category, pagesubcategory=getQuestionbyid.SubCategory.Replace(" ",""), SubtitleId = getQuestionbyid.Product.Replace(" ",""), Qid = getQuestionbyid.Id });
        //    return finaldata;

        //}

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Ask(string request, string info, string id)
        {
            // RebuildEmbeddings();
            var finaldata = "";
            var answer = "";
            var planlimit = "NotOver";
            var custid = _workContext.GetCurrentCustomerAsync().Result.Id;
            var isai = 0;

            bool isRegistered = true;
            var chatBotPrompt = "Chatbot.ChatBotprompt";
            var chatBotWarning = "Chatbot.chatbotfreeaccess";
            var chatbBotNumberOfQuestions = "Chatbot.ChatBotnumberofquestions";

            if (info == "2")
            {
                answer = await _ChatService.GetAnswerAsync(request, info);
                isai = 1;
                _chatHistory.Add(new ChatMessage
                {
                    UserMessage = request,
                    BotResponse = answer,
                    imagepath = Convert.ToString(_localizationService.GetResourceAsync("Chatbot.Chatbotimage").Result)
                });
            }
            else
            {
                answer = await _ChatService.GetAnswerAsync(request, info);
                isai = 1;
                _chatHistory.Add(new ChatMessage
                {
                    UserMessage = request,
                    BotResponse = answer,
                    imagepath = Convert.ToString(_localizationService.GetResourceAsync("Chatbot.Chatbotimage").Result)
                });
            }


            // Chat Bot Before/After Login Process
            if (_workContext.GetCurrentCustomerAsync().Result.Email == null)
            {
                chatbBotNumberOfQuestions = "Chatbot.ChatBotnumberofquestionsBefore";
                chatBotWarning = "Chatbot.chatbotfreeaccessBefore";
                isRegistered = false;
            }

            ViewBag.chatbotwarning = Convert.ToString(_localizationService.GetResourceAsync(chatBotWarning).Result);
            var answertext = Convert.ToString(_localizationService.GetResourceAsync(chatBotPrompt).Result);
            //if (answer != Convert.ToString(_localizationService.GetResourceAsync("Chatbot.ChatBotprompt").Result))
            if (!StripHtml(answer).Equals(StripHtml(answertext), StringComparison.OrdinalIgnoreCase))
            {
                Nop.Core.Domain.ChatBotHistory.chatbothistory chatbothistory = new Nop.Core.Domain.ChatBotHistory.chatbothistory();
                chatbothistory.CustID = custid;
                chatbothistory.question = request;
                chatbothistory.answer = answer;
                chatbothistory.IsAI = Convert.ToBoolean(isai);
                chatbothistory.DateandTime = DateTime.Now;
                chatbothistory.Isregistered = isRegistered;
                _chatbothistory.InsertChatbothistory(chatbothistory);
            }

            var chatbotlimit = Convert.ToInt32(_localizationService.GetResourceAsync(chatbBotNumberOfQuestions).Result);
            var questionNumber = _chatbothistory.GetAllChatbothistory().Where(x => x.CustID == custid && x.IsAI == true).Select(y => y.question).Count();
            if (questionNumber >= chatbotlimit /*&& info != "2"*/)
            {
                return Json(new { answer = answer, image = Convert.ToString(_localizationService.GetResourceAsync("Chatbot.Chatbotimage").Result), planlimit = "over" });
            }

            return Json(new { answer = answer, image = Convert.ToString(_localizationService.GetResourceAsync("Chatbot.Chatbotimage").Result), planlimit = planlimit });
        }
        public ActionResult chatbottimeout()
        {
            var allchat = _chatbothistory.GetAllChatbothistory();
            var custid = _workContext.GetCurrentCustomerAsync().Result.Id;

            var lastQuestionTime = allchat
                .Where(x => x.CustID == custid && x.IsAI == true)
                .OrderByDescending(x => x.DateandTime)
                .Select(x => x.DateandTime)
                .FirstOrDefault();

            var getdate = _localizationService.GetResourceAsync("Chatbot.ChatBotTime").Result;
            ViewBag.chatbotimage = Convert.ToString(_localizationService.GetResourceAsync("Chatbot.Chatbotimage").Result);

            if (int.TryParse(getdate, out int minutes) && lastQuestionTime.HasValue)
            {
                var lastTimeWithAddedMinutes = lastQuestionTime.Value.AddMinutes(minutes);

                if (lastTimeWithAddedMinutes < DateTime.Now)
                {
                    var delehistory = allchat.Where(x => x.CustID == custid).ToList();
                    foreach (var i in delehistory)
                    {
                        _chatbothistory.DeleteChatBotHistory(i);
                    }
                    // allchat = _chatbothistory.GetAllChatbothistory();
                    return Json(1);
                }
            }

            return Json(0);
        }
        public class QuestionRequest
        {
            public string Message { get; set; } = string.Empty;
        }
        //  [HttpPost]
        //  public async Task<IActionResult> Ask(string userInput)
        //  {
        //      var response = await _ChatService.GetAnswerAsync(userInput);
        //      _chatHistory.Add(new ChatMessage { UserMessage = userInput, BotResponse = response });
        //      return RedirectToAction("Index");
        //  }
        //  private readonly string[] _urls =
        //{
        //      "https://hansamed.net/",
        //      "https://citagenix.com/"
        //  };

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RebuildEmbeddings()
        {
            await _ChatService.RebuildAllProductEmbeddingsAsync();
            return Ok("Embeddings rebuilt for all published products.");
        }

        /// <summary>
        /// Example endpoint to get all embeddings (for testing)
        /// </summary>
        [HttpGet("AllEmbeddings")]
        public async Task<IActionResult> GetAllEmbeddings()
        {
            var embeddings = await _ChatService.GetAllEmbeddingsAsync();
            return Ok(embeddings);
        }

        public string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.InnerText.Trim();
        }

        //[HttpGet]
        //public async Task<IActionResult> ScrapeSites()
        //{
        //    foreach (var url in _urls)
        //    {
        //        await _ChatService.ScrapeSite(url);
        //    }

        //    await _ChatService.SaveAllAsync(); // ✅ Save combined.json after all URLs

        //    return RedirectToAction("Index");
        //}      
        public class TopicModel
        {
            public int id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;

        }

        public List<TopicModel> checkdiscountpromotio(string request)
        {

            var Content = new List<TopicModel>();
            try
            {
                var connection = DataSettingsManager.LoadSettings().ConnectionString;
                var conn = new SqlConnection(connection);
                SqlDataAdapter da = new SqlDataAdapter();
                conn.Open();
                using (IDbConnection conn1 = new SqlConnection(connection))
                {

                    DynamicParameters ds = new DynamicParameters();
                    ds.Add("@Keywords", request);
                    var result = conn1.Query<TopicModel>("SearchTopicByKeywords", param: ds, commandType: CommandType.StoredProcedure);
                    conn.Close();
                    return result.ToList();


                }
            }
            catch (Exception ex)
            {
                return Content;
            }
        }
        // ✅ Load data from SQL
        private async Task<List<TopicModel>> LoadTopicsFromDatabase(string request)
        {
            var datasettings = DataSettingsManager.LoadSettings();
            var connstring = datasettings.ConnectionString;
            using var connection = new SqlConnection(connstring);
            await connection.OpenAsync();

            var sql = @"SELECT Title, Body, Id 
                        FROM [Citagenix(New)].[dbo].[Topic] 
                        WHERE Published = 1 AND SystemName IS NOT NULL";
            //var sql = @"SELECT TextData as Title,TextData as Body 
            //            FROM [Citagenix(New)].[dbo].[ProductEmbeddings]";
            //WHERE Published = 1 AND SystemName IS NOT NULL";

            // var topicList = await connection.QueryAsync<TopicModel>(sql);
            var topicList = checkdiscountpromotio(request);
            return topicList.ToList();
        }

        // ✅ Build Lucene index
        //private void BuildIndex(IEnumerable<TopicModel> topics)
        //{
        //    var dir = FSDirectory.Open(_indexPath);
        //    var analyzer = new StandardAnalyzer(AppLuceneVersion);
        //    var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer)
        //    {
        //        OpenMode = Lucene.Net.Index.OpenMode.CREATE // overwrite index each time
        //    };

        //    using var writer = new IndexWriter(dir, indexConfig);

        //    foreach (var topic in topics)
        //    {
        //        var doc = new Lucene.Net.Documents.Document
        //        {
        //            new StringField("Title", topic.Title ?? string.Empty, Field.Store.YES),
        //            new TextField("Body", topic.Body ?? string.Empty, Field.Store.YES),
        //            new TextField("Id", (topic.id ).ToString(), Field.Store.YES)
        //        };
        //        writer.AddDocument(doc);
        //    }

        //    writer.Flush(true, true);
        //}

        //    // ✅ Search Lucene
        //private List<TopicModel> SearchTopics(string queryText, int maxResults = 10)
        //{
        //    var dir = FSDirectory.Open(_indexPath);
        //    var analyzer = new StandardAnalyzer(AppLuceneVersion);

        //    using var reader = DirectoryReader.Open(dir);
        //    var searcher = new IndexSearcher(reader);

        //    // Search both fields
        //    var parser = new MultiFieldQueryParser(AppLuceneVersion, new[] { "Title", "Body", "Id" }, analyzer);

        //    var query = parser.Parse(queryText);

        //    var hits = searcher.Search(query, maxResults).ScoreDocs;

        //    return hits.Select(hit =>
        //    {
        //        var doc = searcher.Doc(hit.Doc);
        //        return new TopicModel
        //        {
        //            id = int.TryParse(doc.Get("Id"), out var parsedId) ? parsedId : 0,
        //            Title = doc.Get("Title"),
        //            Body = doc.Get("Body")
        //        };
        //    }).ToList();
        //}
    }
}