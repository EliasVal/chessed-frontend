using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Chessed
{
    class CustomHttpClient : HttpClient
    {
        public async Task<JsonObject> MakeHTTPReq(string route, dynamic obj, string returnType = "application/json")
        {
            using StringContent stringContent = new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, returnType);
            using HttpResponseMessage res = await PostAsync(route, stringContent);

            JsonObject content = JsonSerializer.Deserialize<JsonObject>(await res.Content.ReadAsStringAsync());

            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return content;
            }

            else throw new Exception(res.StatusCode.ToString(), new Exception(content["error"].ToString()));
        }
    }
}