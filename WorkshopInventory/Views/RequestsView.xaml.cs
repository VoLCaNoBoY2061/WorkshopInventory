using System.Windows;
using System.Windows.Controls;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;

namespace WorkshopInventory.Views
{
    public class RequestRow
    {
        public int Id { get; set; }
        public string MasterName { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    public partial class RequestsView : UserControl
    {
        private AppDbContext _db = null!;
        private RequestService _requests = null!;
        private User _user = null!;

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

            // Видимость элементов управления зависит от роли — как на use case диаграмме
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

            // Список материалов у Мастера мог устареть, если Кладовщик добавил новый —
            // перечитываем при каждом обновлении, а не только при первом входе.
            if (_user.Role == UserRole.Мастер)
                MaterialCombo.ItemsSource = _db.Materials.OrderBy(m => m.Name).ToList();

            var list = _db.Requests
                .Select(r => new RequestRow
                {
                    Id = r.Id,
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
                list = list.Where(r => r.MasterName == _user.FullName).ToList();
            else if (_user.Role == UserRole.Кладовщик)
                list = list.Where(r => r.Status == RequestStatus.Одобрена.ToString()).ToList();

            Grid.ItemsSource = list.OrderByDescending(r => r.DateCreated).ToList();
        }

        private void OnCreateRequest(object sender, RoutedEventArgs e)
        {
            if (MaterialCombo.SelectedItem is not Material material) { MessageBox.Show("Выберите материал"); return; }
            if (!decimal.TryParse(QtyBox.Text, out var qty) || qty <= 0) { MessageBox.Show("Некорректное количество"); return; }

            var type = (RequestType)TypeCombo.SelectedItem;
            _requests.CreateRequest(_user.Id, material.Id, qty, type, CommentBox.Text.Trim());

            QtyBox.Clear();
            CommentBox.Clear();
            Refresh();
        }

        private void OnApprove(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not RequestRow row) return;
            _requests.Approve(row.Id);
            Refresh();
        }

        private void OnReject(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not RequestRow row) return;
            _requests.Reject(row.Id, "Отклонено руководителем");
            Refresh();
        }

        private void OnFulfill(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not RequestRow row) return;
            try
            {
                var ok = _requests.Fulfill(row.Id, _user.Id);
                MessageBox.Show(ok ? "Операция выполнена" : "Недостаточно материала — заявка отклонена, мастер уведомлён");
                Refresh();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OnRefresh(object sender, RoutedEventArgs e) => Refresh();
    }
}
