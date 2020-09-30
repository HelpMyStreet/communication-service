using System;
using System.Threading.Tasks;
using CommunicationService.Core.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CommunicationService.AzureFunction
{
    public class TimedPurgeExpiredLinks
    {
        private readonly IPurgeService _purgeService;

        public TimedPurgeExpiredLinks(IPurgeService purgeService)
        {
            _purgeService = purgeService;
        }

        [FunctionName("TimedPurgeExpiredLinks")]
        public async Task Run([TimerTrigger("%TimedPurgeExpiredLinksExpression%")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Purge Expired Links Timer trigger function executed at: {DateTime.Now}");
            await _purgeService.PurgeExpiredLinks();
        }
    }
}
