using System.Windows;
using WpfLibrary1.Data;

namespace WpfLibrary1
{
    public partial class AddDepartmentWindow : Window
    {
        private readonly Department? _editing;
        public AddDepartmentWindow(Department? editing = null)
        {
            InitializeComponent();
            _editing = editing;
            if (_editing != null)
            {
                NameBox.Text = _editing.Name;
                StreetBox.Text = _editing.Street ?? string.Empty;
                HouseBox.Text = _editing.House ?? string.Empty;
                CityBox.Text = _editing.City ?? string.Empty;
                PhoneBox.Text = _editing.Phone;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text?.Trim();
            var street = StreetBox.Text?.Trim();
            var house = HouseBox.Text?.Trim();
            var city = CityBox.Text?.Trim();
            var phone = PhoneBox.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Укажите имя отдела.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                if (_editing != null)
                {
                    var dept = ctx.Departments.Find(_editing.Id);
                    if (dept != null)
                    {
                        dept.Name = name;
                        dept.Street = string.IsNullOrEmpty(street) ? null : street;
                        dept.House = string.IsNullOrEmpty(house) ? null : house;
                        dept.City = string.IsNullOrEmpty(city) ? null : city;
                        dept.Phone = phone;
                        ctx.SaveChanges();
                    }
                }
                else
                {
                    var dept = new Department { Name = name, Street = string.IsNullOrEmpty(street) ? null : street, House = string.IsNullOrEmpty(house) ? null : house, City = string.IsNullOrEmpty(city) ? null : city, Phone = phone };
                    ctx.Departments.Add(dept);
                    ctx.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Не удалось сохранить отдел: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
