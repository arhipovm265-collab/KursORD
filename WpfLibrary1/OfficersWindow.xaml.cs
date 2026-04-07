using System;
using System.Windows;
using System.Linq;
using WpfLibrary1.Data;

namespace WpfLibrary1
{
    public partial class OfficersWindow : Window
    {
        private readonly bool _isAdmin;
        private readonly int? _departmentId;
        private readonly User? _currentUser;
        public OfficersWindow(User? currentUser = null, int? departmentId = null)
        {
            InitializeComponent();
            var rn = currentUser?.Role?.Name ?? string.Empty;
            _isAdmin = !string.IsNullOrWhiteSpace(rn) && (
                string.Equals(rn, "admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rn, "administrator", StringComparison.OrdinalIgnoreCase)
                || rn.IndexOf("admin", StringComparison.OrdinalIgnoreCase) >= 0
                || rn.IndexOf("админ", StringComparison.OrdinalIgnoreCase) >= 0);
            _departmentId = departmentId;
            _currentUser = currentUser;
            AddButton.IsEnabled = _isAdmin;
            EditButton.IsEnabled = _isAdmin;
            DeleteButton.Visibility = (_isAdmin && !_departmentId.HasValue) ? Visibility.Visible : Visibility.Collapsed;
            try
            {
                var unassignVisible = _isAdmin && _departmentId.HasValue;
                var ub = this.FindName("UnassignButton") as System.Windows.Controls.Button;
                if (ub != null)
                {
                    ub.Visibility = unassignVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch
            {
            }
            Load();
        }

        private void OnUnassign(object sender, RoutedEventArgs e)
        {
            if (OfficersGrid.SelectedItem is not Officer sel)
            {
                MessageBox.Show("Выберите сотрудника для удаления из отдела.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                var hasCases = ctx.CaseRecords.Any(c => c.LeadOfficerId == sel.Id);
                if (hasCases)
                {
                    var rr = MessageBox.Show("Сотрудник является ведущим по одному или нескольким делам. Убрать его из отдела? (дела останутся за сотрудником)", "Подтвердите", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (rr != MessageBoxResult.Yes) return;
                }

                var officer = ctx.Officers.Find(sel.Id);
                if (officer != null)
                {
                    officer.DepartmentId = null;
                    ctx.SaveChanges();
                }
            }
            catch
            {
                MessageBox.Show("Не удалось убрать сотрудника из отдела.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Load();
        }

        private void Load()
        {
            using var ctx = new ORDContext();
            var q = ctx.Officers.AsQueryable();
            if (_departmentId.HasValue)
                q = q.Where(o => o.DepartmentId == _departmentId.Value);
            var list = q.ToList();
            OfficersGrid.ItemsSource = list;
        }

        private void OnAdd(object sender, RoutedEventArgs e)
        {
            if (_departmentId.HasValue)
            {
                var selDlg = new SelectOfficerWindow(_departmentId.Value);
                selDlg.Owner = this;
                var res = selDlg.ShowDialog();
                if (res == true) Load();
                return;
            }

            var dlg = new AddOfficerWindow(_isAdmin, null, _departmentId);
            dlg.Owner = this;
            var res2 = dlg.ShowDialog();
            if (res2 == true)
            {
                Load();
            }
        }

        private void OnEdit(object sender, RoutedEventArgs e)
        {
            if (OfficersGrid.SelectedItem is Officer sel)
            {
                var dlg = new AddOfficerWindow(_isAdmin, sel, _departmentId);
                dlg.Owner = this;
                var res = dlg.ShowDialog();
                if (res == true) Load();
            }
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            if (OfficersGrid.SelectedItem is Officer sel)
            {
                var res = MessageBox.Show($"Удалить сотрудника '{sel.FullName}'?", "Подтвердите", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                try
                {
                    using var ctx = new ORDContext();
                    var hasCases = ctx.CaseRecords.Any(c => c.LeadOfficerId == sel.Id);
                    if (hasCases)
                    {
                        MessageBox.Show("Нельзя удалить сотрудника, который является ведущим по одному или нескольким делам.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var officer = ctx.Officers.Find(sel.Id);
                    if (officer != null)
                    {
                        ctx.Officers.Remove(officer);
                        ctx.SaveChanges();
                    }
                }
                catch
                {
                    MessageBox.Show("Не удалось удалить сотрудника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

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
