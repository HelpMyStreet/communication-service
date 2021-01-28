using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.Shared;
using HelpMyStreet.Contracts.UserService.Request;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Utils.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.UserService
{
    public class ConnectUserService : IConnectUserService
    {
        private readonly IHttpClientWrapper _httpClientWrapper;

        public ConnectUserService(IHttpClientWrapper httpClientWrapper)
        {
            _httpClientWrapper = httpClientWrapper;
        }

        public async Task<GetUsersResponse> GetUsers()
        {
            string path = $"api/GetUsers";
            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.UserService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var usersResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetUsersResponse, UserServiceErrorCode>>(jsonResponse);

                if (usersResponse.HasContent && usersResponse.IsSuccessful)
                {
                    return usersResponse.Content;
                }
                else
                {
                    throw new System.Exception(usersResponse.Errors.ToString());
                }
            }
        }

        public async Task<User> GetUserByIdAsync(int userID)
        {
            string path = $"/api/GetUserByID?ID=" + userID;
            string absolutePath = $"{path}";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.UserService, absolutePath, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var usersResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetUserByIDResponse, UserServiceErrorCode>>(jsonResponse);

                if (usersResponse.HasContent && usersResponse.IsSuccessful)
                {
                    return usersResponse.Content.User;
                }
                else
                {
                    throw new System.Exception(usersResponse.Errors.ToString());
                }
            }
        }

        public async Task<GetVolunteersByPostcodeAndActivityResponse> GetVolunteersByPostcodeAndActivity(string postcode, List<SupportActivities> activities, double? shiftRadius, CancellationToken cancellationToken)
        {
            string path = $"api/GetVolunteersByPostcodeAndActivity";
            GetVolunteersByPostcodeAndActivityRequest request = new GetVolunteersByPostcodeAndActivityRequest
            {
                VolunteerFilter = new VolunteerFilter
                {
                    Postcode = postcode,
                    Activities = activities,
                    OverrideVolunteerRadius = shiftRadius
                }
            };

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.UserService, path, request, cancellationToken).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var helperResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetVolunteersByPostcodeAndActivityResponse, UserServiceErrorCode>>(jsonResponse);

                if (helperResponse.HasContent && helperResponse.IsSuccessful)
                {
                    return helperResponse.Content;
                }
                else
                {
                    throw new System.Exception(helperResponse.Errors.ToString());
                }
            }
        }

        public async Task<List<User>> PostUsersForListOfUserID(List<int> UserIDs)
        {
            List<User> result = new List<User>();
            string path = $"/api/PostUsersForListOfUserID";
            string absolutePath = $"{path}";

            PostUsersForListOfUserIDRequest postUsersForListOfUserIDRequest = new PostUsersForListOfUserIDRequest()
            {
                ListUserID = new ListUserID()
                {
                    UserIDs = UserIDs
                }
            };

            string json = JsonConvert.SerializeObject(postUsersForListOfUserIDRequest, Formatting.Indented);
            var httpContent = new StringContent(json);

            using (HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.UserService, path, httpContent, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var postUsersForListOfUserIDResponse = JsonConvert.DeserializeObject<ResponseWrapper<PostUsersForListOfUserIDResponse, UserServiceErrorCode>>(jsonResponse);

                if (postUsersForListOfUserIDResponse.HasContent && postUsersForListOfUserIDResponse.IsSuccessful)
                {
                    return postUsersForListOfUserIDResponse.Content.Users;
                }
                else
                {
                    throw new System.Exception(postUsersForListOfUserIDResponse.Errors.ToString());
                }
            }
        }

        public async Task<GetIncompleteRegistrationStatusResponse> GetIncompleteRegistrationStatusAsync()
        {
            string path = $"api/GetIncompleteRegistrationStatus";

            using (HttpResponseMessage response = await _httpClientWrapper.GetAsync(HttpClientConfigName.UserService, path, CancellationToken.None).ConfigureAwait(false))
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var incompleteRegistrationStatusResponse = JsonConvert.DeserializeObject<ResponseWrapper<GetIncompleteRegistrationStatusResponse, UserServiceErrorCode>>(jsonResponse);

                if (incompleteRegistrationStatusResponse.HasContent && incompleteRegistrationStatusResponse.IsSuccessful)
                {
                    return incompleteRegistrationStatusResponse.Content;
                }
                else
                {
                    throw new System.Exception(incompleteRegistrationStatusResponse.Errors.ToString());
                }
            }
        }
    }
}
