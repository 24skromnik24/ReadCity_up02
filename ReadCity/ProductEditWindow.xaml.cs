using Microsoft.Win32;
using Npgsql;
using ReadCity.Data;
using ReadCity.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ReadCity
{
    /// <summary>
    /// Логика взаимодействия для ProductEditWindow.xaml
    /// </summary>

    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public partial class ProductEditWindow : Window
    {
        private readonly ProductItem _editingProduct;
        private string _selectedSourceImagePath;
        private string _currentPhotoFileName;

        public ProductEditWindow(ProductItem product = null)
        {
            InitializeComponent();
            _editingProduct = product;

            LoadLookups();

            if (_editingProduct == null)
            {
                Title = "ООО «ЧитайГород» — Добавление товара";
                ShowPreviewImage(null);
            }
            else
            {
                Title = "ООО «ЧитайГород» — Редактирование товара";
                IdPanel.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Visible;

                IdTextBox.Text = _editingProduct.Id.ToString();
                ArticleTextBox.Text = _editingProduct.Article;
                NameTextBox.Text = _editingProduct.ProductName;
                ManufacturerTextBox.Text = _editingProduct.ManufacturerName;
                PriceTextBox.Text = _editingProduct.Price.ToString("0.00", CultureInfo.InvariantCulture);
                UnitTextBox.Text = _editingProduct.UnitName;
                StockTextBox.Text = _editingProduct.StockQuantity.ToString();
                DiscountTextBox.Text = _editingProduct.Discount.ToString();
                DescriptionTextBox.Text = _editingProduct.Description;

                // Выбираем категорию и поставщика в ComboBox
                foreach (LookupItem item in CategoryComboBox.Items)
                    if (item.Name == _editingProduct.CategoryName) CategoryComboBox.SelectedItem = item;

                foreach (LookupItem item in SupplierComboBox.Items)
                    if (item.Name == _editingProduct.SupplierName) SupplierComboBox.SelectedItem = item;

                _currentPhotoFileName = _editingProduct.Photo;
                PhotoNameTextBlock.Text = string.IsNullOrEmpty(_currentPhotoFileName) ? "Заглушка (picture.png)" : _currentPhotoFileName;
                ShowPreviewImage(_editingProduct.ImageSource);
            }
        }

        private void LoadLookups()
        {
            var categories = new List<LookupItem>();
            var suppliers = new List<LookupItem>();

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, category_name FROM categories ORDER BY category_name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) categories.Add(new LookupItem { Id = r.GetInt32(0), Name = r.GetString(1) });

                using (var cmd = new NpgsqlCommand("SELECT id, supplier_name FROM suppliers ORDER BY supplier_name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) suppliers.Add(new LookupItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }

            CategoryComboBox.ItemsSource = categories;
            SupplierComboBox.ItemsSource = suppliers;
            if (categories.Count > 0) CategoryComboBox.SelectedIndex = 0;
            if (suppliers.Count > 0) SupplierComboBox.SelectedIndex = 0;
        }

        private void ShowPreviewImage(BitmapImage source)
        {
            if (source != null)
            {
                PreviewImage.Source = source;
                return;
            }

            string stubPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "picture.png");
            if (File.Exists(stubPath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(stubPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                PreviewImage.Source = bmp;
            }
        }

        private void SelectPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Выберите фотографию товара"
            };

            if (dlg.ShowDialog() == true)
            {
                _selectedSourceImagePath = dlg.FileName;
                PhotoNameTextBlock.Text = Path.GetFileName(_selectedSourceImagePath);

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(_selectedSourceImagePath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                PreviewImage.Source = bmp;
            }
        }

        //уменьшение размера изображения до 300x200 и удаление старого файла при замене
        private string ProcessAndSavePhoto()
        {
            if (string.IsNullOrEmpty(_selectedSourceImagePath))
                return _currentPhotoFileName;

            string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            Directory.CreateDirectory(imagesDir);

            // Удаляем старое фото с диска (если оно было и не является заглушкой)
            if (!string.IsNullOrWhiteSpace(_currentPhotoFileName) && _currentPhotoFileName != "picture.png")
            {
                string oldFilePath = Path.Combine(imagesDir, _currentPhotoFileName);
                if (File.Exists(oldFilePath))
                {
                    try { File.Delete(oldFilePath); } catch { }
                }
            }

            string newFileName = $"{Guid.NewGuid():N}.jpg";
            string destPath = Path.Combine(imagesDir, newFileName);

            using (var srcImage = System.Drawing.Image.FromFile(_selectedSourceImagePath))
            using (var resized = new Bitmap(300, 200))
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(srcImage, 0, 0, 300, 200);
                resized.Save(destPath, ImageFormat.Jpeg);
            }

            return newFileName;
        }

        private int GetOrCreateLookupId(NpgsqlConnection conn, string table, string column, string value)
        {
            using (var checkCmd = new NpgsqlCommand($"SELECT id FROM {table} WHERE LOWER({column}) = LOWER(@val)", conn))
            {
                checkCmd.Parameters.AddWithValue("@val", value);
                var res = checkCmd.ExecuteScalar();
                if (res != null) return Convert.ToInt32(res);
            }

            using (var insCmd = new NpgsqlCommand($"INSERT INTO {table} ({column}) VALUES (@val) RETURNING id", conn))
            {
                insCmd.Parameters.AddWithValue("@val", value);
                return Convert.ToInt32(insCmd.ExecuteScalar());
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string article = ArticleTextBox.Text.Trim();
            string name = NameTextBox.Text.Trim();
            string manufacturer = ManufacturerTextBox.Text.Trim();
            string unit = UnitTextBox.Text.Trim();
            string description = DescriptionTextBox.Text.Trim();

            if (string.IsNullOrEmpty(article) || string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(manufacturer) || string.IsNullOrEmpty(unit) ||
                CategoryComboBox.SelectedValue == null || SupplierComboBox.SelectedValue == null)
            {
                MessageBox.Show("Заполните все обязательные поля, отмеченные звёздочкой (*).",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string priceNormalized = PriceTextBox.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(priceNormalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) || price < 0)
            {
                MessageBox.Show("Цена должна быть корректным неотрицательным числом (например, 450.50).",
                    "Некорректная цена", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(StockTextBox.Text.Trim(), out int stock) || stock < 0)
            {
                MessageBox.Show("Количество на складе должно быть целым неотрицательным числом.",
                    "Некорректное количество", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(DiscountTextBox.Text.Trim(), out int discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Скидка должна быть целым числом от 0 до 100.",
                    "Некорректная скидка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string savedPhoto = ProcessAndSavePhoto();
                int categoryId = (int)CategoryComboBox.SelectedValue;
                int supplierId = (int)SupplierComboBox.SelectedValue;

                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    int manufacturerId = GetOrCreateLookupId(conn, "manufacturers", "manufacturer_name", manufacturer);
                    int unitId = GetOrCreateLookupId(conn, "units", "unit_name", unit);

                    if (_editingProduct == null)
                    {
                        string insertSql = @"
                        INSERT INTO products (article, product_name, id_unit, price, id_supplier,
                        id_manufacturer, id_category, discount, stock_quantity, description, photo)
                        VALUES (@art, @name, @unit, @price, @sup, @man, @cat, @disc, @stock, @desc, @photo)";
                        using (var cmd = new NpgsqlCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@art", article);
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@unit", unitId);
                            cmd.Parameters.AddWithValue("@price", price);
                            cmd.Parameters.AddWithValue("@sup", supplierId);
                            cmd.Parameters.AddWithValue("@man", manufacturerId);
                            cmd.Parameters.AddWithValue("@cat", categoryId);
                            cmd.Parameters.AddWithValue("@disc", discount);
                            cmd.Parameters.AddWithValue("@stock", stock);
                            cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                            cmd.Parameters.AddWithValue("@photo", string.IsNullOrEmpty(savedPhoto) ? (object)DBNull.Value : savedPhoto);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string updateSql = @"
                        UPDATE products
                        SET article = @art, product_name = @name, id_unit = @unit, price = @price,
                        id_supplier = @sup, id_manufacturer = @man, id_category = @cat,
                        discount = @disc, stock_quantity = @stock, description = @desc, photo = @photo
                        WHERE id = @id";
                        using (var cmd = new NpgsqlCommand(updateSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@art", article);
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@unit", unitId);
                            cmd.Parameters.AddWithValue("@price", price);
                            cmd.Parameters.AddWithValue("@sup", supplierId);
                            cmd.Parameters.AddWithValue("@man", manufacturerId);
                            cmd.Parameters.AddWithValue("@cat", categoryId);
                            cmd.Parameters.AddWithValue("@disc", discount);
                            cmd.Parameters.AddWithValue("@stock", stock);
                            cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                            cmd.Parameters.AddWithValue("@photo", string.IsNullOrEmpty(savedPhoto) ? (object)DBNull.Value : savedPhoto);
                            cmd.Parameters.AddWithValue("@id", _editingProduct.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                MessageBox.Show("Данные товара успешно сохранены.", "Успешно",
                MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения товара:\n{ex.Message}",
                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingProduct == null) return;
            var confirm = MessageBox.Show(
            $"Вы действительно хотите удалить товар «{_editingProduct.ProductName}»?\nЭто действие необратимо.",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
                return;
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    // Проверка: присутствует ли товар в заказах
                    using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM order_items WHERE id_product = @id", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", _editingProduct.Id);
                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show(
                            "Невозможно удалить товар, так как он присутствует в оформленных заказах!",
                            "Удаление запрещено", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    using (var delCmd = new NpgsqlCommand("DELETE FROM products WHERE id = @id", conn))
                    {
                        delCmd.Parameters.AddWithValue("@id", _editingProduct.Id);
                        delCmd.ExecuteNonQuery();
                    }
                }
                // Удаляем файл изображения товара с диска (если не заглушка)
                if (!string.IsNullOrWhiteSpace(_editingProduct.Photo) && _editingProduct.Photo != "picture.png")
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", _editingProduct.Photo);
                    if (File.Exists(filePath))
                    {
                        try { File.Delete(filePath); } catch { }
                    }
                }
                MessageBox.Show("Товар успешно удалён.", "Удалено",
                MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении товара:\n{ex.Message}",
                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
