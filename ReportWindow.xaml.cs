using System.Windows;
using System.Windows.Media;
using UniquenessChecker.Data;
using UniquenessChecker.Models;

namespace UniquenessChecker.Views
{
    public partial class ReportWindow : Window
    {
        private readonly DatabaseRepository _repo = new DatabaseRepository();
        private readonly DocumentReport _report;
        private readonly User? _currentUser;

        public ReportWindow(DocumentReport report, User? user = null)
        {
            InitializeComponent();
            _report      = report;
            _currentUser = user;
            LoadReport();

            if (user?.IsTeacher == true)
                CommentForm.Visibility = Visibility.Visible;
        }

        private void LoadReport()
        {
            var doc = _report.Document;
            FileNameText.Text = $"📄 {doc.FileName}";
            AuthorText.Text   = $"Автор: {doc.AuthorName}   •   Логин: {doc.AuthorLogin}";
            DateText.Text     = $"Дата загрузки: {doc.UploadDate:dd.MM.yyyy HH:mm}   •   Размер: {doc.FileSizeKb} КБ";

            // Индикатор уникальности
            if (doc.UniquenessPercent.HasValue)
            {
                var pct = doc.UniquenessPercent.Value;
                UniqPercent.Text  = $"{pct:0}%";
                UniqCircle.Background = pct >= 80
                    ? new SolidColorBrush(Color.FromRgb(39, 174, 96))    // зелёный
                    : pct >= 60
                        ? new SolidColorBrush(Color.FromRgb(243, 156, 18)) // жёлтый
                        : new SolidColorBrush(Color.FromRgb(231, 76, 60)); // красный
            }
            else
            {
                UniqPercent.Text  = "—";
                UniqCircle.Background = Brushes.Gray;
            }

            SourcesGrid.ItemsSource  = _report.Sources;
            CommentsPanel.ItemsSource = _report.Comments;

            Title = $"Отчёт — {doc.FileName}";
        }

        private void OnSaveComment(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null) return;
            var body = CommentBox.Text.Trim();
            if (string.IsNullOrEmpty(body))
            {
                MessageBox.Show("Введите текст комментария.", "Предупреждение",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _repo.AddComment(_report.Document.DocumentId, _currentUser.UserId,
                             FragmentBox.Text.Trim(), body);

            MessageBox.Show("Комментарий сохранён.", "Готово",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            FragmentBox.Clear();
            CommentBox.Clear();

            // Обновляем список
            var updated = _repo.GetDocumentReport(_report.Document.DocumentId);
            CommentsPanel.ItemsSource = updated.Comments;
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();
    }
}
