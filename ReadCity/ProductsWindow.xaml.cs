using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;
using ReadCity.Data;
using ReadCity.Models;
using System.Linq;

namespace ReadCity
{
    /// <summary>
    /// Логика взаимодействия для ProductWindow.xaml
    /// </summary>
    public partial class ProductsWindow : Window
    {
        private List<ProductItem> _allProducts = new List<ProductItem>();

        public ProductsWindow()
        {
            InitializeComponent();
            UserInfoTextBlock.Text = CurrentUser.FullName;

            // Разграничение прав доступа по ролям
            if (CurrentUser.RoleName == "Менеджер" || CurrentUser.RoleName == "Администратор")
            {
                FilterPanel.Visibility = Visibility.Visible;
                OrdersButton.Visibility = Visibility.Visible;
            }
            if (CurrentUser.RoleName == "Администратор")
            {
                AddProductButton.Visibility = Visibility.Visible;
            }

            LoadProducts();
        }

        private void LoadProducts()
        {
            _allProducts.Clear();

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT p.id, p.article, p.product_name, c.category_name, p.description,
                               m.manufacturer_name, s.supplier_name, p.price, u.unit_name,
                               p.stock_quantity, p.discount, p.photo
                        FROM products p
                        JOIN categories c ON p.id_category = c.id
                        JOIN manufacturers m ON p.id_manufacturer = m.id
                        JOIN suppliers s ON p.id_supplier = s.id
                        JOIN units u ON p.id_unit = u.id
                        ORDER BY p.id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allProducts.Add(new ProductItem
                            {
                                Id = reader.GetInt32(0),
                                Article = reader.GetString(1),
                                ProductName = reader.GetString(2),
                                CategoryName = reader.GetString(3),
                                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                                ManufacturerName = reader.GetString(5),
                                SupplierName = reader.GetString(6),
                                Price = reader.GetDecimal(7),
                                UnitName = reader.GetString(8),
                                StockQuantity = reader.GetInt32(9),
                                Discount = reader.GetInt32(10),
                                Photo = reader.IsDBNull(11) ? null : reader.GetString(11)
                            });
                        }
                    }
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров:\n{ex.Message}",
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterControl_Changed(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allProducts == null || ProductsListBox == null)
                return;

            IEnumerable<ProductItem> filtered = _allProducts;

            // 1. Поиск по всем текстовым полям (без учёта регистра)
            string search = SearchTextBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(search)) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)) ||
                    (p.CategoryName != null && p.CategoryName.ToLower().Contains(search)) ||
                    (p.ManufacturerName != null && p.ManufacturerName.ToLower().Contains(search)) ||
                    (p.SupplierName != null && p.SupplierName.ToLower().Contains(search)) ||
                    (p.Article != null && p.Article.ToLower().Contains(search)));
            }

            // 2. Фильтр по диапазонам скидки
            int discountIndex = DiscountFilterComboBox?.SelectedIndex ?? 0;
            switch (discountIndex)
            {
                case 1: // 0-12,99%
                    filtered = filtered.Where(p => p.Discount >= 0 && p.Discount <= 12);
                    break;
                case 2: // 13-16,99%
                    filtered = filtered.Where(p => p.Discount >= 13 && p.Discount <= 16);
                    break;
                case 3: // 17% и более
                    filtered = filtered.Where(p => p.Discount >= 17);
                    break;
            }

            // 3. Сортировка по цене или остатку на складе
            int sortIndex = SortComboBox?.SelectedIndex ?? 0;
            switch (sortIndex)
            {
                case 1:
                    filtered = filtered.OrderBy(p => p.Price);
                    break;
                case 2:
                    filtered = filtered.OrderByDescending(p => p.Price);
                    break;
                case 3:
                    filtered = filtered.OrderBy(p => p.StockQuantity);
                    break;
                case 4:
                    filtered = filtered.OrderByDescending(p => p.StockQuantity);
                    break;
            }

            ProductsListBox.ItemsSource = filtered.ToList();
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.RoleName != "Администратор") return;

            ProductEditWindow editWindow = new ProductEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadProducts();
            }
        }

        private void ProductsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CurrentUser.RoleName != "Администратор") return;

            if (ProductsListBox.SelectedItem is ProductItem selectedProduct)
            {
                ProductEditWindow editWindow = new ProductEditWindow(selectedProduct);
                if (editWindow.ShowDialog() == true)
                {
                    LoadProducts();
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            OrdersWindow ordersWindow = new OrdersWindow();
            ordersWindow.Show();
            this.Close();
        }
    }
}
