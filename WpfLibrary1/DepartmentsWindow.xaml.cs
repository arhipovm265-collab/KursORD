using System.Linq;
using System.Windows;
using WpfLibrary1.Data;

namespace WpfLibrary1
{
    public partial class DepartmentsWindow : Window
    {
        private readonly bool _isAdmin;
        private readonly User? _currentUser;
        public DepartmentsWindow(User? currentUser = null)
        {
            InitializeComponent();
            _currentUser = currentUser;
        var rn = currentUser?.Role?.Name ?? string.Empty;
        _isAdmin = !string.IsNullOrWhiteSpace(rn) && (
            string.Equals(rn, "admin", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(rn, "administrator", System.StringComparison.OrdinalIgnoreCase)
            || rn.IndexOf("admin", System.StringComparison.OrdinalIgnoreCase) >= 0
            || rn.IndexOf("админ", System.StringComparison.OrdinalIgnoreCase) >= 0);

        AddDeptButton.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
        EditDeptButton.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
        DeleteDeptButton.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
            Load();
        }

        private void Load()
        {
            using var ctx = new ORDContext();
            var list = ctx.Departments.ToList();
            DepartmentsGrid.ItemsSource = list;
        }

        private void OnAdd(object sender, RoutedEventArgs e)
        {
            var dlg = new AddDepartmentWindow();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                Load();
            }
        }

        private void OnEdit(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is Department sel)
            {
                var dlg = new AddDepartmentWindow(sel);
                dlg.Owner = this;
                if (dlg.ShowDialog() == true) Load();
            }
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is Department sel)
            {
                var res = MessageBox.Show($"Удалить отдел '{sel.Name}'?", "Подтвердите", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var ctx = new ORDContext();
                        var dept = ctx.Departments.Find(sel.Id);
                        if (dept != null)
                        {
                            ctx.Departments.Remove(dept);
                            ctx.SaveChanges();
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Не удалось удалить отдел.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Load();
                }
            }
        }

        private void OnAddOfficer(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is Department sel)
            {
                var dlg = new SelectOfficerWindow(sel.Id);
                if (dlg.ShowDialog() == true)
                {
                    Load();
                }
            }
        }

        // RemoveDeptOfficer functionality removed per user request.

        private void OnViewOfficers(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is Department sel)
            {
                var win = new OfficersWindow(_currentUser, sel.Id);
                win.Owner = this;
                win.ShowDialog();
                Load();
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
