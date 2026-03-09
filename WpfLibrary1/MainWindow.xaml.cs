using System.Windows;
using WpfLibrary1.ViewModels;
using WpfLibrary1.Data;

namespace WpfLibrary1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (DataContext == null)
            {
                var ctx = new ORDContext();
                DataContext = new ViewModels.MainViewModel(ctx, null);
            }
            UpdateManageOfficersVisibility();
            this.DataContextChanged += (_, _) => UpdateManageOfficersVisibility();
        }

        private void UpdateManageOfficersVisibility()
        {
            if (ManageOfficersButton == null) return;
            if (DataContext is ViewModels.MainViewModel vm)
            {
                ManageOfficersButton.Visibility = vm.CanEditAll ? Visibility.Visible : Visibility.Collapsed;
                if (ManageDepartmentsButton != null)
                    ManageDepartmentsButton.Visibility = vm.CanEditAll ? Visibility.Visible : Visibility.Collapsed;
            }
            else
                ManageOfficersButton.Visibility = Visibility.Collapsed;
        }

        private void OnManageOfficers(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                var win = new OfficersWindow(vm.CurrentUser);
                win.Owner = this;
                win.ShowDialog();
            }
        }

        private void OnManageDepartments(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                var win = new DepartmentsWindow(vm.CurrentUser);
                win.Owner = this;
                win.ShowDialog();
            }
        }

    }
}
