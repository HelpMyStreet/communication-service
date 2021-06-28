using CommunicationService.Core.Configuration;
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
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class GroupWelcomeMessageTests
    {
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        private Mock<IOptions<SendGridConfig>> _sendGridConfig;
        private SendGridConfig _sendGridConfigSettings;

        private GroupWelcomeMessage _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            SetupGroupService();
            SetupUserService();
            SetupSendGridConfig();

            _classUnderTest = new GroupWelcomeMessage(
                _groupService.Object,
                _userService.Object,
                _sendGridConfig.Object) ;
        }

        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();
        }

        private void SetupSendGridConfig()
        {
            _sendGridConfigSettings = new SendGridConfig
            {
                BaseCommunicationUrl = string.Empty
            };

            _sendGridConfig = new Mock<IOptions<SendGridConfig>>();
            _sendGridConfig.SetupGet(x => x.Value).Returns(_sendGridConfigSettings);
        }

        [Test]
        public async Task GivenEmailIsTriggred_ThenEmailIsSentToCorrectRecipient()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = null;
            int? requestId = null;

            var result = await _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId, requestId, null);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(recipientUserId, result[0].RecipientUserID);
        }
    }
}
