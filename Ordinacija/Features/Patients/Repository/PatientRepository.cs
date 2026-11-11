using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Ordinacija.Features.Patients.Models;
using Microsoft.Data.SqlClient;

namespace Ordinacija.Features.Patients.Repository
{
    public class PatientRepository : IPatientRepository
    {
        private readonly string _connectionString;
        private readonly IMapper _mapper;

        public PatientRepository(IConfiguration configuration, IMapper mapper)
        {
            _connectionString = configuration.GetConnectionString("PantheonDB") ?? throw new ArgumentNullException(nameof(configuration));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<IEnumerable<Patient>> GetAllPatients()
        {
            const string query = @"
                               SELECT * 
                               FROM THE_SetSubj
                               WHERE acSupplier = 'F'";

            using var connection = CreateConnection();
            var patients = await connection.QueryAsync<PatientDbo>(query);
            return _mapper.Map<List<Patient>>(patients);
        }

        public async Task InsertPatient(Patient patient)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string sql = @"
                    INSERT INTO tHE_SetSubj (AcSubject, AcName2, AcAddress, AdBirthDate, AcFieldSA, AcFieldSC, AcFieldSD, AcFieldSE, AcFieldSF, AcFieldSG, AcFieldSH, AcFieldSI, AcSupplier)
                    VALUES (@AcSubject, @AcName2, @AcAddress, @AdBirthDate, @AcFieldSA, @AcFieldSC, @AcFieldSD, @AcFieldSE, @AcFieldSF, @AcFieldSG, @AcFieldSH, @AcFieldSI, 'F')
                ";

                var patientDbo = _mapper.Map<PatientDbo>(patient);
                patientDbo.AcSubject = await GenerateNextPatientId(transaction, connection);

                await connection.ExecuteAsync(sql, patientDbo, transaction);
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdatePatient(Patient patient)
        {
            var patientDbo = _mapper.Map<PatientDbo>(patient);

            const string sql = @"
            UPDATE tHE_SetSubj
            SET AcName2 = @AcName2, 
                AcAddress = @AcAddress,
                AdBirthDate = @AdBirthDate,
                AcFieldSA = @AcFieldSA,
                AcFieldSC = @AcFieldSC,
                AcFieldSD = @AcFieldSD,
                AcFieldSE = @AcFieldSE,
                AcFieldSF = @AcFieldSF,
                AcFieldSG = @AcFieldSG,
                AcFieldSH = @AcFieldSH,
                AcFieldSI = @AcFieldSI
            WHERE AcSubject = @AcSubject";

            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, patientDbo);

            if (rowsAffected == 0)
            {
                throw new KeyNotFoundException($"Patient with AcSubject {patientDbo.AcSubject} not found.");
            }
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        private async Task<string> GenerateNextPatientId(SqlTransaction transaction, SqlConnection connection)
        {
            const string sql = "SELECT MAX(AcSubject) FROM tHE_SetSubj WITH (UPDLOCK, ROWLOCK)";

            var lastId = await connection.ExecuteScalarAsync<string>(sql, transaction: transaction);
            int nextId = string.IsNullOrEmpty(lastId) ? 1 : int.Parse(lastId) + 1;

            return nextId.ToString("D6");
        }
    }
}
