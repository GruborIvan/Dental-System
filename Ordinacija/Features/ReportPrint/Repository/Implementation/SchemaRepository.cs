using Dapper;
using Microsoft.Extensions.Configuration;
using Ordinacija.Features.ReportPrint.Exceptions;
using Microsoft.Data.SqlClient;

namespace Ordinacija.Features.ReportPrint.Repository.Implementation
{
    public class SchemaRepository : ISchemaRepository
    {
        private readonly string _connectionString;

        public SchemaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PantheonDB") ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<string> GetTemplateSchemaByKey(string schemaKey)
        {
            string query = $"SELECT SchemaValue FROM SchemaTable WHERE SchemaName = '{schemaKey}'";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var schema = await connection.QuerySingleOrDefaultAsync<string>(query);

            if (schema == null)
            {
                throw new SchemaNotFoundException(schemaKey);
            }

            return schema;
        }
    }
}
