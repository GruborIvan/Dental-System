using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordinacija.Features.Patients.Service;
using Ordinacija.Features.Patients;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Ordinacija.Features.Patients.Repository;
using AutoMapper;
using Ordinacija.Extensions;
using Ordinacija.Features.Login;
using Ordinacija.Features.MedicalReports;
using Ordinacija.Features.MedicalReports.Service;
using Ordinacija.Features.MedicalReports.Service.Implementation;
using Ordinacija.Features.MedicalReports.Repository;
using Ordinacija.Features.MedicalReports.Repository.Implementations;
using Ordinacija.Features.Doctors.Service;
using Ordinacija.Features.Doctors.Repository;
using Ordinacija.Features.Doctors.Service.Implementation;
using Ordinacija.Features.Doctors.Repository.Implementation;
using Ordinacija.Features.Patients.Models;
using Ordinacija.Features.ReportPrint.Repository;
using Ordinacija.Features.ReportPrint.Repository.Implementation;
using Ordinacija.Features.Login.Repository;
using Ordinacija.Features.Login.Repository.Implementation;
using Ordinacija.Helpers;
using Microsoft.Data.SqlClient;
using System.Windows.Threading;

namespace Ordinacija
{
    public partial class App : Application
    {
        private const string LocalSettingsFile = "appsettings.Local.json";

        private readonly IHost _host;
        public static IMapper Mapper { get; private set; }
        public static IConfiguration Configuration { get; private set; }

        public App()
        {
            // Create and build the host with DI services.
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Real connection strings live in this untracked file and override appsettings.json.
                    config.AddJsonFile(LocalSettingsFile, optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configure services (services, ViewModels, and Views)
                    ConfigureServices(services);
                })
                .Build();

            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        // Last line of defence: anything a screen did not handle itself is logged and shown
        // instead of terminating the application.
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is SqlException sqlException)
                ErrorHandler.ShowDatabaseError(sqlException);
            else
                ErrorHandler.ShowUnexpectedError(e.Exception);

            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Configure appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(LocalSettingsFile, optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            // Retrieve AutoMapper service
            Mapper = _host.Services.GetRequiredService<IMapper>();

            // Get MainWindow and show
            var loginWindow = _host.Services.GetRequiredService<LoginView>();
            loginWindow.Show();

            CleanupOldFiles(AppDomain.CurrentDomain.BaseDirectory, TimeSpan.FromDays(7));

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register AutoMapper with the correct namespace
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>(), typeof(App));

            // Register services
            services.AddSingleton<IPatientService, PatientService>();
            services.AddSingleton<IPatientRepository, PatientRepository>();

            services.AddSingleton<IDoctorService, DoctorService>();
            services.AddSingleton<IDoctorRepository, DoctorRepository>();

            services.AddSingleton<IMedicalReportService, MedicalReportService>();
            services.AddSingleton<IMedicalReportRepository, MedicalReportRepository>();

            services.AddTransient<ISchemaRepository, SchemaRepository>();
            services.AddTransient<ILoginRepository, LoginRepository>();

            // Register ViewModels
            services.AddTransient<PatientViewModel>();
            services.AddTransient<MedicalReportViewModel>();

            // Register Views
            services.AddSingleton<PatientsView>();
            services.AddSingleton<LoginView>();

            services.AddScoped<Patient>(provider => new Patient());
        }

        public void CleanupOldFiles(string directory, TimeSpan maxAge)
        {
            var files = Directory.GetFiles(directory, "*.pdf");
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (DateTime.UtcNow - fileInfo.CreationTimeUtc > maxAge)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
                    }
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
