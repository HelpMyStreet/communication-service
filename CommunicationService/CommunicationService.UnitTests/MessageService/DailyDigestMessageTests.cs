using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains.RequestService;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.Core.Services;
using CommunicationService.MessageService;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.AddressService.Request;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Exceptions;
using HelpMyStreet.Utils.Models;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class DailyDigestMessageTests
    {
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        private Mock<IConnectRequestService> _requestService;
        private Mock<IOptions<EmailConfig>> _emailConfig;
        private Mock<IJobFilteringService> _jobFilteringService;
        private Mock<IConnectAddressService> _addressService;
        private Mock<ICosmosDbService> _cosmosDbService;
        private EmailConfig _emailConfigSettings;
        private GetUsersResponse _getUsersResponse;
        private GetUserGroupsResponse _getUserGroupsResponse;
        private User _user;
        private GetJobsByStatusesResponse _getJobsByStatusesResponse;
        private GetPostcodeCoordinatesResponse _getPostcodeCoordinatesResponse;
        private List<JobSummary> _filteredJobs;


        private DailyDigestMessage _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            SetupGroupService();
            SetupUserService();
            SetupRequestService();
            SetupEmailConfig();
            SetupJobFilterService();
            SetupAddressService();
            SetupCosmosDBService();

            _classUnderTest = new DailyDigestMessage(
                _groupService.Object,
                _userService.Object,
                _requestService.Object,
                _emailConfig.Object,
                _jobFilteringService.Object,
                _addressService.Object,
                _cosmosDbService.Object) ;
        }

        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();

            _groupService.Setup(x => x.GetUserGroups(It.IsAny<int>()))
                .ReturnsAsync(() => _getUserGroupsResponse);
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();

            _userService.Setup(x => x.GetUsers())
                .ReturnsAsync(() => _getUsersResponse);

            _userService.Setup(x => x.GetUserByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(() => _user);
        }

        private void SetupRequestService()
        {
            _requestService = new Mock<IConnectRequestService>();

            _requestService.Setup(x => x.GetJobsByStatuses(It.IsAny<GetJobsByStatusesRequest>()))
                .ReturnsAsync(() => _getJobsByStatusesResponse);
        }

        private void SetupEmailConfig()
        {
            _emailConfigSettings = new EmailConfig()
            {
                ShowUserIDInEmailTitle = true,
                DigestOtherJobsDistance = 20,
                RegistrationChaserMaxTimeInHours = 2,
                RegistrationChaserMinTimeInMinutes = 30,
                ServiceBusSleepInMilliseconds = 1000
            };

            _emailConfig = new Mock<IOptions<EmailConfig>>();
            _emailConfig.SetupGet(x => x.Value).Returns(_emailConfigSettings);
        }

        private void SetupAddressService()
        {
            _addressService = new Mock<IConnectAddressService>();
            _addressService.Setup(x => x.GetPostcodeCoordinates(It.IsAny<GetPostcodeCoordinatesRequest>()))
                .ReturnsAsync(() => _getPostcodeCoordinatesResponse);
        }

        private void SetupJobFilterService()
        {
            _jobFilteringService = new Mock<IJobFilteringService>();
            _jobFilteringService.Setup(x => x.FilterJobSummaries(
                It.IsAny<List<JobSummary>>(),
                It.IsAny<List<SupportActivities>>(),
                It.IsAny<string>(),
                It.IsAny<double?>(),
                It.IsAny<Dictionary<SupportActivities, double?>>(),
                It.IsAny<int?>(),
                It.IsAny<List<int>>(),
                It.IsAny<List<JobStatuses>>(),
                It.IsAny<List<PostcodeCoordinate>>()
                )).ReturnsAsync(() => _filteredJobs);
        }

        private void SetupCosmosDBService()
        {
            _cosmosDbService = new Mock<ICosmosDbService>();
        }

        [Test]
        public async Task IdentifyRecipientsBasedOnSupportRadiusMiles_ReturnsCorrectUsers()
        {
            int? recipientUserId = null;
            int? jobId = null;
            int? groupId = null;

            List<UserDetails> userDetails = new List<UserDetails>();
            userDetails.Add(new UserDetails()
            {
                SupportRadiusMiles = 1d,
                UserID = 1
            });

            userDetails.Add(new UserDetails()
            {
                SupportRadiusMiles = 2d,
                UserID = 2
            });

            userDetails.Add(new UserDetails()
            {
                UserID = 3
            });

            _getUsersResponse = new GetUsersResponse();
            _getUsersResponse.UserDetails = userDetails;

            var result = await _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId);
            Assert.AreEqual(userDetails.Count(x => x.SupportRadiusMiles.HasValue), result.Count);
        }

        [Test]
        public async Task PrepareTemplateData_ReturnsNullWhenNoUserGroupsData1()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            _getUserGroupsResponse = new GetUserGroupsResponse()
            {
                Groups = null
            };

            var result = await _classUnderTest.PrepareTemplateData(Guid.NewGuid(),recipientUserId, jobId, groupId,templateName);
            Assert.AreEqual(null,result);

        }

        [Test]
        public async Task PrepareTemplateData_ReturnsNullWhenNoUserGroupsData2()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            _getUserGroupsResponse = null;

            var result = await _classUnderTest.PrepareTemplateData(Guid.NewGuid(),recipientUserId, jobId, groupId, templateName);
            Assert.AreEqual(null, result);
        }

        [Test]
        public async Task PrepareTemplateData_ReturnsMullAsNoJobsReturned()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            _getUserGroupsResponse = new GetUserGroupsResponse()
            {
                Groups = new List<int>() { 1}
            };

            _user = new User()
            {
                ID = 1,
                SupportActivities = new List<SupportActivities>() { SupportActivities.Shopping},
                PostalCode = "NG1 6DQ"
                
            };

            var result = await _classUnderTest.PrepareTemplateData(Guid.NewGuid(),recipientUserId, jobId, groupId, templateName);
            Assert.AreEqual(null, result);
        }

        [Test]
        public async Task PrepareTemplateData_ReturnsEmailBuildDataAsThereAreChosenJobs()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            _getUserGroupsResponse = new GetUserGroupsResponse()
            {
                Groups = new List<int>() { 1 }
            };

            _user = new User()
            {
                ID = 1,
                SupportActivities = new List<SupportActivities>() { SupportActivities.Shopping },
                PostalCode = "NG1 6DQ",
                SupportRadiusMiles = 20,
                UserPersonalDetails = new UserPersonalDetails()
                {
                    FirstName = "FIRST NAME",
                    EmailAddress = "EMAIL ADDRESS",
                    DisplayName = "DISPLAY NAME"
                }

            };

            List<JobSummary> jobSummaries = new List<JobSummary>();
            jobSummaries.Add(new JobSummary()
            {
                DistanceInMiles = 1,
                SupportActivity = SupportActivities.Shopping
            });

            jobSummaries.Add(new JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = SupportActivities.Shopping
            });

            jobSummaries.Add(new JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = SupportActivities.CheckingIn
            });

            var chosenJobCount = jobSummaries.Count(x => _user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles < _user.SupportRadiusMiles);
            var result = await _classUnderTest.PrepareTemplateData(Guid.NewGuid(),recipientUserId, jobId, groupId, templateName);
            DailyDigestData ddd = (DailyDigestData) result.BaseDynamicData;


            Assert.AreEqual(chosenJobCount, ddd.ChosenJobs);
        }

        [Test]
        public async Task PrepareTemplateData_ReturnsEmailBuildDataAsThereAreNoChosenJobs()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            _getUserGroupsResponse = new GetUserGroupsResponse()
            {
                Groups = new List<int>() { 1 }
            };

            _user = new User()
            {
                ID = 1,
                SupportActivities = new List<SupportActivities>() { SupportActivities.Shopping },
                PostalCode = "NG1 6DQ",
                SupportRadiusMiles = 20,
                UserPersonalDetails = new UserPersonalDetails()
                {
                    FirstName = "FIRST NAME",
                    EmailAddress = "EMAIL ADDRESS",
                    DisplayName = "DISPLAY NAME"
                }

            };

            List<JobSummary> jobSummaries = new List<JobSummary>();
            jobSummaries.Add(new JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = SupportActivities.CheckingIn
            });

            _getJobsByStatusesResponse = new GetJobsByStatusesResponse()
            {
                JobSummaries = jobSummaries
            };

            _getPostcodeCoordinatesResponse = new GetPostcodeCoordinatesResponse()
            {
                PostcodeCoordinates = new List<PostcodeCoordinate>()
                {
                    new PostcodeCoordinate(){Postcode="DE23 6NY",Latitude=1d,Longitude=1d}
                }
            };

            _filteredJobs = jobSummaries;

            var chosenJobCount = jobSummaries.Count(x => _user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles < _user.SupportRadiusMiles);
            var result = await _classUnderTest.PrepareTemplateData(Guid.NewGuid(),recipientUserId, jobId, groupId, templateName);
            Assert.AreEqual(null, result);
        }

        [Test]
        public async Task PrepareTemplateData_ThrowsExceptionWhenRecipientUserIDIsNull()
        {
            int? recipientUserId = null;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            Exception ex = Assert.ThrowsAsync<BadRequestException>(() => _classUnderTest.PrepareTemplateData
            (
                Guid.NewGuid(),
                recipientUserId,
                jobId,
                groupId,
                templateName
            ));
            Assert.AreEqual($"recipientUserId is null", ex.Message);
        }

        [Test]
        public async Task PrepareTemplateData_Returns()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            _getUserGroupsResponse = new GetUserGroupsResponse()
            {
                Groups = new List<int>() { 1 }
            };

            _user = new User()
            {
                ID = 1,
                SupportActivities = new List<SupportActivities>() { SupportActivities.Shopping },
                PostalCode = "NG1 6DQ",
                SupportRadiusMiles = 20,
                UserPersonalDetails = new UserPersonalDetails()
                {
                    FirstName = "FIRST NAME",
                    EmailAddress = "EMAIL ADDRESS",
                    DisplayName = "DISPLAY NAME"
                }

            };

            List<JobSummary> jobSummaries = new List<JobSummary>();
            jobSummaries.Add(new JobSummary()
            {
                DistanceInMiles = 1,
                SupportActivity = SupportActivities.Shopping
            });

            jobSummaries.Add(new JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = SupportActivities.Shopping
            });

            jobSummaries.Add(new JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = SupportActivities.CheckingIn
            });

            _getJobsByStatusesResponse = new GetJobsByStatusesResponse()
            {
                JobSummaries = jobSummaries
            };

            _getPostcodeCoordinatesResponse = new GetPostcodeCoordinatesResponse()
            {
                PostcodeCoordinates = new List<PostcodeCoordinate>()
                {
                    new PostcodeCoordinate(){Postcode="DE23 6NY",Latitude=1d,Longitude=1d}
                }
            };

            _filteredJobs = jobSummaries;

            var criteriaJobs = jobSummaries.Where(x => _user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles < _user.SupportRadiusMiles);
            var otherJobs = jobSummaries.Where(x => !criteriaJobs.Contains(x));
            var otherJobsStats = otherJobs.GroupBy(x => x.SupportActivity, x => x.DueDate, (activity, dueDate) => new { Key = activity, Count = dueDate.Count(), Min = dueDate.Min() });
            otherJobsStats = otherJobsStats.OrderByDescending(x => x.Count);

            var result = await _classUnderTest.PrepareTemplateData(Guid.NewGuid(),recipientUserId, jobId, groupId, templateName);
            DailyDigestData ddd = (DailyDigestData)result.BaseDynamicData;


            Assert.AreEqual(criteriaJobs.Count(), ddd.ChosenJobs);
            Assert.AreEqual(otherJobs.Count()>0, ddd.OtherJobs);

        }
    }
}
