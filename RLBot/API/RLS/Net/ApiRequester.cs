using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RLBot.API.RLS.Exceptions;
using RLBot.API.RLS.Net.Models;

namespace RLBot.API.RLS.Net
{
    /// <summary>
    ///     Used to send HTTP requests to https://api.rocketleaguestats.com.
    /// </summary>
    internal class ApiRequester : IDisposable
    {
        private readonly HttpClient _client;

        public ApiRequester(string apiKey)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.rocketleaguestats.com/v1/"),
                DefaultRequestHeaders =
                {
                    { "Authorization", apiKey }
                }
            };
        }

        public async Task<T> Get<T>(string relativeUrl)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl))
            using (var response = await SendAsync(request))
            {
                var result = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(result);
            }
        }

        public async Task<T> Post<T>(string relativeUrl, object data)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl))
            {
                var requestData = JsonConvert.SerializeObject(data, Formatting.None);
                request.Content = new StringContent(requestData, Encoding.UTF8, "application/json");

                using (var response = await SendAsync(request))
                {
                    var result = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<T>(result);
                }
            }
        }

        protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            try
            {
                var errorMessage = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(errorMessage))
                {
                    throw new RlsApiException($"Request failed with status code {(int)response.StatusCode} ({response.StatusCode}), there was no error message available.")
                    {
                        HttpStatusCode = (int)response.StatusCode
                    };
                }

                var error = JsonConvert.DeserializeObject<Error>(errorMessage);

                throw new RlsApiException($"Request failed with status code {(int)response.StatusCode} ({response.StatusCode}), RLS: '{error.Message}'.")
                {
                    HttpStatusCode = (int)response.StatusCode,
                    RlsError = error
                };
            }
            catch (JsonException e)
            {
                throw new RlsApiException($"Request failed with status code {(int)response.StatusCode} ({response.StatusCode}), we were unable to parse the error message.", e)
                {
                    HttpStatusCode = (int)response.StatusCode
                };
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
