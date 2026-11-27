using Npgsql;
using ReadCity.Data;
using ReadCity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ReadCity
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            //валидация ввведённых данных
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                //создание объекта бд
                using (var conn = DbHelper.GetConnection())
                {
                    //получение данных о роли через запрос
                    conn.Open();
                    string sql = @"
                        SELECT u.id, u.last_name, u.first_name, u.middle_name, r.role_name
                        FROM users u
                        JOIN roles r ON u.id_role = r.id
                        WHERE u.login = @login AND u.password = @password";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            //поиск польззователя в бд
                            if (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string lastName = reader.GetString(1);
                                string firstName = reader.GetString(2);
                                string middleName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                string fullName = $"{lastName} {firstName} {middleName}".Trim();
                                string roleName = reader.GetString(4);

                                CurrentUser.SetUser(id, fullName, roleName);

                                OpenProductsWindow();
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль.",
                                    "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //вход гостя
        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentUser.SetGuest();
            OpenProductsWindow();
        }

        private void OpenProductsWindow()
        {
            ProductsWindow productsWindow = new ProductsWindow();
            productsWindow.Show();
            this.Close();
        }
    }
}
