using AutoMapper;
using CommunicationService.Core.Dto;
using CommunicationService.Repo.EntityFramework.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Mappers
{
    public class PostCodeProfile : Profile
    {
        public PostCodeProfile()
        {
            CreateMap<PostCode, PostCodeDTO>()
                .ForMember(dest => dest.AddressDetails, opt => opt.Ignore());
            CreateMap<PostCodeDTO, PostCode>()
                .ForMember(dest => dest.AddressDetails, opt => opt.Ignore());
        }
    }
}
