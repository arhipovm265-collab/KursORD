using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WpfLibrary1.Data;

namespace WpfLibrary1
{
    public partial class LoginWindow : Window
    {
        public User? AuthenticatedUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            CheckFirstRun();
        }

        private void CheckFirstRun()
        {
            try
            {
                using var ctx = new ORDContext();
                bool noUsers = false;
                try
                {
                    if (!ctx.Database.CanConnect())
                    {
                        noUsers = true;
                    }
                    else
                    {
                        noUsers = !ctx.Users.Any();
                    }
                }
                catch
                {
                    noUsers = true;
                }

                FirstRunButton.Visibility = noUsers ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                FirstRunButton.Visibility = Visibility.Visible;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnLogin(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text?.Trim();
            var password = PasswordBox.Password ?? string.Empty;

            if (string.IsNullOrEmpty(username))
            {
                ErrorText.Text = "Введите имя пользователя.";
                return;
            }

            try
            {
                using var ctx = new ORDContext();
                try
                {
                    if (!ctx.Database.CanConnect())
                    {
                        ErrorText.Text = "Нет подключения к базе данных.";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ErrorText.Text = "Ошибка подключения к БД: " + ex.Message;
                    return;
                }

                var user = ctx.Users.Include(u => u.Role).FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    ErrorText.Text = "Пользователь не найден.";
                    return;
                }

                var hash = ComputeHash(password);
                if (user.PasswordHash == null || !hash.SequenceEqual(user.PasswordHash))
                {
                    ErrorText.Text = "Неверный пароль.";
                    return;
                }

                AuthenticatedUser = user;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Ошибка при авторизации: " + ex.Message;
            }
        }

        private static byte[] ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.Unicode.GetBytes(input));
        }

        private void OnFirstRun(object sender, RoutedEventArgs e)
        {
            var dlg = new FirstRunWindow();
            var res = dlg.ShowDialog();
            if (res == true && dlg.CreatedUser != null)
            {
                AuthenticatedUser = dlg.CreatedUser;
                DialogResult = true;
                Close();
            }
        }
    }
}
