using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientService
{
    Task<IEnumerable<TripDTO>> GetClientTrips(int id, CancellationToken cancellationToken);
    Task<bool> RegisterClientToTrip(int idTrip, int idClient, CancellationToken cancellationToken);
    Task<bool> DeleteClientFromTrip(int idClient, int idTrip, CancellationToken cancellationToken);
    Task<int> CreateClient(ClientDTO client, CancellationToken cancellationToken);
}