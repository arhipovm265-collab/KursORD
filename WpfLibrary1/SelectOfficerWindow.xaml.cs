using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WpfLibrary1.Data;

namespace WpfLibrary1
{
    public partial class SelectOfficerWindow : Window
    {
        private readonly int _departmentId;
        public SelectOfficerWindow(int departmentId)
        {
            InitializeComponent();
            _departmentId = departmentId;
            Load();
        }

        private void Load()
        {
            using var ctx = new ORDContext();
            var list = ctx.Officers
                .Include(o => o.Department)
                .Where(o => o.DepartmentId == null || o.DepartmentId != _departmentId)
                .OrderBy(o => o.LastName)
                .ToList();
            AvailableGrid.ItemsSource = list;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnAssign(object sender, RoutedEventArgs e)
        {
            if (AvailableGrid.SelectedItem is not Officer sel)
            {
                MessageBox.Show("Выберите сотрудника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                var officer = ctx.Officers.Find(sel.Id);
                if (officer != null)
                {
                    officer.DepartmentId = _departmentId;
                    ctx.SaveChanges();
                }
                DialogResult = true;
                Close();
            }
            catch
            {
                MessageBox.Show("Не удалось назначить сотрудника в отдел.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
