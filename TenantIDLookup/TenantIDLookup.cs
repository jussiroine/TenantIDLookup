using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace TenantIDLookup
{
    public static class TenantIDLookup
    {
        [FunctionName("TenantIDLookup")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("TenantIDLookup Begin.");

            // parse query parameter
            string tenantName = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "tenantName", true) == 0)
                .Value;

            // call Microsoft Graph to resolve ID 
            string tenantID;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var url = "https://login.windows.net/" + tenantName + ".onmicrosoft.com/v2.0/.well-known/openid-configuration";

                try
                {
                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();

                    dynamic json = JsonConvert.DeserializeObject(content);
                    tenantID = json.authorization_endpoint;
                    tenantID = tenantID.Substring(26, 36);

                }
                catch(Exception ex)
                {
                    tenantID = "Tenant name invalid or not found";

                    log.LogError($"TenantIDLookup: {ex.Message}");
                }
            }

            log.LogInformation("TenantIDLookup End.");

            return tenantID == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a valid tenant name")
                : req.CreateResponse(HttpStatusCode.OK, tenantID);
        }
    }
}
