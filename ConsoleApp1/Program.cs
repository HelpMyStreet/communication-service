using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.GroupService;
using CommunicationService.MessageService;
using Microsoft.Extensions.Options;
using System;

namespace ConsoleApp1
{
    public class Program
    {
        public void Main(string[] args)
        {
            IConnectGroupService _connectGroupService;
            IConnectUserService _connectUserService;
            IConnectRequestService _connectRequestService;
            IOptions<EmailConfig> _emailConfig;

            _connectGroupService = new ConnectGroupService()
            {

            }

            DailyDigestMessage dailyDigestMessage = new DailyDigestMessage
                (
                    _connectGroupService,
                    _connectUserService,
                    _connectRequestService,
                    _emailConfig
                    );

            Console.WriteLine("Hello World!");
        }
    }
}
