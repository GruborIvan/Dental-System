using Microsoft.Extensions.Configuration;
using Ordinacija.Features.Login.Models;
using Microsoft.Data.SqlClient;

namespace Ordinacija.Features.Login.Repository.Implementation
{
    public class LoginRepository : ILoginRepository
    {
        private readonly string _server;
        private readonly string _database;

        public LoginRepository(IConfiguration configuration) 
        {
            var connectionString = configuration.GetConnectionString("PantheonDB")
                               ?? throw new ArgumentNullException(nameof(configuration));

            var builder = new SqlConnectionStringBuilder(connectionString);
            _server = builder.DataSource;
            _database = builder.InitialCatalog;
        }

        public async Task<bool> AuthenticateUser(LoginCredentials loginCredentials)
        {
            var dynamicConnectionString = $"Server={_server};Database={_database};User Id={loginCredentials.Username};Password={loginCredentials.Password};TrustServerCertificate=True;";

            try
            {
                using var connection = new SqlConnection(dynamicConnectionString);
                await connection.OpenAsync(); 
                return true; 
            }
            catch (SqlException ex) when (IsLoginRejected(ex))
            {
                return false;
            }
        }

        // Only errors where the server answered and refused the login count as wrong credentials.
        // Anything else (server unreachable, timeout) is thrown so the caller can report it as such.
        private static bool IsLoginRejected(SqlException ex) => ex.Number is
            18456    // login failed (wrong username or password)
            or 18452 // login from an untrusted domain
            or 18470 // account disabled
            or 18486 // account locked out
            or 18487 // password expired
            or 18488 // password must be changed
            or 4060  // cannot open the database requested by the login
            or 4064; // cannot open the user's default database
    }
}
