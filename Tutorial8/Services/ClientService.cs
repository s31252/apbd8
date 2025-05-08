using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientService : IClientService
{
    private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";
    
    
    
    public async Task<IEnumerable<TripDTO>> GetClientTrips(int id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(connectionString);
            await con.OpenAsync(cancellationToken);

            //pobieranie wycieczek danego klienta
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
            return trips;
    }

    public async Task<bool> RegisterClientToTrip(int idClient, int idTrip, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(connectionString);
            await con.OpenAsync(cancellationToken);
            //sprawdzenie czy klient i wycieczka istnieją oraz pobranie wartości MaxPeople do pózniejszego sprawdzenia
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
                return false;
            }
            await reader.CloseAsync();
            
            //sprawdzenie czy nie ma więcej klientów w tabeli Client_Trip dla danej wycieczki niż MaxPeople  
            var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @idTrip", con);
            countCmd.Parameters.AddWithValue("@idTrip", idTrip);
            var currentCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken));
            if (currentCount >= maxPeople)
                return false;
            
            //sprawdzenie czy klient nie jest już zarejestrowany na tą wycieczkę
            var alreadyRegistered = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @idClient AND IdTrip = @idTrip", con);
            alreadyRegistered.Parameters.AddWithValue("@idClient", idClient);
            alreadyRegistered.Parameters.AddWithValue("@idTrip", idTrip);
            if (await alreadyRegistered.ExecuteScalarAsync(cancellationToken) != null)
                return false;
            
            //wstawienie rekordu do tabeli Client_Trip
            await using var adder = new SqlCommand(@"
                INSERT INTO Client_Trip (IdClient,IdTrip,RegisteredAt)
                VALUES (@IdClient,@IdTrip,@RegisteredAt)",con);
            adder.Parameters.AddWithValue("@IdClient", idClient);
            adder.Parameters.AddWithValue("@IdTrip", idTrip);
            var registeredAt = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));
            adder.Parameters.AddWithValue("@RegisteredAt", registeredAt);

            
            await adder.ExecuteNonQueryAsync(cancellationToken);
            return true;
            
    }

    public async Task<bool> DeleteClientFromTrip(int idClient, int idTrip, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(connectionString);
        //sprawdzenie czy istnieje taka wycieczka i klient
        var alreadyRegistered = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @idClient AND IdTrip = @idTrip", con);
        alreadyRegistered.Parameters.AddWithValue("@idClient", idClient);
        alreadyRegistered.Parameters.AddWithValue("@idTrip", idTrip);
            
        await con.OpenAsync(cancellationToken);

        if (await alreadyRegistered.ExecuteScalarAsync(cancellationToken) == null)
            return false;
            
        //usunięcie rekordu z tabeli Client_Trip
        await using var deleter = new SqlCommand(@"
                DELETE FROM Client_Trip
                WHERE IdClient=@IdClient AND IdTrip=@IdTrip",con);
        deleter.Parameters.AddWithValue("@IdClient", idClient);
        deleter.Parameters.AddWithValue("@IdTrip", idTrip);
            
        await deleter.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<int> CreateClient(ClientDTO newClient, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(connectionString);
        //wstawianie do tabeli Client
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
        return (int)result;
    }
}