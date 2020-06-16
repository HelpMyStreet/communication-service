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
        private Mock<IDirectoryService> _directoryService;
        private List<MigrationHistory> _history;
        private string[] _directoryFiles;
        private string _file;
        private Task<Response> _response;

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
                It.IsAny<SendGridClient.Method>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                  .Returns(() => _response
                  );

        }

        private void SetupCosmosDbService()
        {
            _cosmosDbService = new Mock<ICosmosDbService>();
            _cosmosDbService.Setup(x => x.GetMigrationHistory())
                .Returns(()=>Task.FromResult(_history));
        }

        private void SetupDirectoryService()
        {
            _directoryFiles = new string[1];
            _directoryFiles[0] = "test.html";
            _directoryService = new Mock<IDirectoryService>();
            _directoryService.Setup(x => x.GetFiles(It.IsAny<string>()))
                .Returns(() => _directoryFiles);

            Templates templates = new Templates();
            Template template = new Template()
            {
                name = "KnownTemplate",
                subject = "test"
            };
            templates.templates = new Template[1]
            {
                template
            };
            _file = JsonConvert.SerializeObject(templates);
            _directoryService.Setup(x => x.ReadAllText(It.IsAny<string>()))
                .Returns(() => _file);
        }

        [SetUp]
        public void SetUp()
        {
            SetupCosmosDbService();
            SetupSendGridClient();
            SetupDirectoryService();

            _classUnderTest = new EmailTemplateUploader(_sendGridClient.Object,_cosmosDbService.Object,_directoryService.Object, string.Empty);
        }

        //[Test]
        //public async Task GetTemplateId_ReturnsID_WhenTemplateNameIsKnown()
        //{
        //    _history = new List<MigrationHistory>();
        //    await _classUnderTest.Migrate();
        //}
    }
}
