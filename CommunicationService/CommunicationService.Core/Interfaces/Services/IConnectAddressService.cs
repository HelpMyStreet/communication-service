using System.Threading.Tasks;
using HelpMyStreet.Contracts.AddressService.Request;
using HelpMyStreet.Contracts.AddressService.Response;
using HelpMyStreet.Utils.Enums;

namespace CommunicationService.Core.Interfaces.Services
{
    public interface IConnectAddressService
    {
        Task<GetPostcodeCoordinatesResponse> GetPostcodeCoordinates(GetPostcodeCoordinatesRequest getPostcodeCoordinatesRequest);

        Task<GetLocationResponse> GetLocationDetails(Location location);

        Task<GetLocationsByDistanceResponse> GetLocationsByDistance(string postCode, int maxDistance); 
    }
}
