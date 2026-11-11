using CommunityToolkit.Mvvm.Input;
using Ordinacija.Features.Common;
using Ordinacija.Features.Doctors.Service;
using Ordinacija.Features.MedicalReports.Models;
using Ordinacija.Features.MedicalReports.Service;
using Ordinacija.Features.Patients.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using RelayCommand = Ordinacija.Helpers.RelayCommand;

namespace Ordinacija.Features.MedicalReports
{
    public class MedicalReportViewModel : BaseViewModel
    {
        private readonly IMedicalReportService _medicalReportService;
        private readonly IDoctorService _doctorService;

        public Patient Patient
        {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        public ObservableCollection<MedicalReport> MedicalReports { get; } = new();

        public ICommand LoadMedicalReportsCommand { get; }
        public IAsyncRelayCommand NextReportCommand { get; }
        public IAsyncRelayCommand PreviousReportCommand { get; }
        public IAsyncRelayCommand CreateNewMedicalReportCommand { get; }

        public MedicalReport? CurrentMedicalReport
        {
            get => _currentMedicalReport;
            private set => SetProperty(ref _currentMedicalReport, value);
        }

        public int CurrentReportPage => _currentIndex + 1;
        public int TotalReports => MedicalReports.Count;

        public MedicalReportViewModel(
            Patient patient, 
            IMedicalReportService medicalReportService,
            IDoctorService doctorService)
        {
            Patient = patient ?? throw new ArgumentNullException(nameof(patient));
            _patient = patient;

            _medicalReportService = medicalReportService ?? throw new ArgumentNullException(nameof(medicalReportService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));

            LoadMedicalReportsCommand = new RelayCommand(LoadMedicalReports);
            NextReportCommand = new AsyncRelayCommand(NextReport, CanMoveNext);
            PreviousReportCommand = new AsyncRelayCommand(PreviousReport, CanMovePrevious);
            CreateNewMedicalReportCommand = new AsyncRelayCommand(CreateNewMedicalReport);

            Helpers.ErrorHandler.ObserveErrors(LoadMedicalReports());
        }

        public async Task LoadMedicalReports()
        {
            OnPropertyChanged(nameof(Patient));
            var reports = await _medicalReportService.GetMedicalReportsForPatient(Patient.AcSubject);

            FillMedicalReportsList(reports);

            _currentIndex = 0;
            CurrentMedicalReport = MedicalReports.FirstOrDefault();

            OnPropertyChanged(nameof(MedicalReports));
            OnPropertyChanged(nameof(CurrentReportPage));
            OnPropertyChanged(nameof(TotalReports));
            OnPropertyChanged(nameof(CanMoveNext));
            OnPropertyChanged(nameof(CanMovePrevious));

            NextReportCommand.NotifyCanExecuteChanged();
            PreviousReportCommand.NotifyCanExecuteChanged();
        }

        public async Task CreateNewMedicalReport()
        {
            await _medicalReportService.InsertMedicalReport(CurrentMedicalReport!);
            MedicalReports.Add(CurrentMedicalReport!);
            _currentIndex = MedicalReports.Count - 1;

            RefreshCommandStates();
        }

        private void FillMedicalReportsList(IEnumerable<MedicalReport> medicalReports)
        {
            MedicalReports.Clear();
            foreach (var report in medicalReports)
            {
                MedicalReports.Add(report);
            }
        }

        private async Task NextReport()
        {
            if (!CanMoveNext()) return;

            _currentIndex++;
            CurrentMedicalReport = MedicalReports[_currentIndex];

            RefreshCommandStates();
            await Task.CompletedTask; 
        }

        private async Task PreviousReport()
        {
            if (!CanMovePrevious()) return;

            _currentIndex--;
            CurrentMedicalReport = MedicalReports[_currentIndex];

            RefreshCommandStates();
            await Task.CompletedTask;
        }

        private void RefreshCommandStates()
        {
            OnPropertyChanged(nameof(MedicalReports));
            OnPropertyChanged(nameof(CurrentReportPage));
            OnPropertyChanged(nameof(CanMoveNext));
            OnPropertyChanged(nameof(CanMovePrevious));

            NextReportCommand.NotifyCanExecuteChanged();
            PreviousReportCommand.NotifyCanExecuteChanged();
        }

        private bool CanMoveNext() => _currentIndex < MedicalReports.Count - 1;
        private bool CanMovePrevious() => _currentIndex > 0;

        private Patient _patient;
        private int _currentIndex;
        private MedicalReport? _currentMedicalReport;
    }
}
