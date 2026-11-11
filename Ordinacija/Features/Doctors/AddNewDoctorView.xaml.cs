using Ordinacija.Features.Doctors.Models;
using Ordinacija.Features.Doctors.Service;
using Ordinacija.Features.Patients.Models;
using Microsoft.Data.SqlClient;
using Ordinacija.Helpers;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Ordinacija.Features.Doctors
{
    /// <summary>
    /// Interaction logic for AddNewDoctorView.xaml
    /// </summary>
    public partial class AddNewDoctorView : Window
    {
        private readonly IDoctorService _doctorService;
        public readonly DoctorsView DoctorsView;

        public Doctor CurrentDoctor { get; }
        public string TitleText { get; }

        private const string _editDoctorTitle = "Izmena podataka o doktoru";
        private const string _addDoctorTitle = "Dodavanje novog doktora";
        private bool _isEditMode;

        public AddNewDoctorView(
            IDoctorService doctorService,
            DoctorsView doctorsView,
            Doctor? doctor = null)
        {
            InitializeComponent();

            _doctorService = doctorService;
            _isEditMode = doctor != null;

            DoctorsView = doctorsView;

            CurrentDoctor = doctor ?? new Doctor();

            if (doctor != null)
                PreFillDateOfBirth(doctor.DateOfBirth);

            var pageTitle = _isEditMode ? _editDoctorTitle : _addDoctorTitle;

            this.Title = pageTitle;
            TitleText = pageTitle;

            DataContext = this;
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDoctorEntry())
                return;

            try
            {
                await (
                    _isEditMode
                    ? _doctorService.UpdateExistingDoctor(CurrentDoctor)
                    : _doctorService.CreateNewDoctor(CurrentDoctor));
            }
            catch (SqlException ex)
            {
                ErrorHandler.ShowDatabaseError(ex);
                return;
            }

            // The doctor is saved at this point. A failed reload must not leave the form open,
            // because confirming again would insert the same doctor twice.
            try
            {
                await DoctorsView.DoctorViewModel.LoadDoctors();
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
                "Da li ste sigurni da želite da zatvorite stranicu? Imate nesačuvane izmene.",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private bool ValidateDoctorEntry()
        {
            string errorMessage = string.Empty;

            if (string.IsNullOrEmpty(CurrentDoctor.FirstName))
                errorMessage += "Obavezno je uneti ime doktora.\n";

            if (string.IsNullOrEmpty(CurrentDoctor.LastName))
                errorMessage += "Obavezno je uneti prezime doktora.\n";

            var (isValid, dateOfBirth, errors) = ValidateAndParseDateOfBirth();

            if (isValid)
            {
                CurrentDoctor.DateOfBirth = dateOfBirth;
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
                    MessageBoxImage.Warning
                );
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

        private void PreFillDateOfBirth(DateTime dateOfBirth)
        {
            DayTextBox.Text = dateOfBirth.Day.ToString();
            MonthTextBox.Text = dateOfBirth.Month.ToString();
            YearTextBox.Text = dateOfBirth.Year.ToString();
        }
    }
}
