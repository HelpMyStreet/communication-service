using AutoMapper;
using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.EmailService;
using CommunicationService.GroupService;
using CommunicationService.Handlers;
using CommunicationService.Mappers;
using CommunicationService.MessageService;
using CommunicationService.Repo;
using CommunicationService.RequestService;
using CommunicationService.SendGridService;
using CommunicationService.UserService;
using HelpMyStreet.Utils.PollyPolicies;
using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Utils;
using CommunicationService.AddressService;
using CommunicationService.Core.Services;
using UserService.Core.Utils;

[assembly: FunctionsStartup(typeof(CommunicationService.AzureFunction.Startup))]
namespace CommunicationService.AzureFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // We need to get the app directory this way.  Using Environment.CurrentDirectory doesn't work in Azure
            ExecutionContextOptions executioncontextoptions = builder.Services.BuildServiceProvider()
                .GetService<IOptions<ExecutionContextOptions>>().Value;
            string currentDirectory = executioncontextoptions.AppDirectory;

            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot config = configBuilder.Build();

            // DI doesn't work in startup
            PollyHttpPolicies pollyHttpPolicies = new PollyHttpPolicies(new PollyHttpPoliciesConfig());

            Dictionary<HttpClientConfigName, ApiConfig> httpClientConfigs = config.GetSection("Apis").Get<Dictionary<HttpClientConfigName, ApiConfig>>();

            foreach (KeyValuePair<HttpClientConfigName, ApiConfig> httpClientConfig in httpClientConfigs)
            {
                IAsyncPolicy<HttpResponseMessage> retryPolicy = httpClientConfig.Value.IsExternal ? pollyHttpPolicies.ExternalHttpRetryPolicy : pollyHttpPolicies.InternalHttpRetryPolicy;

                builder.Services.AddHttpClient(httpClientConfig.Key.ToString(), c =>
                {
                    c.BaseAddress = new Uri(httpClientConfig.Value.BaseAddress);

                    c.Timeout = httpClientConfig.Value.Timeout ?? new TimeSpan(0, 0, 0, 15);

                    foreach (KeyValuePair<string, string> header in httpClientConfig.Value.Headers)
                    {
                        c.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    c.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    c.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

                }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    MaxConnectionsPerServer = httpClientConfig.Value.MaxConnectionsPerServer ?? 15,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                }).AddPolicyHandler(retryPolicy);
            }

            IConfigurationSection emailConfigSettings = config.GetSection("EmailConfig");
            builder.Services.Configure<EmailConfig>(emailConfigSettings);

            IConfigurationSection serviceBusConfigSettings = config.GetSection("ServiceBusConfig");
            builder.Services.Configure<ServiceBusConfig>(serviceBusConfigSettings);
            ServiceBusConfig serviceBusConfig = serviceBusConfigSettings.Get<ServiceBusConfig>();

            IConfigurationSection sendGridConfigSettings = config.GetSection("SendGridConfig");
            builder.Services.Configure<SendGridConfig>(sendGridConfigSettings);
            var sendGridConfig = config.GetSection("SendGridConfig").Get<SendGridConfig>();
            builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(sendGridConfig.ApiKey));

            builder.Services.AddTransient<IHttpClientWrapper, HttpClientWrapper>();
            builder.Services.AddMediatR(typeof(SendEmailHandler).Assembly);
            builder.Services.AddAutoMapper(typeof(AddressDetailsProfile).Assembly);
            builder.Services.AddSingleton<IQueueClient>(new QueueClient(serviceBusConfig.ConnectionString, serviceBusConfig.MessageQueueName));

            builder.Services.AddSingleton<IMessageFactory, MessageFactory>();
            //builder.Services.AddSingleton<IConnectSendGridService, SendGridService>();
            builder.Services.AddSingleton<ISendEmailService, SendEmailService>();
            builder.Services.AddSingleton<IConnectUserService, ConnectUserService>();
            builder.Services.AddSingleton<IConnectRequestService, ConnectRequestService>();
            builder.Services.AddSingleton<IConnectGroupService, ConnectGroupService>();
            builder.Services.AddTransient<IJobFilteringService, JobFilteringService>();
            builder.Services.AddSingleton<IConnectAddressService, ConnectAddressService>();
            builder.Services.AddSingleton<IConnectSendGridService, ConnectSendGridService>();
            builder.Services.AddSingleton<IDistanceCalculator, DistanceCalculator>();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                   options.UseInMemoryDatabase(databaseName: "CommunicationService.AzureFunction"));
            builder.Services.AddTransient<IRepository, Repository>();

            CosmosConfig cosmosConfig = config.GetSection("CosmosConfig").Get<CosmosConfig>();
            builder.Services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstance(cosmosConfig));

            SendGridManagement.EmailTemplateUploader emailTemplateUploader =
                new SendGridManagement.EmailTemplateUploader(new SendGridClient(sendGridConfig.ApiKey), InitializeCosmosClientInstance(cosmosConfig));

            emailTemplateUploader.Migrate().ConfigureAwait(false);
        }

        private static CosmosDbService InitializeCosmosClientInstance(CosmosConfig cosmosConfig)
        {
            Microsoft.Azure.Cosmos.Fluent.CosmosClientBuilder clientBuilder = new Microsoft.Azure.Cosmos.Fluent.CosmosClientBuilder(cosmosConfig.ConnectionString);
            Microsoft.Azure.Cosmos.CosmosClient client = clientBuilder
                                .WithConnectionModeDirect()
                                .Build();
            CosmosDbService cosmosDbService = new CosmosDbService(client, cosmosConfig.DatabaseName, cosmosConfig.ContainerName);
            //Microsoft.Azure.Cosmos.DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            //await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            return cosmosDbService;
        }
    }
}