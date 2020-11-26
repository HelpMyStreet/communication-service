using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using CommunicationService.Repo;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Utils.Models;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.UnitTests.MessageService
{
    public class TaskUpdateNewMessageTests
    {
        private TaskUpdateNewMessage _classUnderTest;
        private Mock<IConnectRequestService> _requestService;
        private Mock<IConnectUserService> _userService;
        private Mock<IConnectGroupService> _groupService;
        private Mock<ILinkRepository> _linkRepository;
        private Mock<IOptions<SendGridConfig>> _sendGridConfig;
        private Mock<IOptions<LinkConfig>> _linkConfig;

        private GetJobDetailsResponse _job;
        private User _user;
        private int? _userId;

        [SetUp]
        public void SetUp()
        {
            SetupRequestService();
            SetupUserService();
            SetupGroupService();
            SetupLinkRepository();
            SetupSendGridConfig();
            SetupLinkConfig();

            _classUnderTest = new TaskUpdateNewMessage(
                _requestService.Object,
                _userService.Object,
                _groupService.Object,
                _linkRepository.Object,
                _linkConfig.Object,
                _sendGridConfig.Object);
        }

        #region Setup

        private void SetupRequestService()
        {
            _requestService = new Mock<IConnectRequestService>();
            _requestService.Setup(x => x.GetJobDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(() => _job);
            _requestService.Setup(x => x.GetRelevantVolunteerUserID(It.IsAny<GetJobDetailsResponse>()))
                .Returns(() => _userId);
        }
        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();
            _userService.Setup(x => x.GetUserByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(() => _user);
        }
        private void SetupGroupService()
        {
            _groupService = new Mock<IConnectGroupService>();
        }
        private void SetupLinkRepository()
        {
            _linkRepository = new Mock<ILinkRepository>();
        }
        private void SetupSendGridConfig()
        {
            _sendGridConfig = new Mock<IOptions<SendGridConfig>>();
        }
        private void SetupLinkConfig()
        {
            _linkConfig = new Mock<IOptions<LinkConfig>>();
        }

        #endregion Setup

        [Test]
        public void IdentifyRecipients_Returns3Recipients()
        {
            int? recipientUserId  = null;
            int? jobId = 1;
            int? groupId = null;
            string recipientEmailAddress = "recipient@test.com";
            string requestorEmailAddress = "requestor@test.com";
            string volunteerEmailAddress = "volunteer@test.com";

            _userId = 1;

            _user = new User()
            {
                UserPersonalDetails = new UserPersonalDetails()
                {
                    EmailAddress = volunteerEmailAddress
                }
            };

            _job = new GetJobDetailsResponse()
            {
                Recipient = new RequestPersonalDetails()
                {
                    FirstName = "Recipient FirstName",
                    LastName = "Recipient LastName",
                    EmailAddress = recipientEmailAddress
                },
                Requestor = new RequestPersonalDetails()
                {
                    FirstName = "Requestor FirstName",
                    LastName  = "Requestor LastName",
                    EmailAddress = requestorEmailAddress
                }
            };


            List<SendMessageRequest> recipients = _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId).Result;

            Assert.AreEqual(recipients.Count, 3);
            Assert.AreEqual(recipients[0].AdditionalParameters, null);
            Assert.AreEqual(recipients[1].AdditionalParameters["RecipientOrRequestor"], "Recipient");
            Assert.AreEqual(recipients[2].AdditionalParameters["RecipientOrRequestor"], "Requestor");
        }

        [Test]
        public void IdentifyRecipients_Returns1RecipientsAsRequestor()
        {
            int? recipientUserId = null;
            int? jobId = 1;
            int? groupId = null;
            string requestorEmailAddress = "requestor@test.com";

            _job = new GetJobDetailsResponse()
            {
                Requestor = new RequestPersonalDetails()
                {
                    FirstName = "Requestor FirstName",
                    LastName = "Requestor LastName",
                    EmailAddress = requestorEmailAddress
                }
            };


            List<SendMessageRequest> recipients = _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId).Result;

            Assert.AreEqual(recipients.Count, 1);
            Assert.AreEqual(recipients[0].AdditionalParameters["RecipientOrRequestor"], "Requestor");
        }


        [Test]
        public void IdentifyRecipients_Returns1RecipientsAsRecipient()
        {
            int? recipientUserId = null;
            int? jobId = 1;
            int? groupId = null;
            string recipientEmailAddress = "recipient@test.com";

            _job = new GetJobDetailsResponse()
            {
                Recipient = new RequestPersonalDetails()
                {
                    FirstName = "Recipient FirstName",
                    LastName = "Recipient LastName",
                    EmailAddress = recipientEmailAddress
                }
            };


            List<SendMessageRequest> recipients = _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId).Result;

            Assert.AreEqual(recipients.Count, 1);
            Assert.AreEqual(recipients[0].AdditionalParameters["RecipientOrRequestor"], "Recipient");
        }

        [Test]
        public void IdentifyRecipients_Returns1Recipients_WhenRecipientAndRequestorAreSame()
        {
            int? recipientUserId = null;
            int? jobId = 1;
            int? groupId = null;
            string requestorEmailAddress = "requestor@test.com";

            _job = new GetJobDetailsResponse()
            {
                Requestor = new RequestPersonalDetails()
                {
                    FirstName = "Requestor FirstName",
                    LastName = "Requestor LastName",
                    EmailAddress = requestorEmailAddress
                },
                Recipient = new RequestPersonalDetails()
                {
                    FirstName = "Requestor FirstName",
                    LastName = "Requestor LastName",
                    EmailAddress = requestorEmailAddress
                }
            };


            List<SendMessageRequest> recipients = _classUnderTest.IdentifyRecipients(recipientUserId, jobId, groupId).Result;

            Assert.AreEqual(recipients.Count, 1);
            Assert.AreEqual(recipients[0].AdditionalParameters["RecipientOrRequestor"], "Recipient");
        }
    }
}
