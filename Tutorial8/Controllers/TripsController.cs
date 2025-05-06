using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NuGet.Protocol.Plugins;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var tripsDtos = new List<TripDTO>();

            await using var con =
                new SqlConnection(
                    "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;");
            await con.OpenAsync();
            
            var tripCmd = new SqlCommand("SELECT * FROM Trip", con);
            var reader = await tripCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
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
            
            foreach (var trip in tripsDtos)
            {
                var countryCmd = new SqlCommand(
                    "SELECT c.Name FROM Country_Trip ct JOIN Country c ON ct.IdCountry = c.IdCountry WHERE ct.IdTrip = @IdTrip",
                    con);
                countryCmd.Parameters.AddWithValue("@IdTrip", trip.Id);

                var countryReader = await countryCmd.ExecuteReaderAsync();

                while (await countryReader.ReadAsync())
                {
                    trip.Countries.Add(new CountryDTO
                    {
                        Name = (string)countryReader["Name"]
                    });
                }

                await countryReader.CloseAsync();
            }

            return Ok(tripsDtos);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrip(int id)
        {
            // if( await DoesTripExist(id)){
            //  return NotFound();
            // }
            // var trip = ... GetTrip(id);
            return Ok();
        }
    }
}