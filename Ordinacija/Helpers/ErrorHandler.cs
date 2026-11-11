using Microsoft.Data.SqlClient;
using System.IO;
using System.Windows;

namespace Ordinacija.Helpers
{
    public static class ErrorHandler
    {
        // SQL Server / network error numbers that mean the server could not be reached,
        // as opposed to the server answering with an error.
        private static readonly HashSet<int> ConnectionErrorNumbers = new()
        {
            -2, -1, 2, 26, 40, 53, 64, 121, 233, 258, 1225, 10053, 10054, 10060, 10061, 11001
        };

        public static bool IsConnectionError(SqlException ex) =>
            ex.Class >= 20 || ConnectionErrorNumbers.Contains(ex.Number);

        public static void ShowDatabaseError(SqlException ex)
        {
            Log(ex);

            if (IsConnectionError(ex))
            {
                Show(
                    "Server nije dostupan",
                    "Nije moguća komunikacija sa bazom podataka.\n" +
                    "Proverite da li je server upaljen i da li je računar povezan na mrežu.",
                    ex);
            }
            else
            {
                Show(
                    "Greška u bazi podataka",
                    "Došlo je do greške pri radu sa bazom podataka.",
                    ex);
            }
        }

        /// <summary>
        /// The save itself succeeded; only reloading the list afterwards failed.
        /// </summary>
        public static void ShowSavedButNotRefreshed(SqlException ex)
        {
            Log(ex);

            Show(
                "Prikaz nije osvežen",
                "Podaci su sačuvani, ali prikaz nije mogao da se osveži.\n" +
                "Nemojte ponovo unositi iste podatke. Osvežite prikaz kada server bude dostupan.",
                ex,
                MessageBoxImage.Warning);
        }

        public static void ShowUnexpectedError(Exception ex)
        {
            Log(ex);

            Show(
                "Neočekivana greška",
                "Došlo je do neočekivane greške. Ako se greška ponavlja, obratite se podršci.",
                ex);
        }

        /// <summary>
        /// Awaits a task that was started without being awaited (for example from a constructor)
        /// so that a database failure is shown instead of being silently dropped.
        /// Other exceptions continue to the application-wide handler in App.
        /// </summary>
        public static async void ObserveErrors(Task task)
        {
            try
            {
                await task;
            }
            catch (SqlException ex)
            {
                ShowDatabaseError(ex);
            }
        }

        public static string LogFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ordinacija",
            "errors.log");

        private static void Show(string title, string message, Exception ex, MessageBoxImage icon = MessageBoxImage.Error)
        {
            MessageBox.Show(
                $"{message}\n\nDetalji: {ex.Message}",
                title,
                MessageBoxButton.OK,
                icon);
        }

        private static void Log(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{ex}\n\n");
            }
            catch
            {
                // Logging must never be the reason the app fails.
            }
        }
    }
}
