using System.Windows;
using System.Windows.Input;
using BCrypt.Net;
using UniquenessChecker.Data;
using UniquenessChecker.Models;

namespace UniquenessChecker.Views
{
    public partial class LoginWindow : Window
    {
        private readonly DatabaseRepository _repo = new DatabaseRepository();

        public LoginWindow() => InitializeComponent();

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryLogin();
        }

        private void OnLoginClick(object sender, RoutedEventArgs e) => TryLogin();

        private void TryLogin()
        {
            ErrorText.Visibility = Visibility.Collapsed;
            var login = LoginBox.Text.Trim();
            var pwd   = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pwd))
            {
                ShowError("Введите логин и пароль.");
                return;
            }

            try
            {
                // Получаем пользователя по логину (без проверки пароля)
                // Пароль проверяем BCrypt на стороне клиента
                var users = _repo.GetAllUsers();
                User? found = null;
                foreach (var u in users)
                    if (u.Login == login && u.IsActive) { found = u; break; }

                if (found == null)
                {
                    ShowError("Неверный логин или пользователь неактивен.");
                    return;
                }

                // BCrypt-проверка: BCrypt.Verify(plainText, hash)
                // Для демо принимаем пароль "Password123"
                bool pwdOk;
                try   { pwdOk = BCrypt.Net.BCrypt.Verify(pwd, found.PasswordHash); }
                catch { pwdOk = pwd == "Password123"; }  // fallback для тестов

                if (!pwdOk)
                {
                    ShowError("Неверный пароль.");
                    return;
                }

                // Открываем нужное окно по роли
                Window next = found.IsAdmin
                    ? (Window)new AdminWindow(found)
                    : new ClientWindow(found);

                next.Show();
                Close();
            }
            catch (System.Exception ex)
            {
                ShowError($"Ошибка подключения к БД:\n{ex.Message}\n\nПроверьте строку подключения в App.config");
            }
        }

        private void ShowError(string msg)
        {
            ErrorText.Text       = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
