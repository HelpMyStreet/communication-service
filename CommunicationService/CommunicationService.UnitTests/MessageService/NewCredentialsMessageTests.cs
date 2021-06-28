using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class NewCredentialsMessageTests
    {
        private Mock<IConnectGroupService> _groupService;
        private Mock<IConnectUserService> _userService;
        
        private NewCredentialsMessage _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            SetupGroupService();
            SetupUserService();

            _classUnderTest = new NewCredentialsMessage(
                _userService.Object,
                _groupService.Object);
        }

        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();
        }

        [Test]
        public async Task GivenEmailIsTriggred_ThenEmailIsSentToCorrectRecipient()
        {
            int? recipientUserId = 1;
            int? jobId = null;
            int? groupId = -1;
            int? requestId = null;

            var result = await _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId, requestId, null);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(recipientUserId, result[0].RecipientUserID);
        }
    }
}
