using System;
using RLBot.API.RLS.Net.Models;

namespace RLBot.API.RLS.Exceptions
{
    public class RlsApiException : Exception
    {
        public RlsApiException()
        {
        }

        public RlsApiException(string message) : base(message)
        {
        }

        public RlsApiException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public int HttpStatusCode { get; set; }

        public Error RlsError { get; set; }
    }
}