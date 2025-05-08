using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services
{
    public class TripsService : ITripsService
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";

        public async Task<IEnumerable<TripDTO>> GetTrips(CancellationToken cancellationToken)
        {
            var tripsDtos = new List<TripDTO>();

            await using var con = new SqlConnection(connectionString);
            await con.OpenAsync(cancellationToken);

            //pobieranie informacji zawartych w tabeli Trip
            var com = new SqlCommand("SELECT * FROM Trip", con);
            var reader = await com.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var tripDTO = new TripDTO
                {
                    Id = (int)reader["IdTrip"],
                    Name = (string)reader["Name"],
                    Description = (string)reader["Description"],
                    DateFrom = (DateTime)reader["DateFrom"],
                    DateTo = (DateTime)reader["DateTo"],
                    MaxPeople = (int)reader["MaxPeople"],
                    Countries = new List<CountryDTO>()
                };

                tripsDtos.Add(tripDTO);
            }

            await reader.CloseAsync();

            //dodawanie listy państw do wycieczki
            foreach (var trip in tripsDtos)
            {
                var countryCmd = new SqlCommand(
                    "SELECT c.Name FROM Country_Trip ct JOIN Country c ON ct.IdCountry = c.IdCountry WHERE ct.IdTrip = @IdTrip",
                    con);
                countryCmd.Parameters.AddWithValue("@IdTrip", trip.Id);

                var countryReader = await countryCmd.ExecuteReaderAsync(cancellationToken);

                while (await countryReader.ReadAsync(cancellationToken))
                {
                    trip.Countries.Add(new CountryDTO
                    {
                        Name = (string)countryReader["Name"]
                    });
                }

                await countryReader.CloseAsync();
            }

            return tripsDtos;
        }
    }
}