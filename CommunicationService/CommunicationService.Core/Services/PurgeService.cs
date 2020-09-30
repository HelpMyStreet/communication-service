using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.Core.Services
{
    public class PurgeService : IPurgeService
    {
        private readonly ILinkRepository _linkRepository;

        public PurgeService(ILinkRepository linkRepository)
        {
            _linkRepository = linkRepository;
        }

        public async Task PurgeExpiredLinks()
        {
            var links = await _linkRepository.GetExpiredLinksAsync();

            if(links!=null && links.Count()>0)
            {
                foreach (Links l in links)
                {
                    await _linkRepository.DeleteLink(l.id);

                }
            }
        }
    }
}
