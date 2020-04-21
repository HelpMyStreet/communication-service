using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.Core.Utils;
using HelpMyStreet.Utils.Models;
using Marvin.StreamExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

            PostUsersForListOfUserIDResponse postUsersForListOfUserIDResponse = null;


            using (HttpResponseMessage response = await _httpClientWrapper.PostAsync(HttpClientConfigName.UserService, absolutePath, httpContent, CancellationToken.None).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                postUsersForListOfUserIDResponse = JsonConvert.DeserializeObject<PostUsersForListOfUserIDResponse>(content);

                if (postUsersForListOfUserIDResponse!=null && postUsersForListOfUserIDResponse.Users!=null)
                {
                    result = postUsersForListOfUserIDResponse.Users;
                }
            }
            return result;
        }
    }
}
