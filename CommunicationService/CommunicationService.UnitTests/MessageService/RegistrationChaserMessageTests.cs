using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using HelpMyStreet.Contracts.UserService.Request;
using HelpMyStreet.Contracts.UserService.Response;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class RegistrationChaserMessageTests
    {
        private Mock<IConnectUserService> _userService;
        private Mock<ICosmosDbService> _cosmosDbService;
        private Mock<IOptions<EmailConfig>> _emailConfig;
        private EmailConfig _emailConfigSettings;
        private GetIncompleteRegistrationStatusResponse _getIncompleteRegistrationStatusResponse;
        private List<EmailHistory> _emailHistory;

        private RegistrationChaserMessage _classUnderTest;

        [SetUp]
        public void SetUp()
        {
            SetupUserService();
            SetupEmailConfig();
            SetupCosmosDbService();

            _classUnderTest = new RegistrationChaserMessage(
                _userService.Object,
                _cosmosDbService.Object,
                _emailConfig.Object) ;
        }

        private void SetupUserService()
        {
            _userService = new Mock<IConnectUserService>();
            _userService.Setup(x => x.GetIncompleteRegistrationStatusAsync())
                .ReturnsAsync(() => _getIncompleteRegistrationStatusResponse);
        }

        private void SetupEmailConfig()
        { 
            _emailConfigSettings = new EmailConfig()
            {
                RegistrationChaserMaxTimeInHours = 2,
                RegistrationChaserMinTimeInMinutes = 30
            };
            _emailConfig = new Mock<IOptions<EmailConfig>>();
            _emailConfig.SetupGet(x => x.Value).Returns(_emailConfigSettings);
        }

        private void SetupCosmosDbService()
        {
            _cosmosDbService = new Mock<ICosmosDbService>();
            _cosmosDbService.Setup(x => x.GetEmailHistory(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(() => _emailHistory);
        }

        [Test]
        public async Task GivenIncompleteRegForVol_AndWithinTimeWindow_ThenEmailSent()
        {
            _emailHistory = new List<EmailHistory>();
            _getIncompleteRegistrationStatusResponse = new GetIncompleteRegistrationStatusResponse()
            {
                Users = new System.Collections.Generic.List<UserRegistrationStep>()
                {
                    new UserRegistrationStep()
                    {
                        UserId = 1,
                        RegistrationStep = 2,
                        DateCompleted = DateAndTime.Now.ToUniversalTime().AddMinutes(-60)
                    }
                }
            };

            var result = await _classUnderTest.IdentifyRecipients(null, null, null, null, null);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public async Task GivenIncompleteRegForVol_AndNotWithinTimeWindow_NoEmailSent()
        {
            _emailHistory = new List<EmailHistory>();
            _getIncompleteRegistrationStatusResponse = new GetIncompleteRegistrationStatusResponse()
            {
                Users = new System.Collections.Generic.List<UserRegistrationStep>()
                {
                    new UserRegistrationStep()
                    {
                        UserId = 1,
                        RegistrationStep = 2,
                        DateCompleted = DateAndTime.Now.ToUniversalTime().AddMinutes(-25)
                    }
                }
            };

            var result = await _classUnderTest.IdentifyRecipients(null, null, null, null, null);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GivenIncompleteRegForVol_OnlySentEmailsWithinTimeWindow()
        {
            _emailHistory = new List<EmailHistory>();
            _getIncompleteRegistrationStatusResponse = new GetIncompleteRegistrationStatusResponse()
            {
                Users = new System.Collections.Generic.List<UserRegistrationStep>()
                {
                    new UserRegistrationStep()
                    {
                        UserId = 1,
                        RegistrationStep = 2,
                        DateCompleted = DateAndTime.Now.ToUniversalTime().AddMinutes(-25)
                    },
                    new UserRegistrationStep()
                    {
                        UserId = 2,
                        RegistrationStep = 2,
                        DateCompleted = DateAndTime.Now.ToUniversalTime().AddMinutes(-60)
                    },
                    new UserRegistrationStep()
                    {
                        UserId = 3,
                        RegistrationStep = 2,
                        DateCompleted = DateAndTime.Now.ToUniversalTime().AddHours(-3)
                    }
                }
            };

            var minDate = DateAndTime.Now.ToUniversalTime().AddMinutes(-_emailConfigSettings.RegistrationChaserMinTimeInMinutes);
            var maxDate = DateAndTime.Now.ToUniversalTime().AddHours(-_emailConfigSettings.RegistrationChaserMaxTimeInHours);

            var validUsers = _getIncompleteRegistrationStatusResponse.Users
                .Where(x => minDate>= x.DateCompleted && maxDate<= x.DateCompleted)
                .Select(x=> x.UserId)
                .ToList();

            var result = await _classUnderTest.IdentifyRecipients(null, null, null, null, null);
            Assert.AreEqual(validUsers, result.Select(x=> x.RecipientUserID).ToList());
        }
    }
}
