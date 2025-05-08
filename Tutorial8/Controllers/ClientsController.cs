using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _service;

        public ClientsController(IClientService service)
        {
            _service = service;
        }
        
        private string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";

        //pobieranie wycieczek danego klienta
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientsTrips(int id, CancellationToken cancellationToken)
        {
            var clientTrips = await _service.GetClientTrips(id, cancellationToken);
            if(!clientTrips.Any())
                return NotFound("No trips found");
            return Ok(clientTrips);
        }
        
        //tworzenie nowego rekordu klienta
         [HttpPost]
        public async Task<IActionResult> CreateClient(ClientDTO newClient, CancellationToken cancellationToken)
        {
            var newId = await _service.CreateClient(newClient, cancellationToken);
            return CreatedAtAction(nameof(GetClientsTrips), new { id = newId },"New client created with id: "+newId);
        }
        
        //rejestracja klienta na wycieczkę
        [HttpPut("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> RegisterClient(int idClient,int idTrip , CancellationToken cancellationToken)
        {
            var success = await _service.RegisterClientToTrip(idClient, idTrip, cancellationToken);
            if(!success)
                return Conflict("Could not register client to trip");
            return Ok("Client registered to trip");

        }

        //usunięcie rejestracji klienta
        [HttpDelete("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> DeleteClient(int idClient,int idTrip, CancellationToken cancellationToken)
        {
            var success = await _service.DeleteClientFromTrip(idClient, idTrip, cancellationToken);
            if(!success)
                return Conflict("Could not delete client from trip");
            
            return Ok("Client deleted from trip");
        }
        
    }
}
