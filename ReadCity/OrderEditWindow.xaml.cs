using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using ReadCity.Data;
using ReadCity.Models;

namespace ReadCity
{
    /// <summary>
    /// Логика взаимодействия для OrderEditWindow.xaml
    /// </summary>
    public partial class OrderEditWindow : Window
    {
        private readonly OrderItemModel _editingOrder;

        public OrderEditWindow(OrderItemModel order = null)
        {
            InitializeComponent();
            _editingOrder = order;

            LoadLookups();

            if (_editingOrder == null)
            {
                Title = "ООО «ЧитайГород» — Добавление заказа";
                OrderDatePicker.SelectedDate = DateTime.Today;
                DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(7);
            }
            else
            {
                Title = "ООО «ЧитайГород» — Редактирование заказа";
                DeleteButton.Visibility = Visibility.Visible;

                ArticlesTextBox.Text = _editingOrder.OrderArticles == "Нет товаров" ? "" : _editingOrder.OrderArticles;
                OrderDatePicker.SelectedDate = _editingOrder.OrderDate;
                DeliveryDatePicker.SelectedDate = _editingOrder.DeliveryDate;

                foreach (LookupItem item in StatusComboBox.Items)
                    if (item.Id == _editingOrder.StatusId) StatusComboBox.SelectedItem = item;

                foreach (LookupItem item in PickupPointComboBox.Items)
                    if (item.Id == _editingOrder.PickupPointId) PickupPointComboBox.SelectedItem = item;
            }
        }

        private void LoadLookups()
        {
            var statuses = new List<LookupItem>();
            var pvzList = new List<LookupItem>();
            var productsList = new List<string>();

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand("SELECT id, status_name FROM statuses ORDER BY id", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) statuses.Add(new LookupItem { Id = r.GetInt32(0), Name = r.GetString(1) });

                using (var cmd = new NpgsqlCommand("SELECT id, address FROM pvz ORDER BY id", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) pvzList.Add(new LookupItem { Id = r.GetInt32(0), Name = r.GetString(1) });

                using (var cmd = new NpgsqlCommand("SELECT article, product_name FROM products ORDER BY article", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) productsList.Add($"{r.GetString(0)} — {r.GetString(1)}");
            }

            StatusComboBox.ItemsSource = statuses;
            PickupPointComboBox.ItemsSource = pvzList;
            ProductSelectComboBox.ItemsSource = productsList;

            if (statuses.Count > 0) StatusComboBox.SelectedIndex = 0;
            if (pvzList.Count > 0) PickupPointComboBox.SelectedIndex = 0;
            if (productsList.Count > 0) ProductSelectComboBox.SelectedIndex = 0;
        }

        private void StatusComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (StatusComboBox.SelectedItem is LookupItem selected && selected.Name == "Завершен")
            {
                DeliveryDatePicker.SelectedDate = DateTime.Today;
            }
        }

        private void AddProductToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ProductSelectComboBox.SelectedItem == null) return;

