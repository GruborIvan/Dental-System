using Microsoft.Data.SqlClient;
using System.Windows.Input;

namespace Ordinacija.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public async void Execute(object parameter)
        {
            try
            {
                await _execute();
            }
            catch (SqlException ex)
            {
                ErrorHandler.ShowDatabaseError(ex);
            }
        }

        public event EventHandler CanExecuteChanged;
    }
}
