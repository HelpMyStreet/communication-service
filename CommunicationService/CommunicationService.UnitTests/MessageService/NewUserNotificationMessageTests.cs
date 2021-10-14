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
    public struct GroupMembers
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public GroupRoles GroupRoles { get; set; }
    }
    public class NewUserNotificationMessageTests
    {
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        private SendGridConfig _sendGridConfigSettings;
        private Mock<IOptions<SendGridConfig>> _sendGridConfig;
        private NewUserNotificationMessage _classUnderTest;
        private List<GroupMembers> _groupMembers;

        [SetUp]
        public void SetUp()
        {
            SetupGroupService();
            SetupUserService();
            SetupSendGridConfig();

            _classUnderTest = new NewUserNotificationMessage(
                _groupService.Object,
                _userService.Object,
                _sendGridConfig.Object
                );
        }

        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();

            _groupMembers = new List<GroupMembers>();
            _groupMembers.Add(new GroupMembers() { GroupId = -1, UserId = 1, GroupRoles = GroupRoles.UserAdmin });
            _groupMembers.Add(new GroupMembers() { GroupId = -1, UserId = 2, GroupRoles = GroupRoles.UserAdmin_ReadOnly });
            _groupMembers.Add(new GroupMembers() { GroupId = -1, UserId = 2, GroupRoles = GroupRoles.UserAdmin });
            _groupMembers.Add(new GroupMembers() { GroupId = -1, UserId = 3, GroupRoles = GroupRoles.Member });
            _groupMembers.Add(new GroupMembers() { GroupId = -1, UserId = 4, GroupRoles = GroupRoles.Owner });
            _groupMembers.Add(new GroupMembers() { GroupId = -1, UserId = 5, GroupRoles = GroupRoles.RequestSubmitter });
            _groupMembers.Add(new GroupMembers() { GroupId = -1, UserId = 6, GroupRoles = GroupRoles.TaskAdmin });
            _groupMembers.Add(new GroupMembers() { GroupId = -2, UserId = 2, GroupRoles = GroupRoles.UserAdmin });

            _groupService.Setup(x => x.GetGroupMembersForGivenRole(It.IsAny<int>(), It.IsAny<GroupRoles>()))
                .ReturnsAsync((int i, GroupRoles gr) => _groupMembers.Where(x => x.GroupRoles == gr && x.GroupId == i).Select(x => x.UserId).ToList());
          
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();
        }

        private void SetupSendGridConfig()
        {
            _sendGridConfigSettings = new SendGridConfig();

            _sendGridConfig = new Mock<IOptions<SendGridConfig>>();
            _sendGridConfig.SetupGet(x => x.Value).Returns(_sendGridConfigSettings);
        }

        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(-3)]
        [Test]
        public async Task IdentifyRecipients_ReturnsCorrectUsers(int groupId)
        {
            int? recipientUserId = null;
            int? jobId = null;
            int? requestId = null;
            Dictionary<string, string> additionalParameters = new Dictionary<string, string>()
            {
                { "Volunteer","5" }
            };
            var recipients = await _classUnderTest.IdentifyRecipients(
                recipientUserId,
                jobId,
                groupId,
                requestId,
                additionalParameters);

            var expectedRecipients = _groupMembers
                .Where(x => x.GroupId == groupId && (x.GroupRoles == GroupRoles.UserAdmin || x.GroupRoles == GroupRoles.UserAdmin_ReadOnly))
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

            Assert.AreEqual(expectedRecipients, recipients.Select(x => x.RecipientUserID).ToList()) ;
        }
    }
}