            if (!int.TryParse(ProductQtyTextBox.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Введите корректное количество (целое число больше 0).",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedText = ProductSelectComboBox.SelectedItem.ToString();
            string article = selectedText.Split(new[] { " — " }, StringSplitOptions.None)[0].Trim();

            string newEntry = $"{article}, {qty}";
            if (string.IsNullOrWhiteSpace(ArticlesTextBox.Text))
                ArticlesTextBox.Text = newEntry;
            else
                ArticlesTextBox.Text = ArticlesTextBox.Text.Trim().TrimEnd(',') + ", " + newEntry;
        }

        //разбор строки вида "А112Т4, 2, G843H5, 1" в список (артикул, количество)
        private List<(string Article, int Quantity)> ParseOrderArticles(string input)
        {
            var result = new List<(string, int)>();
            string[] parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0 || parts.Length % 2 != 0)
                throw new Exception("Укажите пары «Артикул, Количество» через запятую (например: А112Т4, 2, G843H5, 1).");

            for (int i = 0; i < parts.Length; i += 2)
            {
                string article = parts[i].Trim();
                if (!int.TryParse(parts[i + 1].Trim(), out int qty) || qty <= 0)
                    throw new Exception($"Некорректное количество для артикула «{article}».");

                result.Add((article, qty));
            }

            return result;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (StatusComboBox.SelectedValue == null || PickupPointComboBox.SelectedValue == null ||
                OrderDatePicker.SelectedDate == null || DeliveryDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Заполните все обязательные поля заказа (*).",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DeliveryDatePicker.SelectedDate < OrderDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата доставки не может быть раньше даты заказа.",
                    "Ошибка дат", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<(string Article, int Quantity)> items;
            try
            {
                items = ParseOrderArticles(ArticlesTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка формата артикула", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int statusId = (int)StatusComboBox.SelectedValue;
            int pvzId = (int)PickupPointComboBox.SelectedValue;
            DateTime orderDate = OrderDatePicker.SelectedDate.Value;
            DateTime deliveryDate = DeliveryDatePicker.SelectedDate.Value;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        // Проверяем существование всех артикулов в таблице products
                        var resolvedItems = new List<(int ProductId, int Quantity)>();
                        foreach (var item in items)
                        {
                            using (var findCmd = new NpgsqlCommand("SELECT id FROM products WHERE LOWER(article) = LOWER(@art)", conn, tx))
                            {
                                findCmd.Parameters.AddWithValue("@art", item.Article);
                                var prodIdObj = findCmd.ExecuteScalar();
                                if (prodIdObj == null)
                                {
                                    MessageBox.Show($"Товар с артикулом «{item.Article}» не найден в каталоге!",
                                        "Неизвестный артикул", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                                resolvedItems.Add((Convert.ToInt32(prodIdObj), item.Quantity));
                            }
                        }

                        int orderId;
                        if (_editingOrder == null)
                        {
                            // Генерируем следующий код получения (MAX + 1)
                            int nextCode = 911;
                            using (var codeCmd = new NpgsqlCommand("SELECT COALESCE(MAX(pickup_code), 900) + 1 FROM orders", conn, tx))
                                nextCode = Convert.ToInt32(codeCmd.ExecuteScalar());

                            int userId = CurrentUser.Id > 0 ? CurrentUser.Id : 1;

                            string insertOrder = @"
                                INSERT INTO orders (order_date, delivery_date, id_pickup_point, id_user, pickup_code, id_status)
                                VALUES (@od, @dd, @pvz, @uid, @code, @st) RETURNING id";

                            using (var cmd = new NpgsqlCommand(insertOrder, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@od", orderDate);
                                cmd.Parameters.AddWithValue("@dd", deliveryDate);
                                cmd.Parameters.AddWithValue("@pvz", pvzId);
                                cmd.Parameters.AddWithValue("@uid", userId);
                                cmd.Parameters.AddWithValue("@code", nextCode);
                                cmd.Parameters.AddWithValue("@st", statusId);
                                orderId = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }
                        else
                        {
                            orderId = _editingOrder.Id;
                            string updateOrder = @"
                                UPDATE orders
                                SET order_date = @od, delivery_date = @dd, id_pickup_point = @pvz, id_status = @st
                                WHERE id = @id";

                            using (var cmd = new NpgsqlCommand(updateOrder, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@od", orderDate);
                                cmd.Parameters.AddWithValue("@dd", deliveryDate);
                                cmd.Parameters.AddWithValue("@pvz", pvzId);
                                cmd.Parameters.AddWithValue("@st", statusId);
                                cmd.Parameters.AddWithValue("@id", orderId);
                                cmd.ExecuteNonQuery();
                            }

                            using (var delItems = new NpgsqlCommand("DELETE FROM order_items WHERE id_order = @id", conn, tx))
                            {
                                delItems.Parameters.AddWithValue("@id", orderId);
                                delItems.ExecuteNonQuery();
                            }
                        }

                        foreach (var ri in resolvedItems)
                        {
                            using (var insItem = new NpgsqlCommand("INSERT INTO order_items (id_order, id_product, quantity) VALUES (@oid, @pid, @qty)", conn, tx))
                            {
                                insItem.Parameters.AddWithValue("@oid", orderId);
                                insItem.Parameters.AddWithValue("@pid", ri.ProductId);
                                insItem.Parameters.AddWithValue("@qty", ri.Quantity);
                                insItem.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                }
                MessageBox.Show("Заказ успешно сохранён.", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения заказа:\n{ex.Message}",
                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingOrder == null) return;
            var confirm = MessageBox.Show(
            $"Вы действительно хотите удалить заказ № {_editingOrder.Id}?\nЭто действие необратимо.",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
                return;
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM orders WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editingOrder.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Заказ успешно удалён.", "Удалено",
                MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении заказа:\n{ex.Message}",
                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
