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

        [SetUp]
        public void SetUp()
        {
            SetupCosmosDbService();
            SetupSendGridClient();

            _classUnderTest = new EmailTemplateUploader(_sendGridClient.Object,_cosmosDbService.Object);
        }

        //[Test]
        //public async Task GetTemplateId_ReturnsID_WhenTemplateNameIsKnown()
        //{
        //    _history = new List<MigrationHistory>();
        //    await _classUnderTest.Migrate();
        //}
    }
}
