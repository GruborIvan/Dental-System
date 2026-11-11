using Ordinacija.Features.Doctors;
using Ordinacija.Features.Doctors.Service;
using Ordinacija.Features.MedicalReports;
using Ordinacija.Features.MedicalReports.Service;
using Ordinacija.Features.Patients.Models;
using Ordinacija.Features.Patients.Service;
using Ordinacija.Features.ReportPrint;
using Ordinacija.Features.ReportPrint.Repository;
using System.Windows;
using System.Windows.Controls;

namespace Ordinacija.Features.Patients
{
    /// <summary>
    /// Interaction logic for PatientsView.xaml
    /// </summary>
    public partial class PatientsView : Window
    {
        public PatientViewModel PatientViewModel;

        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IMedicalReportService _medicalReportService;
        private readonly ISchemaRepository _schemaRepository;

        public PatientsView(
            IPatientService patientService,
            IDoctorService doctorService,
            IMedicalReportService medicalReportService,
            ISchemaRepository schemaRepository)
        {
            InitializeComponent();

            PatientViewModel = new PatientViewModel(patientService);
            _patientService = patientService;
            _doctorService = doctorService;
            _schemaRepository = schemaRepository;
            _medicalReportService = medicalReportService;
            DataContext = PatientViewModel;
        }

        private void AddPatient_Click(object sender, RoutedEventArgs e)
        {
            var addNewPatientView = new AddNewPatientView(_patientService, this);
            addNewPatientView.ShowDialog();
        }

        private void EditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Patient selectedPatient)
            {
                var editPatientView = new AddNewPatientView(_patientService, this, selectedPatient);
                editPatientView.ShowDialog();
            }
        }

        private void OpenMedicalReports_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Patient selectedPatient)
            {
                var medicalReportView = new MedicalReportsView(_medicalReportService, _doctorService, _schemaRepository, selectedPatient);
                medicalReportView.Show();
            }
        }

        private void Doctors_Click(object sender, RoutedEventArgs e)
        {
            var doctorsView = new DoctorsView(_doctorService);
            doctorsView.Show();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Patient selectedPatient)
            {
                var documentsManagerView = new DocumentsManagerView(_doctorService, selectedPatient);
                documentsManagerView.Show();
            }
        }

        private void AddNewMedicalReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Patient selectedPatient)
            {
                var medicalReportView = new MedicalReportsView(_medicalReportService, _doctorService, _schemaRepository, selectedPatient);
                medicalReportView.Show();

                medicalReportView.ContentRendered += (sender, e) =>
                {
                    var addNewMedicalReport = new AddNewMedicalReportView(_medicalReportService, _doctorService, _schemaRepository, medicalReportView, selectedPatient, false)
                    {
                        Topmost = true
                    };

                    addNewMedicalReport.ShowDialog();
                };
            }
        }

        private void OsveziPrikazButton_Click(object sender, RoutedEventArgs e)
        {
            PatientViewModel = new PatientViewModel(_patientService);
            DataContext = PatientViewModel;
        }
    }
}
