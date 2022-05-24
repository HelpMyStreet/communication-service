using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Domains.SendGrid;
using CommunicationService.Core.Exception;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.SendGridManagement;
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

namespace CommunicationService.UnitTests.SendGridManagement
{
    public class EmailTemplateUploaderTests
    {
        private Mock<ISendGridClient> _sendGridClient;
        private Mock<ICosmosDbService> _cosmosDbService;
        private List<MigrationHistory> _history;
        private string[] _directoryFiles;
        private string _file;
        private Task<Response> _response;
        private Task<Response> _deleteResponse;
        private Templates _templates;

        private EmailTemplateUploader _classUnderTest;

        private void SetupSendGridClient()
        {
            _sendGridClient = new Mock<ISendGridClient>();

            Templates templates = new Templates();
            Template template = new Template()
            {
                id = "testTemplateId",
                name = "KnownTemplate"
            };
            templates.templates = new Template[1]
            {
                template
            };
            string responseBody = JsonConvert.SerializeObject(templates);
            _response = Task.FromResult(new Response(System.Net.HttpStatusCode.Created, new StringContent(responseBody), null));

            _sendGridClient.Setup(x => x.RequestAsync(
                SendGridClient.Method.GET,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                  .Returns(() => _response
                  );
          
            _deleteResponse = Task.FromResult(new Response(System.Net.HttpStatusCode.NoContent, new StringContent(string.Empty), null));
            _sendGridClient.Setup(x => x.RequestAsync(
                SendGridClient.Method.DELETE,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                  .Returns(() => _deleteResponse
                  );
        }

        private void SetupCosmosDbService()
        {
            _cosmosDbService = new Mock<ICosmosDbService>();
            _cosmosDbService.Setup(x => x.GetMigrationHistory())
                .Returns(()=>Task.FromResult(_history));
        }

        [SetUp]
        public void SetUp()
        {
            SetupCosmosDbService();
            SetupSendGridClient();

            _classUnderTest = new EmailTemplateUploader(_sendGridClient.Object,_cosmosDbService.Object);
        }

        [Test]
        public async Task WhenMoreThanThreeVersionsExist_DeleteOtherInactiveTemplates()
        {
            string templateId = "testTemplateId";
            string templateName = "KnownTemplate";
            _templates = new Templates();
            _templates.templates = new Template[1]
            {
                new Template()
                {
                    id = templateId,
                    name = templateName,
                    versions = new Core.Domains.SendGrid.Version[]
                    {
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 1,
                            updated_at = "2022-02-01",
                            id = "1"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 0,
                            updated_at = "2022-01-31",
                            id = "2"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 0,
                            updated_at = "2022-01-30",
                            id = "3"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 0,
                            updated_at = "2022-01-29",
                            id = "4"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = "testTemplateId",
                            active = 0,
                            updated_at = "2022-01-28",
                            id = "5"
                        }
                    }
                }
            };

            string responseBody = JsonConvert.SerializeObject(_templates);
            _response = Task.FromResult(new Response(System.Net.HttpStatusCode.OK, new StringContent(responseBody), null));

            await _classUnderTest.EnsureOnlyMaxTwoVersionsOfEmailsExist();
            _sendGridClient.Verify(x => x.RequestAsync(BaseClient.Method.DELETE, It.IsAny<string>(), It.IsAny<string>(), $"/templates/{templateId}/versions/1", It.IsAny<CancellationToken>()), Times.Exactly(0));
            _sendGridClient.Verify(x => x.RequestAsync(BaseClient.Method.DELETE, It.IsAny<string>(), It.IsAny<string>(), $"/templates/{templateId}/versions/2", It.IsAny<CancellationToken>()), Times.Exactly(0));
            _sendGridClient.Verify(x => x.RequestAsync(BaseClient.Method.DELETE, It.IsAny<string>(), It.IsAny<string>(), $"/templates/{templateId}/versions/3", It.IsAny<CancellationToken>()), Times.Exactly(0));
            _sendGridClient.Verify(x => x.RequestAsync(BaseClient.Method.DELETE, It.IsAny<string>(), It.IsAny<string>(), $"/templates/{templateId}/versions/4", It.IsAny<CancellationToken>()), Times.Exactly(1));
            _sendGridClient.Verify(x => x.RequestAsync(BaseClient.Method.DELETE, It.IsAny<string>(), It.IsAny<string>(), $"/templates/{templateId}/versions/5", It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public async Task WhenLessThanThreeVersionsExist_DoNotDeleteAnyVersions()
        {
            string templateId = "testTemplateId";
            string templateName = "KnownTemplate";
            _templates = new Templates();
            _templates.templates = new Template[1]
            {
                new Template()
                {
                    id = templateId,
                    name = templateName,
                    versions = new Core.Domains.SendGrid.Version[]
                    {
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 1,
                            updated_at = "2022-02-01"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 0,
                            updated_at = "2022-01-31"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 0,
                            updated_at = "2022-01-30"
                        }
                    }
                }
            };

            string responseBody = JsonConvert.SerializeObject(_templates);
            _response = Task.FromResult(new Response(System.Net.HttpStatusCode.OK, new StringContent(responseBody), null));

            await _classUnderTest.EnsureOnlyMaxTwoVersionsOfEmailsExist();
            _sendGridClient.Verify(x => x.RequestAsync(BaseClient.Method.DELETE, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
        }

        [Test]
        public async Task WhenMoreThanThreeVersionsExistButAllAreActive_DoNotDeleteAnyVersions()
        {
            string templateId = "testTemplateId";
            string templateName = "KnownTemplate";
            _templates = new Templates();
            _templates.templates = new Template[1]
            {
                new Template()
                {
                    id = templateId,
                    name = templateName,
                    versions = new Core.Domains.SendGrid.Version[]
                    {
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 1,
                            updated_at = "2022-02-01"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 1,
                            updated_at = "2022-01-31"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 1,
                            updated_at = "2022-01-30"
                        },                        
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 1,
                            updated_at = "2022-01-29"
                        },
                        new Core.Domains.SendGrid.Version()
                        {
                            template_id = templateId,
                            active = 1,
                            updated_at = "2022-01-28"
                        }
                    }
                }
            };

            string responseBody = JsonConvert.SerializeObject(_templates);
            _response = Task.FromResult(new Response(System.Net.HttpStatusCode.OK, new StringContent(responseBody), null));

            await _classUnderTest.EnsureOnlyMaxTwoVersionsOfEmailsExist();
            _sendGridClient.Verify(x => x.RequestAsync(BaseClient.Method.DELETE, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
        }
    }
}
