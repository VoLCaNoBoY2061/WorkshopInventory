using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;

namespace WorkshopInventory.Views
{
    public class RequestRow
    {
        public int Id { get; set; }
        public int MasterId { get; set; } // Добавлено для фильтрации уведомлений
        public string MasterName { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string Comment { get; set; } = string.Empty;

        // Вычисляемое свойство для триггера в XAML
        public bool IsRejected => Status == "Отклонена";
    }

    public partial class RequestsView : UserControl
    {
        private AppDbContext _db = null!;
        private RequestService _requests = null!;
        private User _user = null!;

        // Флаг, чтобы не показывать уведомление об отклонении каждый раз при обновлении таблицы
        private bool _hasShownRejectionWarning = false;

        public RequestsView()
        {
            InitializeComponent();
            TypeCombo.ItemsSource = Enum.GetValues(typeof(RequestType));
            TypeCombo.SelectedIndex = 0;
        }

        public void Initialize(AppDbContext db, RequestService requests, User user)
        {
            _db = db;
            _requests = requests;
            _user = user;
            _hasShownRejectionWarning = false; // Сбрасываем флаг при инициализации

            CreatePanel.Visibility = user.Role == UserRole.Мастер ? Visibility.Visible : Visibility.Collapsed;
            ApproveButton.Visibility = RejectButton.Visibility =
                user.Role == UserRole.Руководитель ? Visibility.Visible : Visibility.Collapsed;
            FulfillButton.Visibility = user.Role == UserRole.Кладовщик ? Visibility.Visible : Visibility.Collapsed;

            if (user.Role == UserRole.Мастер)
                MaterialCombo.ItemsSource = _db.Materials.OrderBy(m => m.Name).ToList();

            Refresh();
        }

        public void Refresh()
        {
            if (_db == null) return;

            if (_user.Role == UserRole.Мастер)
                MaterialCombo.ItemsSource = _db.Materials.OrderBy(m => m.Name).ToList();

            var list = _db.Requests
                .Select(r => new RequestRow
                {
                    Id = r.Id,
                    MasterId = r.MasterId,
                    MasterName = r.Master!.FullName,
                    MaterialName = r.Material!.Name,
                    Quantity = r.Quantity,
                    Type = r.Type.ToString(),
                    Status = r.Status.ToString(),
                    DateCreated = r.DateCreated,
                    Comment = r.Comment
                })
                .ToList();

            if (_user.Role == UserRole.Мастер)
            {
                list = list.Where(r => r.MasterId == _user.Id).ToList();

                // ВИЗУАЛЬНОЕ УВЕДОМЛЕНИЕ ДЛЯ МАСТЕРА
                if (!_hasShownRejectionWarning && list.Any(r => r.IsRejected))
                {
                    MessageBox.Show(
                        "Внимание: У вас есть отклонённые заявки.\nПожалуйста, проверьте колонку 'Комментарий' и красные строки в таблице, чтобы узнать причину.",
                        "Уведомление о заявках",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    _hasShownRejectionWarning = true; // Больше не показываем при простых обновлениях
                }
            }
            else if (_user.Role == UserRole.Кладовщик)
            {
                list = list.Where(r => r.Status == RequestStatus.Одобрена.ToString()).ToList();
            }

            Grid.ItemsSource = list.OrderByDescending(r => r.DateCreated).ToList();
        }

        private void OnCreateRequest(object sender, RoutedEventArgs e)
        {
            if (MaterialCombo.SelectedItem is not Material material)
            {
                MessageBox.Show("Выберите материал", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(QtyBox.Text, out var qty) || qty <= 0)
            {
                MessageBox.Show("Количество должно быть больше 0", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                QtyBox.Focus();
                return;
            }

            var type = (RequestType)TypeCombo.SelectedItem;

            // ИСПРАВЛЕНО: передаём _user вместо _user.Id
            _requests.CreateRequest(_user, material.Id, qty, type, CommentBox.Text.Trim());

            QtyBox.Clear();
            CommentBox.Clear();
            MessageBox.Show("Заявка успешно создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Refresh();
        }

        private void OnApprove(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not RequestRow row) return;

            // ИСПРАВЛЕНО: передаём _user
            _requests.Approve(row.Id, _user);
            MessageBox.Show("Заявка одобрена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Refresh();
        }

        private void OnReject(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not RequestRow row) return;

            // ИСПРАВЛЕНО: передаём _user
            _requests.Reject(row.Id, "Отклонено руководителем", _user);
            MessageBox.Show("Заявка отклонена.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            Refresh();
        }

        private void OnFulfill(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not RequestRow row) return;
            try
            {
                // ИСПРАВЛЕНО: передаём _user
                var ok = _requests.Fulfill(row.Id, _user);
                MessageBox.Show(
                    ok ? "Материал успешно выдан/принят!" : "Недостаточно материала на складе — заявка отклонена, мастер уведомлён.",
                    ok ? "Успех" : "Внимание",
                    MessageBoxButton.OK,
                    ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRefresh(object sender, RoutedEventArgs e) => Refresh();

        // Метод валидации ввода (чтобы нельзя было ввести буквы в количество)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}