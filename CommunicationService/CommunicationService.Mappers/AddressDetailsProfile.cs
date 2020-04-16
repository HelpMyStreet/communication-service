using AutoMapper;
using CommunicationService.Core.Dto;
using CommunicationService.Repo.EntityFramework.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Mappers
{
    public class AddressDetailsProfile : Profile
    {
        public AddressDetailsProfile()
        {
            CreateMap<AddressDetails, AddressDetailsDTO>();
            CreateMap<AddressDetailsDTO, AddressDetails>();
        }
    }
}
