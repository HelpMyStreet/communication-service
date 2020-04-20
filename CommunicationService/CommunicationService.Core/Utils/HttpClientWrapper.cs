using CommunicationService.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Core.Utils
{
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpClientWrapper(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public Task<HttpResponseMessage> GetAsync(HttpClientConfigName httpClientConfigName, string absolutePath, CancellationToken cancellationToken)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient(httpClientConfigName.ToString());
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, absolutePath);
            return httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        public Task PostAsync(HttpClientConfigName httpClientConfigName, string absolutePath, HttpContent content, CancellationToken cancellationToken)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient(httpClientConfigName.ToString());
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, absolutePath);
            request.Content = content;
            return httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

    }
}
