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
        
        [HttpPut("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> RegisterClient(int idClient,int idTrip , CancellationToken cancellationToken)
        {
            await using var con = new SqlConnection(connectionString);
            await con.OpenAsync(cancellationToken);
            var checker = new SqlCommand("SELECT 1,MaxPeople FROM Client,Trip WHERE Client.IdClient = @idClient AND Trip.IdTrip=@idTrip ", con);
            checker.Parameters.AddWithValue("@idClient", idClient);
            checker.Parameters.AddWithValue("@idTrip", idTrip);

            var reader = await checker.ExecuteReaderAsync(cancellationToken);
            int maxPeople = 0;
            if (await reader.ReadAsync(cancellationToken))
            {
                maxPeople = (int)reader["MaxPeople"];
            }
            else
            {
                await reader.CloseAsync();
                return NotFound("Client or Trip not found");
            }
            await reader.CloseAsync();
            
            var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @idTrip", con);
            countCmd.Parameters.AddWithValue("@idTrip", idTrip);
            var currentCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken));
            if (currentCount >= maxPeople)
                return Conflict("Max participants reached for this trip");
            

            var alreadyRegistered = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @idClient AND IdTrip = @idTrip", con);
            alreadyRegistered.Parameters.AddWithValue("@idClient", idClient);
            alreadyRegistered.Parameters.AddWithValue("@idTrip", idTrip);
            if ((await alreadyRegistered.ExecuteScalarAsync(cancellationToken)) != null)
                return Conflict("Client is already registered for this trip");
            

            await using var adder = new SqlCommand(@"
                INSERT INTO Client_Trip (IdClient,IdTrip,RegisteredAt)
                VALUES (@IdClient,@IdTrip,@RegisteredAt)",con);
            adder.Parameters.AddWithValue("@IdClient", idClient);
            adder.Parameters.AddWithValue("@IdTrip", idTrip);
            var registeredAt = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));
            adder.Parameters.AddWithValue("@RegisteredAt", registeredAt);



            
            await adder.ExecuteNonQueryAsync(cancellationToken);
            
            return Ok("Client registered to trip");

        }

        [HttpDelete("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> DeleteClient(int idClient,int idTrip, CancellationToken cancellationToken)
        {
            await using var con = new SqlConnection(connectionString);
            var alreadyRegistered = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @idClient AND IdTrip = @idTrip", con);
            alreadyRegistered.Parameters.AddWithValue("@idClient", idClient);
            alreadyRegistered.Parameters.AddWithValue("@idTrip", idTrip);
            
            await con.OpenAsync(cancellationToken);
            
            if (await alreadyRegistered.ExecuteScalarAsync(cancellationToken) == null)
                return NotFound("Client is not registered for this trip");
            
            
            
            await using var deleter = new SqlCommand(@"
                DELETE FROM Client_Trip
                WHERE IdClient=@IdClient AND IdTrip=@IdTrip",con);
            deleter.Parameters.AddWithValue("@IdClient", idClient);
            deleter.Parameters.AddWithValue("@IdTrip", idTrip);
            
            await deleter.ExecuteNonQueryAsync(cancellationToken);
            
            return Ok("Client deleted");
        }
        
    }
}
