using AutoMapper;
using CommunicationService.Core;
using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.Core.Utils;
using CommunicationService.EmailService;
using CommunicationService.Handlers;
using CommunicationService.Mappers;
using CommunicationService.Repo;
using CommunicationService.UserService;
using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

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

            Dictionary<HttpClientConfigName, ApiConfig> httpClientConfigs = config.GetSection("Apis").Get<Dictionary<HttpClientConfigName, ApiConfig>>();

            foreach (KeyValuePair<HttpClientConfigName, ApiConfig> httpClientConfig in httpClientConfigs)
            {

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
                });
            }

            IConfigurationSection sendGridConfigSettings = config.GetSection("SendGridConfig");
            builder.Services.Configure<SendGridConfig>(sendGridConfigSettings);
            builder.Services.AddTransient<IHttpClientWrapper, HttpClientWrapper>();
            builder.Services.AddMediatR(typeof(SendEmailHandler).Assembly);
            builder.Services.AddAutoMapper(typeof(AddressDetailsProfile).Assembly);
            builder.Services.AddSingleton<ISendEmailService, SendEmailService>();
            builder.Services.AddSingleton<IConnectUserService, ConnectUserService>();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                   options.UseInMemoryDatabase(databaseName: "CommunicationService.AzureFunction"));
            builder.Services.AddTransient<IRepository, Repository>();
        }
    }
}