using CommunicationService.Core.Configuration;
using CommunicationService.Core.Utils;
using Marvin.StreamExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Core
{
    //public class GetChampionCountByPostcodeResponse
    //{
    //    public int Count { get; set; }
    //}
    //public interface IUserService
    //{
    //    Task<int> GetChampionCountByPostcode(string postcode, CancellationToken cancellationToken);
    //}
    //public class UserService : IUserService
    //{
    //    private readonly IHttpClientWrapper _httpClientWrapper;

    //    public UserService(IHttpClientWrapper httpClientWrapper)
    //    {
    //        _httpClientWrapper = httpClientWrapper;
    //    }

    //    public async Task<int> GetChampionCountByPostcode(string postcode, CancellationToken cancellationToken)
    //    {
    //        string path = $"api/GetChampionCountByPostcode?postcode={postcode}";
    //        GetChampionCountByPostcodeResponse championCountResponse;
    //        using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.UserService, path, cancellationToken).ConfigureAwait(false))
    //        {
    //            response.EnsureSuccessStatusCode();
    //            Stream stream = await response.Content.ReadAsStreamAsync();
    //            championCountResponse = stream.ReadAndDeserializeFromJson<GetChampionCountByPostcodeResponse>();
    //        }
    //        return championCountResponse.Count;
    //    }
    //}
}
