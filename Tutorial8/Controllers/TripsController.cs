using Microsoft.AspNetCore.Mvc;
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

        private string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";


        //pobieranie wszysktich wycieczek z ich informacjami
        [HttpGet("/api/trips")]
        public async Task<IActionResult> GetTrips(CancellationToken cancellationToken)
        {
            var trips = await _tripsService.GetTrips(cancellationToken);
            return Ok(trips);
        }
    }
}