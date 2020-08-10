using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Exception;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using CommunicationService.MessageService.Substitution;
using CommunicationService.SendGridService;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.UserService.Response;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class DailyDigestMessageTests
    {
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        private Mock<IConnectRequestService> _requestService;
        private Mock<IOptions<EmailConfig>> _emailConfig;
        private EmailConfig _emailConfigSettings;
        private GetUsersResponse _getUsersResponse;
        private GetUserGroupsResponse _getUserGroupsResponse;
        private HelpMyStreet.Utils.Models.User _user;
        private GetJobsByFilterResponse _getJobsByFilterResponse;

        private DailyDigestMessage _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            SetupGroupService();
            SetupUserService();
            SetupRequestService();
            SetupEmailConfig();

            _classUnderTest = new DailyDigestMessage(_groupService.Object, _userService.Object, _requestService.Object, _emailConfig.Object);
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

            _requestService.Setup(x => x.GetJobsByFilter(It.IsAny<GetJobsByFilterRequest>()))
                .ReturnsAsync(() => _getJobsByFilterResponse);
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

        [Test]
        public void IdentifyRecipientsBasedOnSupportRadiusMiles_ReturnsCorrectUsers()
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

            var result = _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId);
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

            var result = await _classUnderTest.PrepareTemplateData(recipientUserId, jobId, groupId,templateName);
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

            var result = await _classUnderTest.PrepareTemplateData(recipientUserId, jobId, groupId, templateName);
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

            _user = new HelpMyStreet.Utils.Models.User()
            {
                ID = 1,
                SupportActivities = new List<HelpMyStreet.Utils.Enums.SupportActivities>() { HelpMyStreet.Utils.Enums.SupportActivities.Shopping},
                PostalCode = "NG1 6DQ"
                
            };

            _getJobsByFilterResponse = new GetJobsByFilterResponse()
            {
                JobSummaries = new List<HelpMyStreet.Utils.Models.JobSummary>()
            };

            var result = await _classUnderTest.PrepareTemplateData(recipientUserId, jobId, groupId, templateName);
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

            _user = new HelpMyStreet.Utils.Models.User()
            {
                ID = 1,
                SupportActivities = new List<HelpMyStreet.Utils.Enums.SupportActivities>() { HelpMyStreet.Utils.Enums.SupportActivities.Shopping },
                PostalCode = "NG1 6DQ",
                SupportRadiusMiles = 20,
                UserPersonalDetails = new HelpMyStreet.Utils.Models.UserPersonalDetails()
                {
                    FirstName = "FIRST NAME",
                    EmailAddress = "EMAIL ADDRESS",
                    DisplayName = "DISPLAY NAME"
                }

            };

            List<HelpMyStreet.Utils.Models.JobSummary> jobSummaries = new List<HelpMyStreet.Utils.Models.JobSummary>();
            jobSummaries.Add(new HelpMyStreet.Utils.Models.JobSummary()
            {
                DistanceInMiles = 1,
                SupportActivity = HelpMyStreet.Utils.Enums.SupportActivities.Shopping
            });

            jobSummaries.Add(new HelpMyStreet.Utils.Models.JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = HelpMyStreet.Utils.Enums.SupportActivities.Shopping
            });

            jobSummaries.Add(new HelpMyStreet.Utils.Models.JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = HelpMyStreet.Utils.Enums.SupportActivities.CheckingIn
            });

            _getJobsByFilterResponse = new GetJobsByFilterResponse()
            {
                JobSummaries = jobSummaries
            };

            var chosenJobCount = jobSummaries.Count(x => _user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles < _user.SupportRadiusMiles);
            var result = await _classUnderTest.PrepareTemplateData(recipientUserId, jobId, groupId, templateName);
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

            _user = new HelpMyStreet.Utils.Models.User()
            {
                ID = 1,
                SupportActivities = new List<HelpMyStreet.Utils.Enums.SupportActivities>() { HelpMyStreet.Utils.Enums.SupportActivities.Shopping },
                PostalCode = "NG1 6DQ",
                SupportRadiusMiles = 20,
                UserPersonalDetails = new HelpMyStreet.Utils.Models.UserPersonalDetails()
                {
                    FirstName = "FIRST NAME",
                    EmailAddress = "EMAIL ADDRESS",
                    DisplayName = "DISPLAY NAME"
                }

            };

            List<HelpMyStreet.Utils.Models.JobSummary> jobSummaries = new List<HelpMyStreet.Utils.Models.JobSummary>();
            jobSummaries.Add(new HelpMyStreet.Utils.Models.JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = HelpMyStreet.Utils.Enums.SupportActivities.CheckingIn
            });

            _getJobsByFilterResponse = new GetJobsByFilterResponse()
            {
                JobSummaries = jobSummaries
            };

            var chosenJobCount = jobSummaries.Count(x => _user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles < _user.SupportRadiusMiles);
            var result = await _classUnderTest.PrepareTemplateData(recipientUserId, jobId, groupId, templateName);
            Assert.AreEqual(null, result);
        }

        [Test]
        public async Task PrepareTemplateData_ThrowsExceptionWhenRecipientUserIDIsNull()
        {
            int? recipientUserId = null;
            int? jobId = null;
            int? groupId = null;
            string templateName = string.Empty;

            Exception ex = Assert.ThrowsAsync<Exception>(() => _classUnderTest.PrepareTemplateData
            (
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

            _user = new HelpMyStreet.Utils.Models.User()
            {
                ID = 1,
                SupportActivities = new List<HelpMyStreet.Utils.Enums.SupportActivities>() { HelpMyStreet.Utils.Enums.SupportActivities.Shopping },
                PostalCode = "NG1 6DQ",
                SupportRadiusMiles = 20,
                UserPersonalDetails = new HelpMyStreet.Utils.Models.UserPersonalDetails()
                {
                    FirstName = "FIRST NAME",
                    EmailAddress = "EMAIL ADDRESS",
                    DisplayName = "DISPLAY NAME"
                }

            };

            List<HelpMyStreet.Utils.Models.JobSummary> jobSummaries = new List<HelpMyStreet.Utils.Models.JobSummary>();
            jobSummaries.Add(new HelpMyStreet.Utils.Models.JobSummary()
            {
                DistanceInMiles = 1,
                SupportActivity = HelpMyStreet.Utils.Enums.SupportActivities.Shopping
            });

            jobSummaries.Add(new HelpMyStreet.Utils.Models.JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = HelpMyStreet.Utils.Enums.SupportActivities.Shopping
            });

            jobSummaries.Add(new HelpMyStreet.Utils.Models.JobSummary()
            {
                DistanceInMiles = 2,
                SupportActivity = HelpMyStreet.Utils.Enums.SupportActivities.CheckingIn
            });

            _getJobsByFilterResponse = new GetJobsByFilterResponse()
            {
                JobSummaries = jobSummaries
            };

            var criteriaJobs = jobSummaries.Where(x => _user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles < _user.SupportRadiusMiles);
            var otherJobs = jobSummaries.Where(x => !criteriaJobs.Contains(x));
            var otherJobsStats = otherJobs.GroupBy(x => x.SupportActivity, x => x.DueDate, (activity, dueDate) => new { Key = activity, Count = dueDate.Count(), Min = dueDate.Min() });
            otherJobsStats = otherJobsStats.OrderByDescending(x => x.Count);

            var result = await _classUnderTest.PrepareTemplateData(recipientUserId, jobId, groupId, templateName);
            DailyDigestData ddd = (DailyDigestData)result.BaseDynamicData;


            Assert.AreEqual(criteriaJobs.Count(), ddd.ChosenJobs);
            Assert.AreEqual(otherJobs.Count()>0, ddd.OtherJobs);

        }
    }
}
