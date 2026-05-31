using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UniquenessChecker.Data;
using UniquenessChecker.Models;

namespace UniquenessChecker.Views
{
    public partial class ClientWindow : Window
    {
        private readonly DatabaseRepository _repo = new DatabaseRepository();
        private readonly User _currentUser;
        private string? _selectedFilePath;

        public ClientWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            UserNameText.Text = $"{user.FullName}\n({user.RoleName})";

            if (user.IsTeacher)
                BtnStudentWorks.Visibility = Visibility.Visible;

            LoadMyDocs();
        }

        // ── Навигация ─────────────────────────────────────────
        private void OnNavClick(object sender, RoutedEventArgs e)
        {
            var tag = (string)((Button)sender).Tag;
            PanelDocs.Visibility         = tag == "docs"         ? Visibility.Visible : Visibility.Collapsed;
            PanelUpload.Visibility       = tag == "upload"       ? Visibility.Visible : Visibility.Collapsed;
            PanelStudentWorks.Visibility = tag == "studentworks" ? Visibility.Visible : Visibility.Collapsed;

            if (tag == "docs")         LoadMyDocs();
            if (tag == "studentworks") LoadStudentWorks();
        }

        private void OnLogout(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        // ── Мои документы ─────────────────────────────────────
        private void LoadMyDocs()
        {
            try { MyDocsGrid.ItemsSource = _repo.GetDocumentsByUser(_currentUser.UserId); }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnViewMyReport(object sender, RoutedEventArgs e)
        {
            var docId = (int)((Button)sender).Tag;
            try
            {
                var report = _repo.GetDocumentReport(docId);
                new ReportWindow(report, _currentUser).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Загрузка файла ────────────────────────────────────
        private void OnBrowseFile(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Выберите документ для проверки",
                Filter = "Документы (*.docx;*.pdf;*.txt;*.doc)|*.docx;*.pdf;*.txt;*.doc|Все файлы|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _selectedFilePath = dlg.FileName;
                FilePathBox.Text  = dlg.FileName;
                UploadStatus.Text = "";
            }
        }

        private async void OnUploadFile(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                MessageBox.Show("Сначала выберите файл.", "Предупреждение",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                UploadProgress.Visibility = Visibility.Visible;
                UploadStatus.Text         = "⏳ Загрузка и проверка...";

                var ext    = Path.GetExtension(_selectedFilePath).ToLower();
                var name   = Path.GetFileName(_selectedFilePath);
                var sizeKb = (int)(new FileInfo(_selectedFilePath).Length / 1024) + 1;

                // Добавляем документ в БД
                var docId = _repo.AddDocument(name, sizeKb, ext, _currentUser.UserId);

                // Симуляция проверки (в реальной системе — вызов CheckEngine)
                await System.Threading.Tasks.Task.Delay(2000);
                var rand        = new Random();
                var uniqueness  = (decimal)(rand.Next(55, 98));
                var sources     = new[]
                {
                    ("Интернет-источник 1", "https://example.com/source1", (decimal)rand.Next(3, 15)),
                    ("Учебное пособие",     null,                          (decimal)rand.Next(2, 8)),
                };

                foreach (var (sName, sUrl, sPct) in sources)
                    _repo.AddBorrowingSource(docId, sName, sUrl, sPct);

                _repo.SaveCheckResult(docId, uniqueness, $"Проверено источников: {sources.Length}", _currentUser.UserId);

                UploadProgress.Visibility = Visibility.Collapsed;
                UploadStatus.Text         = $"✅ Готово! Уникальность: {uniqueness}%";
                UploadStatus.Foreground   = uniqueness >= 80
                    ? System.Windows.Media.Brushes.Green
                    : uniqueness >= 60
                        ? System.Windows.Media.Brushes.DarkOrange
                        : System.Windows.Media.Brushes.Red;

                // Показываем отчёт
                var report = _repo.GetDocumentReport(docId);
                new ReportWindow(report, _currentUser).ShowDialog();
            }
            catch (Exception ex)
            {
                UploadProgress.Visibility = Visibility.Collapsed;
                UploadStatus.Text         = $"❌ Ошибка: {ex.Message}";
                UploadStatus.Foreground   = System.Windows.Media.Brushes.Red;
            }
        }

        // ── Работы студентов (преподаватель) ──────────────────
        private void LoadStudentWorks()
        {
            try { StudentWorksGrid.ItemsSource = _repo.GetAllDocuments(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
