using Ordinacija.Features.Common;
using Ordinacija.Features.Patients.Models;
using Ordinacija.Features.Patients.Service;
using Ordinacija.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Ordinacija.Features.Patients
{
    public class PatientViewModel : BaseViewModel
    {
        private readonly IPatientService _patientService;
        private string _searchText = string.Empty;

        public ObservableCollection<Patient> Patients { get; set; } = new();
        public ObservableCollection<Patient> FilteredPatients { get; set; } = new();

        public ICommand LoadPatientsCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterPatients();
            }
        }

        public PatientViewModel(IPatientService patientService)
        {
            _patientService = patientService;
            LoadPatientsCommand = new RelayCommand(LoadPatients);
            ErrorHandler.ObserveErrors(LoadPatients());
        }

        public async Task LoadPatients()
        {
            var patients = await _patientService.GetAllPatients();
            FilteredPatients.Clear();
            Patients.Clear();
            foreach (var patient in patients)
            {
                Patients.Add(patient);
                FilteredPatients.Add(patient);
            }
        }

        private void FilterPatients()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredPatients.Clear();
                foreach (var patient in Patients)
                {
                    FilteredPatients.Add(patient);
                }
            }
            else
            {
                var lowerSearch = SearchText.ToLower();

                var filtered = Patients.Where(p =>
                    p.FirstName.ToLower().Contains(lowerSearch) ||
                    p.LastName.ToLower().Contains(lowerSearch)).ToList();

                FilteredPatients.Clear();
                foreach (var patient in filtered)
                {
                    FilteredPatients.Add(patient);
                }
            }
        }
    }
}
