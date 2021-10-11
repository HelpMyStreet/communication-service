using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Exception;
using CommunicationService.SendGridService;
using HelpMyStreet.Cache;
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
        private Mock<IMemDistCache<Template>> _memCacheTemplate;
        private Mock<IMemDistCache<UnsubscribeGroup>> _memCacheUnsubscribeGroup;
        private SendGridConfig _sendGridConfigSettings;
        private ConnectSendGridService _classUnderTest;
        private Task<Response> _templatesResponse;
        private Task<Response> _groupsResponse;
        private string _templateId;
        private Task<Response> _sendEmailResponse;
        private Template _template;
        private UnsubscribeGroup _unsubscribeGroup;

        [SetUp]
        public void SetUp()
        {
            _sendGridConfigSettings = new SendGridConfig()
            {
                ApiKey = "TestKey",
                FromName = "Test Name",
                FromEmail = "Test Email",
                BaseUrl = "Base Url"
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

            _memCacheTemplate = new Mock<IMemDistCache<Template>>();
            _template = template;

            _memCacheTemplate.Setup(x => x.GetCachedDataAsync(
                It.IsAny<Func<CancellationToken, Task<Template>>>(), 
                It.IsAny<string>(), It.IsAny<RefreshBehaviour>(),
                It.IsAny<CancellationToken>(), 
                It.IsAny<NotInCacheBehaviour>(),
                It.IsAny<Func<DateTimeOffset, DateTimeOffset>>()))
                .ReturnsAsync(() => _template);

            _memCacheUnsubscribeGroup = new Mock<IMemDistCache<UnsubscribeGroup>>();

            _unsubscribeGroup = new UnsubscribeGroup() { id = -1 };

            _memCacheUnsubscribeGroup.Setup(x => x.GetCachedDataAsync(
                It.IsAny<Func<CancellationToken, Task<UnsubscribeGroup>>>(),
                It.IsAny<string>(), It.IsAny<RefreshBehaviour>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<NotInCacheBehaviour>(),
                It.IsAny<Func<DateTimeOffset, DateTimeOffset>>()))                
                .ReturnsAsync(() => _unsubscribeGroup);


            UnsubscribeGroup[] groups = new UnsubscribeGroup[1];
            groups[0] = new UnsubscribeGroup()
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

            _classUnderTest = new ConnectSendGridService(_sendGridConfig.Object,_sendGridClient.Object, _memCacheTemplate.Object, _memCacheUnsubscribeGroup.Object);
        }

        [Test]
        public async Task GetTemplate_ReturnsTemplate_WhenTemplateNameIsKnown()
        {
            var template = await _classUnderTest.GetTemplate("KnownTemplate");
            Assert.AreEqual(_templateId, template.id);
        }

        [Test]
        public void GetTemplateId_ThrowsException_WhenTemplateNameIsUnknown()
        {
            _templatesResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.OK, new StringContent(string.Empty), null));

            UnknownTemplateException ex = Assert.ThrowsAsync<UnknownTemplateException>(async () => await _classUnderTest.GetTemplate("UnknownTemplate"));
            Assert.AreEqual("No templates found", ex.Message);
        }

        [Test]
        public void GetTemplateId_ThrowsExceptionTemplateNotFound_WhenTemplateNameIsUnknown()
        {
            string templateName = "UnknownTemplate";
            UnknownTemplateException ex = Assert.ThrowsAsync<UnknownTemplateException>(async () => await _classUnderTest.GetTemplate(templateName));
            Assert.AreEqual($"{templateName} cannot be found in templates", ex.Message);
        }

        [Test]
        public void GetTemplateId_ThrowsSendGridException_WhenTemplateNameIsUnknown()
        {
            _templatesResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.BadRequest,new StringContent(string.Empty), null));

            Assert.ThrowsAsync<SendGridException>(async () => await _classUnderTest.GetTemplate("UnknownTemplate"));
        }

        [Test]
        public async Task SendDynamicEmail_ReturnsTrue_WhenEmailSentAndOkReturned()
        {
            _sendEmailResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.OK,new StringContent(string.Empty),null));

            bool success = await _classUnderTest.SendDynamicEmail("messageId","KnownTemplate","KnownGroup",
                new Core.Domains.EmailBuildData()
                {
                    BaseDynamicData = new Core.Domains.BaseDynamicData()
                });
            Assert.AreEqual(true, success);
        }

        [Test]
        public async Task SendDynamicEmail_ReturnsTrue_WhenEmailSentAndAcceptedReturned()
        {
            _sendEmailResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.Accepted, new StringContent(string.Empty), null));

            bool success = await _classUnderTest.SendDynamicEmail("messageId", "KnownTemplate", "KnownGroup", 
                new Core.Domains.EmailBuildData()
                {
                    BaseDynamicData = new Core.Domains.BaseDynamicData()
                });
            Assert.AreEqual(true, success);
        }

        [Test]
        public async Task SendDynamicEmail_ReturnsFalse_WhenEmailNotSent()
        {
            _sendEmailResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.BadRequest, new StringContent(string.Empty), null));

            bool success = await _classUnderTest.SendDynamicEmail("messageId", "KnownTemplate", "KnownGroup", new Core.Domains.EmailBuildData()
            {
                BaseDynamicData = new Core.Domains.BaseDynamicData()
            });
            Assert.AreEqual(false, success);
        }

    }
}
