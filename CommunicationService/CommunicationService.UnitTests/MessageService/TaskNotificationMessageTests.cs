using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class TaskNotificationMessageTests
    {
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        private Mock<IConnectRequestService> _requestService;
        private Mock<IOptions<EmailConfig>> _emailConfig;
        private GetGroupMembersResponse _getGroupMembersResponse;
        private GetGroupNewRequestNotificationStrategyResponse _getGroupNewRequestNotificationStrategyResponse;
        private GetVolunteersByPostcodeAndActivityResponse _getVolunteersByPostcodeAndActivityResponse;
        private GetJobDetailsResponse _getJobDetailsResponse;
        private const int GROUPID = -1;

        private TaskNotificationMessage _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            SetupGroupService();
            SetupUserService();
            SetupRequestService();
            SetupEmailConfig();

            _getGroupMembersResponse = new GetGroupMembersResponse()
            {
                Users = new List<int>()
                {
                    1,2,3,4,5,6,7,8,9,10,11,12
                }
            };

            _getJobDetailsResponse = new GetJobDetailsResponse()
            {
                JobSummary = new JobSummary()
                {
                    ReferringGroupID = GROUPID,
                    PostCode = "PostCode",
                    SupportActivity = SupportActivities.Shopping
                }
            };

            _classUnderTest = new TaskNotificationMessage(
                _userService.Object,
                _requestService.Object,
                _groupService.Object,
                _emailConfig.Object
                ); ;
        }

        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();

            _groupService.Setup(x => x.GetGroupMembers(It.IsAny<int>()))
                .ReturnsAsync(() => _getGroupMembersResponse);

            _groupService.Setup(x => x.GetGroupNewRequestNotificationStrategy(It.IsAny<int>()))
                .ReturnsAsync(() => _getGroupNewRequestNotificationStrategyResponse);
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();

            _userService.Setup(x => x.GetVolunteersByPostcodeAndActivity(
                It.IsAny<string>(),
                It.IsAny<List<SupportActivities>>(),
                It.IsAny<double?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _getVolunteersByPostcodeAndActivityResponse);
        }

        private void SetupRequestService()
        {
            _requestService = new Mock<IConnectRequestService>();

            _requestService.Setup(x => x.GetJobDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(() => _getJobDetailsResponse);
        }

        private void SetupEmailConfig()
        {
            _emailConfig = new Mock<IOptions<EmailConfig>>();
        }

        [Test]
        public void MissingStrategy_ThrowsException()
        {
            int? jobId = 1;
            int? groupId = GROUPID;
            int? recipientUserId = null;
            int? requestId = null;

            _getGroupNewRequestNotificationStrategyResponse = null;
            Exception ex = Assert.ThrowsAsync<Exception>(() => _classUnderTest.IdentifyRecipients
            (
             recipientUserId, jobId, groupId, requestId, null
            ));

            Assert.AreEqual($"No strategy for {GROUPID}", ex.Message);
        }

        [Test]
        public async Task IdentifyRecipientsReturnsCorrectUsers()
        {
            int? recipientUserId = null;
            int? jobId = 1;
            int? groupId = GROUPID;
            int? requestId = null;
            int maxVolunteer = 10;

            _getGroupNewRequestNotificationStrategyResponse = new GetGroupNewRequestNotificationStrategyResponse()
            {
                MaxVolunteer = maxVolunteer,
                NewRequestNotificationStrategy = NewRequestNotificationStrategy.ClosestNEligibleVolunteers
            };

            List<VolunteerSummary> volunteerSummaries = new List<VolunteerSummary>();
            Random random = new Random();

            for (int i = 1; i < 50; i++)
            {
                volunteerSummaries.Add(new VolunteerSummary()
                {
                    DistanceInMiles = random.NextDouble(),
                    UserID = i
                });
            }

            _getVolunteersByPostcodeAndActivityResponse = new GetVolunteersByPostcodeAndActivityResponse()
            {
                Volunteers = volunteerSummaries
            };

            List<Core.Domains.SendMessageRequest> result = await _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId, requestId, null);

            Assert.AreEqual(maxVolunteer, result.Count(x => x.TemplateName == TemplateName.TaskNotification));
            Assert.AreEqual(0, result.Count(x => x.TemplateName == TemplateName.RequestorTaskNotification));

            var expected = volunteerSummaries.Where(x => _getGroupMembersResponse.Users.Any(u => u == x.UserID)).OrderBy(x => x.DistanceInMiles).Take(10).Select(x=> x.UserID).ToList();
            var actual = result.Where(x => x.TemplateName == TemplateName.TaskNotification).Select(x => x.RecipientUserID).ToList();
            Assert.AreEqual(expected,actual);
        }

        [Test]
        public async Task MissingGroupID_ThrowsException()
        {
            int? jobId = 1;
            int? groupId = null;
            int? recipientUserId = null;
            int? requestId = null;

            Exception ex = Assert.ThrowsAsync<Exception>(() => _classUnderTest.IdentifyRecipients
            (
             recipientUserId, jobId, groupId, requestId, null  
            ));

            Assert.AreEqual($"GroupID or JobID is missing", ex.Message);
        }

        

    }
}
