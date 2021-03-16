using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.AddressService.Request;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectAddressService
    {
        Task<GetPostcodeCoordinatesResponse> GetPostcodeCoordinates(GetPostcodeCoordinatesRequest getPostcodeCoordinatesRequest);

        Task<LocationDetails> GetLocationDetails(Location location, CancellationToken cancellationToken);

        Task<GetLocationsByDistanceResponse> GetLocationsByDistance(string postCode, int maxDistance);        
    }
}
