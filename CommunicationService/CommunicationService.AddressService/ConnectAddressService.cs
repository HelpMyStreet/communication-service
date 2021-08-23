using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Cache;
using HelpMyStreet.Contracts.AddressService.Request;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Contracts.Shared;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Utils.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.AddressService
{
    public class ConnectAddressService : IConnectAddressService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly IMemDistCache<LocationDetails> _memDistCache;
        private const string CACHE_KEY_PREFIX = "address-service-";

        public ConnectAddressService(IHttpClientWrapper httpClientWrapper, IMemDistCache<LocationDetails> memDistCache)
        {
            _httpClientWrapper = httpClientWrapper;
            _memDistCache = memDistCache;
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

        public async Task<LocationDetails> GetLocationDetails(Location location, CancellationToken cancellationToken)
        {
            return await _memDistCache.GetCachedDataAsync(async (cancellationToken) =>
            {
                GetLocationRequest request = new GetLocationRequest() { LocationRequest = new LocationRequest() { Location = location } };
                string json = JsonConvert.SerializeObject(request);
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.AddressService, "/api/GetLocation", data, CancellationToken.None))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var sendEmailResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetLocationResponse, AddressServiceErrorCode>>(jsonResponse);
                    if (sendEmailResponse.HasContent && sendEmailResponse.IsSuccessful)
                    {
                        return sendEmailResponse.Content.LocationDetails;
                    }
                    else
                    {
                        throw new HttpRequestException("Unable to fetch location details");
                    }
                }                
            }, $"{CACHE_KEY_PREFIX}-location-{(int)location}", RefreshBehaviour.WaitForFreshData, cancellationToken);
        }

        public async Task<GetLocationsByDistanceResponse> GetLocationsByDistance(string postCode, int maxDistance)
        {
            GetLocationsByDistanceRequest request = new GetLocationsByDistanceRequest() { Postcode = postCode, MaxDistance = maxDistance};
            string json = JsonConvert.SerializeObject(request);
            StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.AddressService, "/api/GetLocationsByDistance", data, CancellationToken.None))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var sendEmailResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetLocationsByDistanceResponse, AddressServiceErrorCode>>(jsonResponse);
                if (sendEmailResponse.HasContent && sendEmailResponse.IsSuccessful)
                {
                    return sendEmailResponse.Content;
                }
            }
            return null;
        }
    }
}
