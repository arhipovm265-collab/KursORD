using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using WpfLibrary1.Data;
using Microsoft.EntityFrameworkCore;

namespace WpfLibrary1
{
    public partial class FirstRunWindow : Window
    {
        public User? CreatedUser { get; private set; }

        public FirstRunWindow()
        {
            InitializeComponent();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnCreate(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text?.Trim();
            var password = PasswordBox.Password ?? string.Empty;
            var firstName = FirstNameBox.Text?.Trim() ?? string.Empty;
            var lastName = LastNameBox.Text?.Trim() ?? string.Empty;
            var patronymic = PatronymicBox.Text?.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(firstName))
            {
                MessageBox.Show("Заполните имя пользователя, пароль, фамилию и имя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                // Try to apply migrations; if that fails (e.g., permissions or incompatible state), fall back to EnsureCreated
                try
                {
                    ctx.Database.Migrate();
                }
                catch (Exception migrateEx)
                {
                    try
                    {
                        ctx.Database.EnsureCreated();
                    }
                    catch (Exception ensureEx)
                    {
                        MessageBox.Show($"Не удалось подготовить базу данных:\nМиграция: {migrateEx.Message}\nEnsureCreated: {ensureEx.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                var adminRole = ctx.Roles.FirstOrDefault(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    adminRole = new Role { Name = "Admin" };
                    ctx.Roles.Add(adminRole);
                    ctx.SaveChanges();
                }
                var officer = new Officer
                {
                    FirstName = firstName,
                    Patronymic = string.IsNullOrEmpty(patronymic) ? null : patronymic,
                    LastName = lastName,
                    CanBeLead = false
                };
                ctx.Officers.Add(officer);
                ctx.SaveChanges();

                var user = new User
                {
                    Username = username,
                    PasswordHash = ComputeHash(password),
                    RoleId = adminRole.Id,
                    OfficerId = officer.Id
                };
                ctx.Users.Add(user);
                ctx.SaveChanges();
                var u = ctx.Users.Find(user.Id);
                CreatedUser = u;
                MessageBox.Show("Администратор создан.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                var res = MessageBox.Show("Перейти к добавлению сотрудников?", "Дальше", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    var win = new OfficersWindow(u);
                    win.Owner = this;
                    win.ShowDialog();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось создать пользователя: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static byte[] ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.Unicode.GetBytes(input));
        }
    }
}
