using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";

        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientsTrips(int id, CancellationToken cancellationToken)
        {
            await using var con = new SqlConnection(connectionString);
            await con.OpenAsync(cancellationToken);

            var checker = new SqlCommand("SELECT FirstName,LastName FROM Client WHERE IdClient = @id", con);
            checker.Parameters.AddWithValue("@id", id);

            var reader = await checker.ExecuteReaderAsync(cancellationToken);
            if (!reader.HasRows)
            {
                await reader.CloseAsync();
                return NotFound("Client not found");
            }
            await reader.CloseAsync();

            var trips = new List<TripDTO>();
            var request = new SqlCommand(@"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                       ct.RegisteredAt, ct.PaymentDate
                FROM Client_Trip ct
                JOIN Trip t ON ct.IdTrip = t.IdTrip
                WHERE ct.IdClient = @Id", con);
            request.Parameters.AddWithValue("@Id", id);

            var fullReader = await request.ExecuteReaderAsync(cancellationToken);

            while (await fullReader.ReadAsync(cancellationToken))
            {
                trips.Add(new TripDTO
                {
                    Id = (int)fullReader["IdTrip"],
                    Name = (string)fullReader["Name"],
                    Description = (string)fullReader["Description"],
                    DateFrom = (DateTime)fullReader["DateFrom"],
                    DateTo = (DateTime)fullReader["DateTo"],
                    MaxPeople = (int)fullReader["MaxPeople"],
                    RegisteredAt = (int)fullReader["RegisteredAt"],
                    PaymentDate = fullReader["PaymentDate"] == DBNull.Value ? null : (int?)fullReader["PaymentDate"]
                });
            }

            await fullReader.CloseAsync();

            if (trips.Count == 0)
                return NotFound("No trips found");

            return Ok(trips);
        }
        
         [HttpPost]
        public async Task<IActionResult> CreateClient(ClientDTO newClient, CancellationToken cancellationToken)
        {
            await using var con = new SqlConnection(connectionString);
            await using var com = new SqlCommand(@"
                    INSERT INTO Client (FirstName,LastName,Email,Telephone,Pesel) 
                    VALUES (@FirstName,@LastName,@Email,@Telephone,@Pesel);
                    SELECT SCOPE_IDENTITY()", con);
            com.Parameters.AddWithValue("@FirstName", newClient.FirstName);
            com.Parameters.AddWithValue("@LastName", newClient.LastName);
            com.Parameters.AddWithValue("@Email", newClient.Email);
            com.Parameters.AddWithValue("@Telephone", newClient.Telephone);
            com.Parameters.AddWithValue("@Pesel", newClient.Pesel);
            
            await con.OpenAsync(cancellationToken);
            
            var result = await com.ExecuteScalarAsync(cancellationToken);
            
            return Ok("New client created with id: "+result);
        }
        
        
    }
}
