using System;
using System.Windows;
using Npgsql;

namespace Demo
{
    public partial class LoginWindow : Window
    {
        string connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=1234";

        public LoginWindow()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT login FROM users";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ComboUsers.Items.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (ComboUsers.SelectedItem == null) { MessageBox.Show("Выберите пользователя!"); return; }

            string selectedLogin = ComboUsers.SelectedItem.ToString();
            string role = "Пользователь";

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = $"SELECT roul FROM users WHERE login = '{selectedLogin}'";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null) role = result.ToString();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); return; }

            MainWindow main = new MainWindow(selectedLogin, role);
            main.Show();
            this.Close();
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = new MainWindow("", "Guest");
            main.Show();
            this.Close();
        }
    }
}