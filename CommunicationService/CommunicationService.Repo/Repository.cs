using AutoMapper;
using CommunicationService.Core.Interfaces.Repositories;

namespace CommunicationService.Repo
{
    public class Repository : IRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public Repository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }        
    }
}
