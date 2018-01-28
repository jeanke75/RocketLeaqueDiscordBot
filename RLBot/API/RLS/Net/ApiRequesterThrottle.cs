using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RLBot.API.RLS.Net
{
    /// <summary>
    ///     Used to send throttled HTTP requests to https://api.rocketleaguestats.com.
    ///     Automatically detects your rate limit configuration from the headers.
    /// </summary>
    internal class ApiRequesterThrottle : ApiRequester
    {
        private readonly Semaphore _queue;

        private int _rateLimitRemaining;

        private DateTime _rateLimitResetRemaining;

        public ApiRequesterThrottle(string apiKey) : base(apiKey)
        {
            _queue = new Semaphore(1, 1);
            _rateLimitRemaining = 2;
            _rateLimitResetRemaining = DateTime.UtcNow.AddSeconds(1);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            try
            {
                _queue.WaitOne();

                if (_rateLimitRemaining == 0)
                {
                    var startTime = DateTime.UtcNow;
                    var difference = _rateLimitResetRemaining - startTime;

                    if (difference > TimeSpan.Zero)
                    {
                        await Task.Delay(difference);
                    }
                }

                var response = await base.SendAsync(request);

                if (response.Headers.TryGetValues("x-rate-limit-remaining", out IEnumerable<string> rateLimitRemainingValues) &&
                    response.Headers.TryGetValues("x-rate-limit-reset-remaining", out IEnumerable<string> rateLimitResetValues))
                {
                    if (int.TryParse(rateLimitRemainingValues.First(), out int rateLimitRemaining) &&
                        int.TryParse(rateLimitResetValues.First(), out int rateLimitResetRemaining))
                    {
                        _rateLimitRemaining = rateLimitRemaining;
                        _rateLimitResetRemaining = DateTime.UtcNow.AddMilliseconds(rateLimitResetRemaining);
                    }
                }

                return response;
            }
            finally
            {
                _queue.Release();
            }
        }
    }
}