using System.Windows;
using System.Linq;
using System.Text;
using WpfLibrary1.Data;

namespace WpfLibrary1
{
    public partial class AddOfficerWindow : Window
    {
        private readonly bool _isAdmin;
        private readonly Officer? _editingOfficer;
        private readonly int? _departmentId;

        public AddOfficerWindow(bool isAdmin, Officer? editing = null, int? departmentId = null)
        {
            InitializeComponent();
            _isAdmin = isAdmin;
            _editingOfficer = editing;
            _departmentId = departmentId;
            CreateAccountCheck.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (_editingOfficer != null)
            {
                LastNameBox.Text = _editingOfficer.LastName;
                FirstNameBox.Text = _editingOfficer.FirstName;
                PatronymicBox.Text = _editingOfficer.Patronymic;
                EmailBox.Text = _editingOfficer.Email;
                PhoneBox.Text = _editingOfficer.Phone;
                CanBeLeadCheck.IsChecked = _editingOfficer.CanBeLead;
                try
                {
                    using var ctx = new ORDContext();
                    var ranks = ctx.OfficerRanks.OrderBy(r => r.Name).ToList();
                    RankBox.ItemsSource = ranks;
                    if (_editingOfficer.RankId.HasValue)
                        RankBox.SelectedItem = ranks.FirstOrDefault(r => r.Id == _editingOfficer.RankId.Value);

                    var user = ctx.Users.FirstOrDefault(u => u.OfficerId == _editingOfficer.Id);
                    if (user != null)
                    {
                        CreateAccountCheck.IsChecked = true;
                        CreateAccountCheck.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
                        AccountPanel.Visibility = Visibility.Visible;
                        NewUsernameBox.Text = user.Username;
                    }
                }
                catch
                {
                }
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnAdd(object sender, RoutedEventArgs e)
        {
            var last = LastNameBox.Text?.Trim();
            var first = FirstNameBox.Text?.Trim();
            var patron = PatronymicBox.Text?.Trim();
            var email = EmailBox.Text?.Trim();
            var phone = PhoneBox.Text?.Trim();
            var rank = RankBox.SelectedItem as OfficerRank;
            var canBeLead = CanBeLeadCheck.IsChecked == true;

            if (string.IsNullOrEmpty(last) || string.IsNullOrEmpty(first))
            {
                MessageBox.Show("Укажите фамилию и имя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                Officer officer;
                if (_editingOfficer != null)
                {
                    officer = ctx.Officers.FirstOrDefault(o => o.Id == _editingOfficer.Id) ?? _editingOfficer;
                    officer.FirstName = first;
                    officer.Patronymic = string.IsNullOrEmpty(patron) ? null : patron;
                    officer.LastName = last;
                    officer.RankId = rank?.Id;
                    officer.CanBeLead = canBeLead;
                    officer.Email = string.IsNullOrEmpty(email) ? null : email;
                    officer.Phone = string.IsNullOrEmpty(phone) ? null : phone;
                }
                else
                {
                    officer = new Officer
                    {
                        FirstName = first,
                        Patronymic = string.IsNullOrEmpty(patron) ? null : patron,
                        LastName = last,
                        RankId = rank?.Id,
                        CanBeLead = canBeLead,
                        Email = string.IsNullOrEmpty(email) ? null : email,
                        Phone = string.IsNullOrEmpty(phone) ? null : phone,
                        DepartmentId = _departmentId
                    };
                    ctx.Officers.Add(officer);
                }
                ctx.SaveChanges();
                if (_isAdmin && CreateAccountCheck.IsChecked == true)
                {
                    var uname = NewUsernameBox.Text?.Trim();
                    var pwd = NewPasswordBox.Password ?? string.Empty;
                    var pwd2 = NewPasswordConfirmBox.Password ?? string.Empty;
                    if (string.IsNullOrEmpty(uname))
                    {
                        MessageBox.Show("Укажите логин для учётной записи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (!string.IsNullOrEmpty(pwd) && pwd != pwd2)
                    {
                        MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var role = ctx.Roles.FirstOrDefault(r => r.Name == "Officer");
                    if (role == null)
                    {
                        role = new Role { Name = "Officer" };
                        ctx.Roles.Add(role);
                        ctx.SaveChanges();
                    }
                    var existing = ctx.Users.FirstOrDefault(u => u.OfficerId == officer.Id);
                    if (existing == null)
                    {
                        var usr = new User
                        {
                            Username = uname,
                            PasswordHash = string.IsNullOrEmpty(pwd) ? null : ComputeHash(pwd),
                            RoleId = role.Id,
                            OfficerId = officer.Id
                        };
                        ctx.Users.Add(usr);
                    }
                    else
                    {
                        existing.Username = uname;
                        if (!string.IsNullOrEmpty(pwd)) existing.PasswordHash = ComputeHash(pwd);
                        existing.RoleId = role.Id;
                        existing.OfficerId = officer.Id;
                    }
                    ctx.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Не удалось добавить сотрудника: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCreateAccountChecked(object sender, RoutedEventArgs e)
        {
            AccountPanel.Visibility = CreateAccountCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private static byte[] ComputeHash(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(Encoding.Unicode.GetBytes(input));
        }
    }
}
