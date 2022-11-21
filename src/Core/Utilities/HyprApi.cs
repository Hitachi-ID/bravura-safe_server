using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Bit.Core.Utilities.Hypr
{
    public class HyprApi : IDisposable
    {
        private const string UrlScheme = "https";
        private const string UserAgent = "BravuraSafe_HyprAPICSharp/1.0 (.NET Core)";
        private const int defaultTimeout = 100;

        private readonly string _server;
        private readonly string _appid;
        private HttpClient httpClient;

        public HyprApi(string akey, string server, string appid, int timeout = defaultTimeout)
        {
            _server = server;
            _appid = appid;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgent);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", akey);
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);

            if (!ValidServer(server))
            {
                throw new HyprException("Invalid Hypr server configured.", new ArgumentException(nameof(server)));
            }
        }

        public static bool ValidServer(string server)
        {
            if (Uri.TryCreate($"{UrlScheme}://{server}", UriKind.Absolute, out var uri))
            {
                return (string.IsNullOrWhiteSpace(uri.PathAndQuery) || uri.PathAndQuery == "/");
            }
            return false;
        }

        public static string CanonicalizeParams(Dictionary<string, string> parameters)
        {
            var ret = new List<string>();
            foreach (var pair in parameters)
            {
                var p = string.Format("{0}={1}", HttpUtility.UrlEncode(pair.Key), HttpUtility.UrlEncode(pair.Value));
                ret.Add(p);
            }

            ret.Sort(StringComparer.Ordinal);
            return string.Join("&", ret.ToArray());
        }

        public async Task<HttpResponseMessage> ApiCallAsync(string method, string path, string jsonMessage = "{}")
        {
            HttpResponseMessage response;
            var url = string.Format("{0}://{1}{2}", UrlScheme, _server, path);
            if (method == "GET")
            {
                response = await httpClient.GetAsync(url);
            }
            else if (method == "PUT")
            {
                response = await httpClient.PutAsync(url, new StringContent(jsonMessage, Encoding.UTF8, "application/json"));
            }
            else if (method == "POST")
            {
                response = await httpClient.PostAsync(url, new StringContent(jsonMessage, Encoding.UTF8, "application/json"));
            }
            else
            {
                throw new HyprException("Unsupported method selected.", new ArgumentException(method));
            }
            return response;
        }

        public async Task<(HttpResponseMessage,T)> JSONApiCallAsync<T>(string method, string path, string jsonMessage = "{}")
            where T : class
        {
            var res = await ApiCallAsync(method, path, jsonMessage);
            var responseBody = await res.Content.ReadAsStringAsync();
            try
            {
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    return (res,JsonSerializer.Deserialize<T>(responseBody));
                }
                return (res, null);
            }
            catch (Exception e)
            {
                throw new HyprException("Could not deserialize JSON message.", new BadResponseException((int)res.StatusCode, e));
            }
        }

        public static string GetNonce()
        {
            SHA1 sha1 = SHA1.Create();
            Random rand = new Random();
            var generatedRandom = rand.Next().ToString();
            return BitConverter.ToString(sha1.ComputeHash(Encoding.Default.GetBytes(generatedRandom))).Replace("-", "");
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }

    public class HyprException : Exception
    {
        public int HttpStatus { get; private set; }

        public HyprException(string message)
            : base(message)
        { }

        public HyprException(string message, Exception inner)
            : base(message, inner)
        { }

        public HyprException(int httpStatus, string message, Exception inner)
            : base(message, inner)
        {
            HttpStatus = httpStatus;
        }
    }

    public class ApiException : HyprException
    {
        public int Code { get; private set; }
        public string ApiMessage { get; private set; }
        public string ApiMessageDetail { get; private set; }

        public ApiException(int code, int httpStatus, string apiMessage, string apiMessageDetail)
            : base(httpStatus, FormatMessage(code, apiMessage, apiMessageDetail), null)
        {
            Code = code;
            ApiMessage = apiMessage;
            ApiMessageDetail = apiMessageDetail;
        }

        private static string FormatMessage(int code, string apiMessage, string apiMessageDetail)
        {
            return string.Format("Hypr API Error {0}: '{1}' ('{2}')", code, apiMessage, apiMessageDetail);
        }
    }

    public class BadResponseException : HyprException
    {
        public BadResponseException(int httpStatus, Exception inner)
            : base(httpStatus, FormatMessage(httpStatus, inner), inner)
        { }

        private static string FormatMessage(int httpStatus, Exception inner)
        {
            var innerMessage = "(null)";
            if (inner != null)
            {
                innerMessage = string.Format("'{0}'", inner.Message);
            }
            return string.Format("Got error {0} with HTTP Status {1}", innerMessage, httpStatus);
        }
    }
}
