using CommunicationService.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Core.Utils
{
    public interface IHttpClientWrapper
    {
        Task<HttpResponseMessage> GetAsync(HttpClientConfigName httpClientConfigName, string absolutePath, CancellationToken cancellationToken);
        Task<HttpResponseMessage> PostAsync(HttpClientConfigName httpClientConfigName, string absolutePath, HttpContent content, CancellationToken cancellationToken);
    }
}
