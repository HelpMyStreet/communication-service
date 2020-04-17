using AutoMapper;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Handlers;
using CommunicationService.Mappers;
using CommunicationService.Repo;
using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

[assembly: FunctionsStartup(typeof(CommunicationService.AzureFunction.Startup))]
namespace CommunicationService.AzureFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddMediatR(typeof(SendEmailHandler).Assembly);
            builder.Services.AddAutoMapper(typeof(AddressDetailsProfile).Assembly);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                   options.UseInMemoryDatabase(databaseName: "CommunicationService.AzureFunction"));
            builder.Services.AddTransient<IRepository, Repository>();
        }
    }
}