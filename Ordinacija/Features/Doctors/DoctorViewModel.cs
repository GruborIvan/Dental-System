using Ordinacija.Features.Common;
using Ordinacija.Features.Doctors.Models;
using Ordinacija.Features.Doctors.Service;
using Ordinacija.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Ordinacija.Features.Doctors
{
    public class DoctorViewModel : BaseViewModel
    {
        private readonly IDoctorService _doctorService;

        public ObservableCollection<Doctor> Doctors { get; set; } = new();

        public ICommand LoadDoctorsCommand { get; }

        public DoctorViewModel(IDoctorService doctorService)
        {
            _doctorService = doctorService;
            LoadDoctorsCommand = new RelayCommand(LoadDoctors);
            ErrorHandler.ObserveErrors(LoadDoctors());
        }

        public async Task LoadDoctors()
        {
            var doctors = await _doctorService.GetAllDoctors();

            Doctors.Clear();

            foreach (var patient in doctors)
            {
                Doctors.Add(patient);
            }
        }
    }
}
