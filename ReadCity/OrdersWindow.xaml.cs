using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using ReadCity.Data;
using ReadCity.Models;

namespace ReadCity
{
    /// <summary>
    /// Логика взаимодействия для OrdersWindow.xaml
    /// </summary>
    public partial class OrdersWindow : Window
    {
        public OrdersWindow()
        {
            InitializeComponent();
            UserInfoTextBlock.Text = CurrentUser.FullName;


            //отображение панели админа
            if (CurrentUser.RoleName == "Администратор")
            {
                AdminOrderPanel.Visibility = Visibility.Visible;
            }

            LoadOrders();
        }

        private void LoadOrders()
        {
            var orders = new List<OrderItemModel>();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    //получение данных о товарах с помощью запроса
                    conn.Open();
                    //сборка составного артикля
                    string sql = @"
                        SELECT o.id,
                               COALESCE(STRING_AGG(p.article || ', ' || oi.quantity, ', ' ORDER BY oi.id), 'Нет товаров') AS articles,
                               o.id_status,
                               s.status_name,
                               o.id_pickup_point,
                               pvz.address,
                               o.order_date,
                               o.delivery_date,
                               o.pickup_code,
                               o.id_user
                        FROM orders o
                        JOIN statuses s ON o.id_status = s.id
                        JOIN pvz ON o.id_pickup_point = pvz.id
                        LEFT JOIN order_items oi ON o.id = oi.id_order
                        LEFT JOIN products p ON oi.id_product = p.id
                        GROUP BY o.id, s.status_name, pvz.address
                        ORDER BY o.id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new OrderItemModel
                            {
                                Id = reader.GetInt32(0),
                                OrderArticles = reader.GetString(1),
                                StatusId = reader.GetInt32(2),
                                StatusName = reader.GetString(3),
                                PickupPointId = reader.GetInt32(4),
                                PickupAddress = reader.GetString(5),
                                OrderDate = reader.GetDateTime(6),
                                DeliveryDate = reader.GetDateTime(7),
                                PickupCode = reader.GetInt32(8),
                                UserId = reader.GetInt32(9)
                            });
                        }
                    }
                }

                OrdersListBox.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов:\n{ex.Message}",
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //кнопка добавления заказа
        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.RoleName != "Администратор") return;

            OrderEditWindow editWindow = new OrderEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        //просмотр данных о заказе
        private void OrdersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUser.RoleName != "Администратор") return;

            if (OrdersListBox.SelectedItem is OrderItemModel selectedOrder)
            {
                OrderEditWindow editWindow = new OrderEditWindow(selectedOrder);
                if (editWindow.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ProductsWindow productsWindow = new ProductsWindow();
            productsWindow.Show();
            this.Close();
        }
    }
}
