using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Ordinacija.Features.Doctors.Models;
using Microsoft.Data.SqlClient;

namespace Ordinacija.Features.Doctors.Repository.Implementation
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly string _connectionString;
        private readonly IMapper _mapper;

        public DoctorRepository(IConfiguration configuration, IMapper mapper)
        {
            _connectionString = configuration.GetConnectionString("PantheonDB") ?? throw new ArgumentNullException(nameof(configuration));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctors()
        {
            using var connection = new SqlConnection(_connectionString);
            
            await connection.OpenAsync();

            var query = @"SELECT AcSubject, AcName2, AcAddress, AdBirthDate, AcFieldSC 
                          FROM THE_SetSubj
                          WHERE acSupplier = 'T';
                         ";

            var doctorsDbo = await connection.QueryAsync<DoctorDbo>(query);
            return _mapper.Map<List<Doctor>>(doctorsDbo);
        }


        public async Task InsertDoctor(Doctor doctor)
        {
            var doctorDbo = _mapper.Map<DoctorDbo>(doctor);

            var sql = @"
                INSERT INTO tHE_SetSubj (AcSubject, AcName2, AdBirthDate, AcFieldSC, AcSupplier) 
                VALUES (@AcSubject, @AcName2, @AdBirthDate, @AcFieldSC, 'T');
            ";

            using var connection = new SqlConnection(_connectionString);
            
            await connection.OpenAsync();

            var lastId = await connection.ExecuteScalarAsync<string>("SELECT MAX(AcSubject) FROM tHE_SetSubj");
            int nextId = (string.IsNullOrEmpty(lastId) ? 1 : int.Parse(lastId) + 1);
            doctorDbo.AcSubject = nextId.ToString("D6");

            await connection.ExecuteAsync(sql, doctorDbo);
        }

        public async Task UpdateDoctor(Doctor doctor)
        {
            var doctorDbo = _mapper.Map<DoctorDbo>(doctor);

            var sql = @"
                UPDATE tHE_SetSubj
                SET 
                    AcName2 = @AcName2, 
                    AdBirthDate = @AdBirthDate,
                    AcFieldSC = @AcFieldSC
                WHERE AcSubject = @AcSubject";

            using var connection = new SqlConnection(_connectionString);
            
            try
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(sql, doctorDbo);
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                throw;
            }

        }
    }
}
