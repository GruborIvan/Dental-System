using Microsoft.IdentityModel.Tokens;
using Ordinacija.Features.Patients.Models;
using Ordinacija.Features.Patients.Service;
using Microsoft.Data.SqlClient;
using Ordinacija.Helpers;
using System.Windows;
using System.Windows.Input;

namespace Ordinacija.Features.Patients
{
    /// <summary>
    /// Interaction logic for AddNewPatientView.xaml
    /// </summary>
    public partial class AddNewPatientView : Window
    {
        private readonly IPatientService _patientService;
        private readonly PatientsView _patientsView;

        private const string _editPatientTitle = "Izmena podataka o pacijentu";
        private const string _addNewPatientTitle = "Unos novog pacijenta";

        public Patient CurrentPatient { get; private set; }

        public string TitleText { get; }
        private bool _isEditMode;

        public AddNewPatientView(
            IPatientService patientService,
            PatientsView patientView,
            Patient? patient = null)
        {
            InitializeComponent();
            _patientService = patientService;
            _patientsView = patientView;

            _isEditMode = patient != null;

            CurrentPatient = patient ?? new Patient();
            
            if (patient != null) 
                PreFillDateOfBirth(patient.DateOfBirth);

            var pageTitle = _isEditMode ? _editPatientTitle : _addNewPatientTitle;

            this.Title = pageTitle;
            TitleText = pageTitle;

            DataContext = this;
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePatientEntry())
                return;

            try
            {
                await (
                    _isEditMode
                    ? _patientService.UpdateExistingPatient(CurrentPatient)
                    : _patientService.CreateNewPatient(CurrentPatient));
            }
            catch (SqlException ex)
            {
                ErrorHandler.ShowDatabaseError(ex);
                return;
            }

            // The patient is saved at this point. A failed reload must not leave the form open,
            // because confirming again would insert the same patient twice.
            try
            {
                await _patientsView.PatientViewModel.LoadPatients();
            }
            catch (SqlException ex)
            {
                ErrorHandler.ShowSavedButNotRefreshed(ex);
            }

            this.Close();
        }

        private void PonistiButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Da li ste sigurni da želite da zatvorite stranicu bez čuvanja izmena?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private bool ValidatePatientEntry()
        {
            string errorMessage = string.Empty;

            if (string.IsNullOrEmpty(CurrentPatient.FirstName))
                errorMessage += "Obavezno je uneti ime pacijenta.\n";

            if  (string.IsNullOrEmpty(CurrentPatient.LastName))
                errorMessage += "Obavezno je uneti prezime pacijenta.\n";

            var (isValid, dateOfBirth, errors) = ValidateAndParseDateOfBirth();

            if (isValid)
            {
                CurrentPatient.DateOfBirth = dateOfBirth;
            }
            else
            {
                var dateErrors = string.Join("\n", errors);
                errorMessage += dateErrors;
            }

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

        public (bool IsValid, DateTime DateOfBirth, List<string> Errors) ValidateAndParseDateOfBirth()
        {
            var day = DayTextBox.Text;
            var month = MonthTextBox.Text;
            var year = YearTextBox.Text;

            var errorList = new List<string>();
            DateTime resultDate = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(day)) errorList.Add("Polje za dan je obavezno.");
            if (string.IsNullOrWhiteSpace(month)) errorList.Add("Polje za mesec je obavezno.");
            if (string.IsNullOrWhiteSpace(year)) errorList.Add("Polje za godinu je obavezno.");

            if (errorList.Count > 0)
            {
                return (false, resultDate, errorList);
            }

            if (!int.TryParse(day, out int dayValue)) errorList.Add("Dan mora biti broj.");
            if (!int.TryParse(month, out int monthValue)) errorList.Add("Mesec mora biti broj.");
            if (!int.TryParse(year, out int yearValue)) errorList.Add("Godina mora biti broj.");

            if (errorList.Count > 0)
            {
                return (false, resultDate, errorList);
            }

            if (!DateTime.TryParse($"{yearValue}-{monthValue:00}-{dayValue:00}", out resultDate))
            {
                errorList.Add("Uneti datum rodjenja nije validan.");
                return (false, resultDate, errorList);
            }

            if (resultDate > DateTime.Today)
            {
                errorList.Add("Datum rodjenja ne može biti u budućnosti.");
                return (false, resultDate, errorList);
            }

            return (true, resultDate, errorList);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
            }
        }

        private void PreFillDateOfBirth(DateTime dateOfBirth)
        {
            DayTextBox.Text = dateOfBirth.Day.ToString();
            MonthTextBox.Text = dateOfBirth.Month.ToString();
            YearTextBox.Text = dateOfBirth.Year.ToString();
        }
    }
}
