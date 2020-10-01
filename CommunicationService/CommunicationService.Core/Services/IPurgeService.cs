using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Utils.Dtos;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Core.Services
{
    public interface IPurgeService
    {
        Task PurgeExpiredLinks();
    }
}
