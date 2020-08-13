using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Exception;
using CommunicationService.SendGridService;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.UnitTests.SendGridService
{
    public class EmailTemplateUploaderTests
    {
        private Mock<IOptions<SendGridConfig>> _sendGridConfig;
        private Mock<ISendGridClient> _sendGridClient;
        private SendGridConfig _sendGridConfigSettings;
        private ConnectSendGridService _classUnderTest;
        private Task<Response> _templatesResponse;
        private Task<Response> _groupsResponse;
        private string _templateId;
        private Task<Response> _sendEmailResponse;

        [SetUp]
        public void SetUp()
        {
            _sendGridConfigSettings = new SendGridConfig()
            {
                ApiKey = "TestKey",
                FromName = "Test Name",
                FromEmail = "Test Email"
            };

            _sendGridConfig = new Mock<IOptions<SendGridConfig>>();
            _sendGridConfig.SetupGet(x => x.Value).Returns(_sendGridConfigSettings);
            _sendGridClient = new Mock<ISendGridClient>();
            _templateId = "testTemplateId";

            Templates templates = new Templates();
            Template template = new Template()
            {
                id = _templateId,
                name = "KnownTemplate"
            };
            templates.templates = new Template[1]
            {
                template
            };
            string responseBody = JsonConvert.SerializeObject(templates);
            _templatesResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.OK, new StringContent(responseBody), null));

            _sendGridClient.Setup(x => x.RequestAsync(
                It.IsAny<SendGridClient.Method>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(),
               It.Is<String>(s => s.Contains("templates")),
                It.IsAny<CancellationToken>()))
                  .Returns(() => _templatesResponse
                  );

            UnsubscribeGroups[] groups = new UnsubscribeGroups[1];
            groups[0] = new UnsubscribeGroups()
            {
                id = 1,
                name = "KnownGroup",
                description = "KnownGroup description"
            };
            string groupsResponseBody = JsonConvert.SerializeObject(groups);
            _groupsResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.OK, new StringContent(groupsResponseBody), null));

            _sendGridClient.Setup(x => x.RequestAsync(
                It.IsAny<SendGridClient.Method>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<String>(s => s.Contains("groups")),
                It.IsAny<CancellationToken>()))
                  .Returns(() => _groupsResponse
                  );


            _sendGridClient.Setup(x => x.SendEmailAsync(
                It.IsAny<SendGridMessage>(),
                It.IsAny<CancellationToken>()
                )).Returns(() => _sendEmailResponse);

            _classUnderTest = new ConnectSendGridService(_sendGridConfig.Object,_sendGridClient.Object);
        }

        [Test]
        public async Task GetTemplateId_ReturnsID_WhenTemplateNameIsKnown()
        {
            var templateId = await _classUnderTest.GetTemplateId("KnownTemplate");
            Assert.AreEqual(_templateId, templateId);
        }

        [Test]
        public void GetTemplateId_ThrowsException_WhenTemplateNameIsUnknown()
        {
            _templatesResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.OK, new StringContent(string.Empty), null));

            UnknownTemplateException ex = Assert.ThrowsAsync<UnknownTemplateException>(async () => await _classUnderTest.GetTemplateId("UnknownTemplate"));
            Assert.AreEqual("No templates found", ex.Message);
        }

        [Test]
        public void GetTemplateId_ThrowsExceptionTemplateNotFound_WhenTemplateNameIsUnknown()
        {
            string templateName = "UnknownTemplate";
            UnknownTemplateException ex = Assert.ThrowsAsync<UnknownTemplateException>(async () => await _classUnderTest.GetTemplateId(templateName));
            Assert.AreEqual($"{templateName} cannot be found in templates", ex.Message);
        }

        [Test]
        public void GetTemplateId_ThrowsSendGridException_WhenTemplateNameIsUnknown()
        {
            _templatesResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.BadRequest,new StringContent(string.Empty), null));

            Assert.ThrowsAsync<SendGridException>(async () => await _classUnderTest.GetTemplateId("UnknownTemplate"));
        }

        [Test]
        public async Task SendDynamicEmail_ReturnsTrue_WhenEmailSentAndOkReturned()
        {
            _sendEmailResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.OK,new StringContent(string.Empty),null));

            bool success = await _classUnderTest.SendDynamicEmail("messageId","KnownTemplate","KnownGroup",new Core.Domains.EmailBuildData());
            Assert.AreEqual(true, success);
        }

        [Test]
        public async Task SendDynamicEmail_ReturnsTrue_WhenEmailSentAndAcceptedReturned()
        {
            _sendEmailResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.Accepted, new StringContent(string.Empty), null));

            bool success = await _classUnderTest.SendDynamicEmail("messageId", "KnownTemplate", "KnownGroup", new Core.Domains.EmailBuildData());
            Assert.AreEqual(true, success);
        }

        [Test]
        public async Task SendDynamicEmail_ReturnsFalse_WhenEmailNotSent()
        {
            _sendEmailResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.BadRequest, new StringContent(string.Empty), null));

            bool success = await _classUnderTest.SendDynamicEmail("messageId", "KnownTemplate", "KnownGroup", new Core.Domains.EmailBuildData());
            Assert.AreEqual(false, success);
        }

    }
}
