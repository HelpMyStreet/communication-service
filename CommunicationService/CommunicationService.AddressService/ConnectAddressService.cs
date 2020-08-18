using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.AddressService.Request;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Contracts.Shared;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Utils;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.AddressService
{
    public class ConnectAddressService : IConnectAddressService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;

        public ConnectAddressService(IHttpClientWrapper httpClientWrapper)
        {
            _httpClientWrapper = httpClientWrapper;
        }

        public async Task<GetPostcodeCoordinatesResponse> GetPostcodeCoordinates(GetPostcodeCoordinatesRequest getPostcodeCoordinatesRequest)
        {
            string json = JsonConvert.SerializeObject(getPostcodeCoordinatesRequest);
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.AddressService, "/api/GetPostcodeCoordinates", data, CancellationToken.None))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var sendEmailResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetPostcodeCoordinatesResponse, AddressServiceErrorCode>>(jsonResponse);
                if (sendEmailResponse.HasContent && sendEmailResponse.IsSuccessful)
                {
                    return sendEmailResponse.Content;
                }
            }
            return null;
        }
    }
}
