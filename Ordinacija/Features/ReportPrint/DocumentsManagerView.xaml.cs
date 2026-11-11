using Ordinacija.Features.Doctors.Models;
using Ordinacija.Features.Doctors.Service;
using Ordinacija.Features.Patients.Models;
using Ordinacija.Features.ReportPrint.Repository.Implementation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Ordinacija.Helpers;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ordinacija.Features.ReportPrint
{
    /// <summary>
    /// Interaction logic for DocumentsManagerView.xaml
    /// </summary>
    public partial class DocumentsManagerView : Window, INotifyPropertyChanged
    {
        private string _formText = string.Empty;
        public string FormText
        {
            get => _formText;
            set
            {
                if (_formText != value)
                {
                    _formText = value;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<Doctor> Doctors { get; } = new();
        public List<string> DocumentTypes { get; }

        private readonly PdfPrintService _pdfPrintService;
        private readonly IDoctorService _doctorService;

        private readonly Patient _patient;
        private string _currentlySelectedDoctorId = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DocumentsManagerView(
            IDoctorService doctorService,
            Patient patient)
        {
            InitializeComponent();

            _doctorService = doctorService;
            _pdfPrintService = new PdfPrintService();
            _patient = patient;

            DocumentTypes = GetFileTypeOptions();

            InitializeAsync();

            DataContext = this;
        }

        private async void InitializeAsync()
        {
            try
            {
                var doctors = await _doctorService.GetAllDoctors();

                foreach (var doctor in doctors)
                    Doctors.Add(doctor);
            }
            catch (SqlException ex)
            {
                ErrorHandler.ShowDatabaseError(ex);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;

            var doctorName = GetDoctorFullNameById(_currentlySelectedDoctorId);

            var selectedFileTypeOption = DocumentTypesComboBox.Text;

            if (selectedFileTypeOption.Equals(""))
            {
                SaveButton.IsEnabled = true;
                return;
            }

            switch(selectedFileTypeOption)
            {
                case "Lekarska potvrda": 
                    _pdfPrintService.PrintDoctorsExemption(FormText);
                    break;
                case "Potvrda za predskolsku ustanovu":
                    _pdfPrintService.PrintPreSchoolApproval(FormText);
                    break;
                case "Preporuke za ishranu - Alergije":
                    _pdfPrintService.PrintAlergyConfirmation(FormText);
                    break;
                default: 
                    break;

            }

            SaveButton.IsEnabled = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Doctor selectedDoctor)
            {
                _currentlySelectedDoctorId = selectedDoctor.Id;
            }
        }

        private string GetDoctorFullNameById(string doctoId)
        {
            return Doctors.Where(d => d.Id.Equals(doctoId)).FirstOrDefault()!.FullName;
        }

        private void ComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private List<string> GetFileTypeOptions()
        {
            return new List<string>()
            {
                "Lekarska potvrda",
                "Potvrda za predskolsku ustanovu",
                "Preporuke za ishranu - Alergije"
            };
        }

        private void DocumentTypesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is string documentType)
            {
                switch(documentType)
                {
                    case "Lekarska potvrda": FormText = GetDoctorsExemptionText(); break;
                    case "Potvrda za predskolsku ustanovu": FormText = GetPreSchoolExemptionText(); break;
                    case "Preporuke za ishranu - Alergije": FormText = GetAlergyConfirmationText(); break;
                    default: break;
                }
            }
        }

        private string GetDoctorsExemptionText()
        {
            return string.Format(Constants.DOCTORS_EXEMPTION, _patient.FullName);
        }

        private string GetPreSchoolExemptionText()
        {
            return string.Format(Constants.PRE_SCHOOL_APPROVAL, _patient.FullName, _patient.DateOfBirth.ToString("dd.MM.yyyy."));
        }

        private string GetAlergyConfirmationText()
        {
            var doctorName = GetDoctorFullNameById(_currentlySelectedDoctorId);
            return string.Format(Constants.ALERGY_CONFIRMATION, _patient.FullName, doctorName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
