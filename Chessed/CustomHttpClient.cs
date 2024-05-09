using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chessed
{
    class CustomHttpClient : HttpClient
    {
        public async Task<dynamic> MakeHTTPReq(string route, dynamic obj, string returnType = "application/json")
        {
            using StringContent stringContent = new StringContent(JsonSerializer.Serialize(obj, Encoding.UTF8, returnType));
            HttpResponseMessage res = await PostAsync(route, stringContent);

            if (res.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<dynamic>(await res.Content.ReadAsStringAsync());
            }

            else throw new Exception(res.StatusCode.ToString());
        }
    }
}