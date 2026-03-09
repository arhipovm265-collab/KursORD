using System;
using System.Windows;

namespace WpfLibrary1
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                var login = new LoginWindow();
                var res = login.ShowDialog();
                if (res != true || login.AuthenticatedUser == null)
                {
                    app.Shutdown();
                    return;
                }
                var ctx = new WpfLibrary1.Data.ORDContext();
                var main = new MainWindow();
                main.DataContext = new ViewModels.MainViewModel(ctx, login.AuthenticatedUser);
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.Run(main);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Приложение не удалось запустить:\n{ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
