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
            await using var trips = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;");

            await using var com = new SqlCommand();
            com.Connection = trips;
            com.CommandText = "select * from Trip";

            await trips.OpenAsync();

            var reader = await com.ExecuteReaderAsync();
            
            var tripsDtos = new List<TripDTO>();
            while (await reader.ReadAsync())
            {
                var tripDTO = new TripDTO
                {
                    Id = (int)reader["Id"],
                    Name = (string)reader["Name"],
                    DateFrom = (string)reader["DateFrom"],
                    DateTo = (string)reader["DateTo"],
                    MaxPeople = (int)reader["MaxPeople"],
                };
                tripsDtos.Add(tripDTO);
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