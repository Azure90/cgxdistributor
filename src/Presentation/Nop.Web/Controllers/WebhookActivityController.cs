using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Web.Controllers
{
    public class WebhookActivityController : Controller
    {
        [HttpPost]
        public async Task MyWebHookEndpoint()
        {
            using (StreamReader reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8))
            {
                try
                {
                    var body = await reader.ReadToEndAsync();
                    string jsontext = body;
                    dynamic data = JObject.Parse(body);
                    Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(jsontext);

                    if (!string.IsNullOrEmpty(myDeserializedClass.MessageId))
                    {
                        var statusname = myDeserializedClass.@event;
                        if (statusname == "unique_opened")
                        {
                            statusname = "Opened";
                        }
                        if (statusname == "invalid_email")
                        {
                            statusname = "Invalid Email";
                        }
                        if (statusname == "proxy_open")
                        {
                            statusname = "Proxy Open";
                        }
                        if (statusname == "soft_bounce")
                        {
                            statusname = "Soft Bounce";
                        }
                        if (statusname == "hard_bounce")
                        {
                            statusname = "Hard Bounce";
                        }

                        var connection = DataSettingsManager.LoadSettings().ConnectionString;
                        using (IDbConnection conn1 = new SqlConnection(connection))
                        {

                            DynamicParameters ds = new DynamicParameters();

                            ds.Add("@MessageId", myDeserializedClass.MessageId);
                            ds.Add("@statusname", statusname);
                            var result = conn1.Query("[UpdateEmailStatus]", param: ds, commandType: CommandType.StoredProcedure);

                        }
                    }
                }
                catch (Exception ex)
                {
                    Response.StatusCode = 502;
                }


            }
        }
        public class Root
        {
            public string @event { get; set; }
            public int id { get; set; }
            public string date { get; set; }
            public int ts { get; set; }
            [JsonProperty("message-id")]
            public string MessageId { get; set; }
            public string email { get; set; }
            public int ts_event { get; set; }
            public string subject { get; set; }
            public string sending_ip { get; set; }
            public long ts_epoch { get; set; }
        }
    }
}
