using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace Demo
{
    public partial class MainWindow : Window
    {
        public string CurrentLogin { get; set; }
        public string CurrentRole { get; set; }
        string connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=1234";

        List<CartItem> cart = new List<CartItem>();

        public MainWindow(string login, string role)
        {
            InitializeComponent();
            CurrentLogin = login;
            CurrentRole = role;

            if (role == "Guest")
            {
                TxtWelcome.Text = "Добро пожаловать, Гость!";
                FilterPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtWelcome.Text = $"Добро пожаловать, {login} (Роль: {role})";
            }

            LoadProducts();
        }

        private void LoadProducts()
        {
            List<Product> products = new List<Product>();
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sqlProducts = "SELECT nameproduct, category, price, image FROM products";
                    using (var cmd = new NpgsqlCommand(sqlProducts, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string imageName = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();

                            // Формируем путь (используем пакетный URI, он самый надежный)
                            string imagePath = "";
                            if (!string.IsNullOrEmpty(imageName))
                            {
                                imagePath = "pack://application:,,,/Images/" + imageName;
                            }

                            products.Add(new Product
                            {
                                NameProduct = reader.GetString(0),
                                Category = reader.GetString(1),
                                Price = Convert.ToInt32(reader.GetValue(2)),
                                TotalStock = 0,
                                ImagePath = imagePath
                            });
                        }
                    }

                    string sqlStock = "SELECT \"nameProduct\", kolvo FROM stockitems";
                    using (var cmd = new NpgsqlCommand(sqlStock, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            int kolvo = Convert.ToInt32(reader.GetValue(1));
                            foreach (var p in products)
                            {
                                if (p.NameProduct == name) p.TotalStock += kolvo;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
                    products = products.Where(p => p.NameProduct.ToLower().Contains(TxtSearch.Text.ToLower())).ToList();

                GridProducts.ItemsSource = products;
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadProducts();
        }

        private void GridProducts_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var product = e.Row.Item as Product;
            if (product != null && product.TotalStock <= 3)
            {
                e.Row.Background = (SolidColorBrush)Application.Current.Resources["ColorWarning"];
            }
        }

        private void BtnAddToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (GridProducts.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар из каталога!");
                return;
            }

            var selectedProduct = (Product)GridProducts.SelectedItem;

            if (selectedProduct.TotalStock <= 0)
            {
                MessageBox.Show("Этого товара нет в наличии!");
                return;
            }

            var existing = cart.FirstOrDefault(c => c.NameProduct == selectedProduct.NameProduct);
            if (existing != null)
            {
                existing.Kolvo++;
                existing.Summa = existing.Kolvo * existing.Price;
            }
            else
            {
                cart.Add(new CartItem
                {
                    NameProduct = selectedProduct.NameProduct,
                    Price = selectedProduct.Price,
                    Kolvo = 1,
                    Summa = selectedProduct.Price
                });
            }

            UpdateCart();
        }

        private void UpdateCart()
        {
            GridCart.ItemsSource = null;
            GridCart.ItemsSource = cart;
            int total = cart.Sum(c => c.Summa);
            TxtTotal.Text = $"Итого: {total} руб.";
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста! Добавьте товары.");
                return;
            }

            string fio = CurrentRole == "Guest" ? "Гость" : CurrentLogin;
            int nextOrderNumber = 1;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(MAX(numberorder), 0) + 1 FROM orders", conn))
                    {
                        nextOrderNumber = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    string currentDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                    foreach (var item in cart)
                    {
                        string sqlInsert = $@"
                            INSERT INTO orders (numberorder, dateorder, fio, category, nameorder, proizvodstvo, razmer, kolvo, buy) 
                            VALUES ({nextOrderNumber}, '{currentDate}', '{fio}', 'Обувь', '{item.NameProduct}', 'Не указано', 0, {item.Kolvo}, {item.Price})";

                        using (var cmd = new NpgsqlCommand(sqlInsert, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show($"Заказ №{nextOrderNumber} успешно оформлен!");
                cart.Clear();
                UpdateCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении заказа: " + ex.Message);
            }
        }
    }

    public class Product
    {
        public string NameProduct { get; set; }
        public string Category { get; set; }
        public int Price { get; set; }
        public int TotalStock { get; set; }

        public string ImagePath { get; set; } 
    }

    public class CartItem
    {
        public string NameProduct { get; set; }
        public int Price { get; set; }
        public int Kolvo { get; set; }
        public int Summa { get; set; }
    }
}