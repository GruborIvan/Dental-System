using Ordinacija.Features.Doctors.Models;
using Ordinacija.Features.Doctors.Service;
using Ordinacija.Features.MedicalReports.Models;
using Ordinacija.Features.MedicalReports.Service;
using Ordinacija.Features.Patients.Models;
using Ordinacija.Features.ReportPrint.Repository;
using Ordinacija.Features.ReportPrint.Repository.Implementation;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using Ordinacija.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ordinacija.Features.MedicalReports
{
    /// <summary>
    /// Interaction logic for AddNewMedicalReportView.xaml
    /// </summary>
    public partial class AddNewMedicalReportView : Window
    {
        private readonly IMedicalReportService _medicalReportService;
        private readonly IDoctorService _doctorService;
        private readonly ISchemaRepository _schemaRepository;
        private readonly Patient _patient;

        private bool _isEditMode;
        private string _currentlySelectedDoctorId = string.Empty;
        private readonly MedicalReportsView _medicalReportsView;

        public MedicalReport CurrentMedicalReport { get; set; }
        public ObservableCollection<Doctor> Doctors { get; } = new();
        public string TitleText { get; }

        private const string _editMedicalReportTitle = "Izmena podataka o nalazu";
        private const string _addNewMedicalReportTitle = "Unos novog nalaza";

        public AddNewMedicalReportView(
            IMedicalReportService medicalReportService,
            IDoctorService doctorService,
            ISchemaRepository schemaRepository,
            MedicalReportsView medicalReportsView,
            Patient patient,
            bool isUltrasound,
            MedicalReport? medicalReport = null)
        {
            InitializeComponent();

            _medicalReportService = medicalReportService;
            _doctorService = doctorService;
            _schemaRepository = schemaRepository;
            _patient = patient;

            _medicalReportsView = medicalReportsView;
            _isEditMode = medicalReport != null;

            FillUltraSoundSchema(isUltrasound);

            var pageTitle = _isEditMode ? _editMedicalReportTitle : _addNewMedicalReportTitle;

            this.Title = pageTitle;
            TitleText = pageTitle;

            CurrentMedicalReport = medicalReport ?? new MedicalReport { PatientId = _patient.AcSubject };

            DataContext = this;

            Loaded += async (s, e) =>
            {
                try
                {
                    await InitializeAsync(medicalReport, isUltrasound);
                }
                catch (SqlException ex)
                {
                    ErrorHandler.ShowDatabaseError(ex);
                }
            };
        }

        private async Task InitializeAsync(MedicalReport? medicalReport, bool isUltraSound)
        {
            await LoadDoctors();
            SetComboBoxText(CurrentMedicalReport.DoctorId);

            if (medicalReport != null)
            {
                DatumNalazaDatePicker.SelectedDate = medicalReport.DateOfReport;
            }
        }

        private async void FillUltraSoundSchema(bool isUltraSound)
        {
            if (isUltraSound)
            {
                try
                {
                    CurrentMedicalReport = new MedicalReport();
                    Anamneza.Text = await _schemaRepository.GetTemplateSchemaByKey("UZ ABDOMENA I BUBREGA");
                }
                catch (SqlException ex)
                {
                    ErrorHandler.ShowDatabaseError(ex);
                }
            }
        }

        private async Task LoadDoctors()
        {
            var doctorsList = await _doctorService.GetAllDoctors();
            Doctors.Clear();
            foreach (var doctor in doctorsList)
            {
                Doctors.Add(doctor);
            }
        }

        private async void ConfirmCreateMedicalReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateMedicalReportEntry())
                return;

            _currentlySelectedDoctorId = GetDoctorIdForDoctorName(DoctorComboBox.Text);

            if (_currentlySelectedDoctorId == string.Empty)
                return;

            try
            {
                CurrentMedicalReport.DoctorId = _currentlySelectedDoctorId;
                CurrentMedicalReport.DateOfReport = DatumNalazaDatePicker.SelectedDate ?? DateTime.Now;

                if (_isEditMode)
                {
                    await _medicalReportService.UpdateMedicalReport(CurrentMedicalReport);
                }
                else
                {
                    await _medicalReportService.InsertMedicalReport(CurrentMedicalReport);
                }
            }
            catch (SqlException ex)
            {
                ErrorHandler.ShowDatabaseError(ex);
                return;
            }

            // The report is saved at this point. Whatever happens below (reload or printing fails),
            // the form must close, because confirming again would insert the same report twice.
            try
            {
                try
                {
                    await _medicalReportsView.MedicalReportViewModel.LoadMedicalReports();
                }
                catch (SqlException ex)
                {
                    ErrorHandler.ShowSavedButNotRefreshed(ex);
                }

                var shouldPrint = Dialog_DoYouWantToPrint();

                if (shouldPrint)
                {
                    var printPdfRepositor = new PdfPrintService();
                    CurrentMedicalReport.DoctorName = DoctorComboBox.Text;
                    printPdfRepositor.PrintMedicalReport(CurrentMedicalReport, _patient);
                }
            }
            finally
            {
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Doctor selectedDoctor)
            {
                _currentlySelectedDoctorId = selectedDoctor.Id;
            }
        }

        private void ComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        public void SetComboBoxText(string doctorId)
        {
            var comboBox = FindName("DoctorComboBox") as ComboBox;
            if (comboBox != null)
            {
                comboBox.SelectedValue = doctorId;
            }
        }

        public bool ValidateMedicalReportEntry()
        {
            string errorMessage = string.Empty;

            if (string.IsNullOrEmpty(CurrentMedicalReport.Anamneza))
                errorMessage += "Obavezno je uneti anamnezu i fizikalni nalaz.\n";

            if (string.IsNullOrEmpty(CurrentMedicalReport.DG))
                errorMessage += "Obavezno je uneti dijagnozu.\n";

            if (string.IsNullOrEmpty(CurrentMedicalReport.TH))
                errorMessage += "Obavezno je uneti terapiju.\n";

            if (string.IsNullOrEmpty(CurrentMedicalReport.Control))
                errorMessage += "Obavezno je uneti kontrolu.\n";

            if (errorMessage != string.Empty)
            {
                MessageBox.Show(
                $"Potrebno je popuniti sva obavezna polja! \n {errorMessage}",
                "Nepotpun unos",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            }

            return errorMessage == string.Empty;
        }

        private bool Dialog_DoYouWantToPrint()
        {
            var messageBoxResult = MessageBox.Show(
                "Nalaz je uspešno sačuvan! Da li želite odmah da ga odštampate?",
                "Kreiran novi nalaz",
                MessageBoxButton.YesNo,     
                MessageBoxImage.Question
            );

            return messageBoxResult == MessageBoxResult.Yes;
        }

        private string GetDoctorIdForDoctorName(string doctorName)
        {
            var doctor = Doctors.Where(d => d.FullName.Equals(doctorName)).FirstOrDefault();

            if (doctor == null)
            {
                MessageBox.Show(
                $"Niste popunili polje lekara.",
                "Nepotpun unos",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

                return string.Empty;
            }
            else
            {
                return doctor.Id;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
