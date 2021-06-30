using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using HelpMyStreet.Utils.Enums;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class NewTaskPendingApprovalNotificationTests
    {
        private Mock<IConnectRequestService> _requestService;
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        private Mock<ILinkRepository> _linkRepository;
        private Mock<IOptions<LinkConfig>> _linkConfig;
        private LinkConfig _linkConfigSettings;
        private List<int> _groupMembers;

        private NewTaskPendingApprovalNotification _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            SetupRequestService();
            SetupGroupService();
            SetupUserService();
            SetupLinkRepository();
            SetupLinkConfig();

            _classUnderTest = new NewTaskPendingApprovalNotification(
                _requestService.Object,
                _groupService.Object,
                _userService.Object,
                _linkRepository.Object,
                _linkConfig.Object) ;
        }

        private void SetupRequestService()
        {
            _requestService = new Mock<IConnectRequestService>();
        }

        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();
            _groupService.Setup(x => x.GetGroupMembersForGivenRole(It.IsAny<int>(), It.IsAny<GroupRoles>()))
                .ReturnsAsync(() => _groupMembers);
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();
        }

        private void SetupLinkRepository()
        {
            _linkRepository = new Mock<ILinkRepository>();
        }

        private void SetupLinkConfig()
        {
            _linkConfigSettings = new LinkConfig();
            _linkConfig = new Mock<IOptions<LinkConfig>>();
            _linkConfig.SetupGet(x => x.Value).Returns(_linkConfigSettings);
        }

        [Test]
        public async Task GivenNoAdminsNoEmailSent()
        {
            int? recipientUserId = null;
            int? jobId = null;
            int? groupId = (int) Groups.Generic;
            int? requestId = 1;

            _groupMembers = new List<int>();

            var result = await _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId, requestId, null);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GivenXAdminsThenEachAdminSentEmail()
        {
            int? recipientUserId = null;
            int? jobId = null;
            int? groupId = (int)Groups.Generic;
            int? requestId = 1;

            _groupMembers = new List<int>()
            {
                1,
                2
            };

            var result = await _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId, requestId, null);
            Assert.AreEqual(_groupMembers, result.Select(x=> x.RecipientUserID).ToList());
        }
    }
}
