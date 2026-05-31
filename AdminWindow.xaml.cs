using System;
using System.Windows;
using System.Windows.Controls;
using UniquenessChecker.Data;
using UniquenessChecker.Models;

namespace UniquenessChecker.Views
{
    public partial class AdminWindow : Window
    {
        private readonly DatabaseRepository _repo = new DatabaseRepository();
        private readonly User _currentUser;

        public AdminWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            UserNameText.Text = $"{user.FullName}\n({user.RoleName})";
            LoadRoles();
            LoadUsers();
        }

        // ── Навигация ─────────────────────────────────────────
        private void OnNavClick(object sender, RoutedEventArgs e)
        {
            var tag = (string)((Button)sender).Tag;
            PanelUsers.Visibility   = tag == "users"   ? Visibility.Visible : Visibility.Collapsed;
            PanelDocs.Visibility    = tag == "docs"    ? Visibility.Visible : Visibility.Collapsed;
            PanelSources.Visibility = tag == "sources" ? Visibility.Visible : Visibility.Collapsed;
            PanelSQL.Visibility     = tag == "sql"     ? Visibility.Visible : Visibility.Collapsed;
            PanelStats.Visibility   = tag == "stats"   ? Visibility.Visible : Visibility.Collapsed;

            if (tag == "docs")    LoadDocs();
            if (tag == "sources") LoadSources();
            if (tag == "stats")   LoadStats();
        }

        private void OnLogout(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        // ── Пользователи ──────────────────────────────────────
        private void LoadRoles()
        {
            NewRole.ItemsSource       = _repo.GetRoles();
            NewRole.DisplayMemberPath = "RoleName";
            NewRole.SelectedValuePath = "RoleId";
            NewRole.SelectedIndex     = 2;   // Студент по умолчанию
        }

        private void LoadUsers()
        {
            UsersGrid.ItemsSource = _repo.GetAllUsers();
        }

        private void OnAddUser(object sender, RoutedEventArgs e)
        {
            var login    = NewLogin.Text.Trim();
            var name     = NewFullName.Text.Trim();
            var email    = NewEmail.Text.Trim();
            var roleId   = (int)NewRole.SelectedValue;

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Хешируем стандартный пароль "Password123" — пользователь сменит сам
                var hash = BCrypt.Net.BCrypt.HashPassword("Password123");
                _repo.AddUser(login, hash, name, email, roleId);
                MessageBox.Show($"Пользователь «{login}» добавлен.\nВременный пароль: Password123",
                                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                NewLogin.Clear(); NewFullName.Clear(); NewEmail.Clear();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDeleteUser(object sender, RoutedEventArgs e)
        {
            var userId = (int)((Button)sender).Tag;
            if (userId == _currentUser.UserId)
            {
                MessageBox.Show("Нельзя удалить текущего пользователя.", "Предупреждение",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show("Деактивировать пользователя?", "Подтверждение",
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _repo.DeleteUser(userId);
                LoadUsers();
            }
        }

        // ── Документы ─────────────────────────────────────────
        private void LoadDocs()
        {
            DocsGrid.ItemsSource = _repo.GetAllDocuments();
        }

        private void OnDocSelected(object sender, SelectionChangedEventArgs e) { }

        private void OnViewReport(object sender, RoutedEventArgs e)
        {
            var docId = (int)((Button)sender).Tag;
            try
            {
                var report = _repo.GetDocumentReport(docId);
                new ReportWindow(report).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Источники ─────────────────────────────────────────
        private void LoadSources()
        {
            SourcesGrid.ItemsSource = null;
            try
            {
                SourcesGrid.ItemsSource = _repo.GetUniquenessSummary().DefaultView;
            }
            catch { }
        }

        // ── SQL-консоль ───────────────────────────────────────
        private void OnExecuteSql(object sender, RoutedEventArgs e)
        {
            var sql = SqlInput.Text.Trim();
            if (string.IsNullOrEmpty(sql)) return;

            try
            {
                var dt = _repo.ExecuteRawQuery(sql);
                SqlResultGrid.ItemsSource = dt.DefaultView;
                SqlStatus.Text     = $"✅ Выполнено. Строк: {dt.Rows.Count}";
                SqlStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                SqlResultGrid.ItemsSource = null;
                SqlStatus.Text     = $"❌ Ошибка: {ex.Message}";
                SqlStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void OnClearSql(object sender, RoutedEventArgs e)
        {
            SqlInput.Clear();
            SqlResultGrid.ItemsSource = null;
            SqlStatus.Text = "";
        }

        // ── Статистика ────────────────────────────────────────
        private void LoadStats()
        {
            try
            {
                StatsRangeGrid.ItemsSource = _repo.GetUniquenessSummary().DefaultView;
                StatsUserGrid.ItemsSource  = _repo.GetUserStats().DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
