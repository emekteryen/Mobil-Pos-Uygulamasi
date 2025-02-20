using MySql.Data.MySqlClient;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Data;

namespace EmekPos
{
    public partial class stokTakip : ContentPage
    {
        // Veri Modeli
        public class ProductModel
        {
            public string ProductName { get; set; }
            public decimal ProductPrice { get; set; }
            public int Quantity { get; set; }
        }

        // Veritabanı Bağlantısı
        public class DatabaseService
        {
            private string _connectionString = "Server=10.0.2.2;Port=3306;Database=emekpos;User ID=root;Password=;SslMode=none;"; // Bağlantı dizesini burada belirtin

            public async Task<List<ProductModel>> GetProductsFromDatabase()
            {
                var products = new List<ProductModel>();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT urun_adi AS ProductName, urun_fiyati AS ProductPrice, stok AS Quantity FROM urun where kullanici_id=1"; // SQL sorgusunu düzenleyin
                    var command = new MySqlCommand(query, connection);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var product = new ProductModel
                            {
                                ProductName = reader.GetString("ProductName"),
                                ProductPrice = reader.GetDecimal("ProductPrice"),
                                Quantity = reader.GetInt32("Quantity")
                            };
                            products.Add(product);
                        }
                    }
                }

                return products;
            }
        }

        // ViewModel yerine doğrudan Page içinde veri yükleme
        public ObservableCollection<ProductModel> stoklistesi { get; set; }

        public stokTakip()
        {
            stoklistesi = new ObservableCollection<ProductModel>();
            BindingContext = this;
            LoadProducts();

            // UI oluşturma işlemi
            var collectionView = new CollectionView
            {
                ItemsSource = stoklistesi,
                BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
                HeightRequest = 200,
            };

            collectionView.ItemTemplate = new DataTemplate(() =>
            {
                var frame = new Frame
                {
                    BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
                    CornerRadius = 15,
                    BorderColor = Microsoft.Maui.Graphics.Colors.Gray,
                    Padding = 10,
                    Margin = 5
                };

                var stackLayout = new StackLayout { Orientation = StackOrientation.Vertical };

                var horizontalStack = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10
                };

                var productNameLabel = new Label { FontSize = 16 };
                productNameLabel.SetBinding(Label.TextProperty, "ProductName");

                var productPriceLabel = new Label { FontSize = 16 };
                productPriceLabel.SetBinding(Label.TextProperty, new Binding("ProductPrice", stringFormat: "₺{0:F2}"));

                var quantityLabel = new Label { Text = "Adet:", FontSize = 16, VerticalOptions = LayoutOptions.Center };
                var quantityValueLabel = new Label { FontSize = 16 };
                quantityValueLabel.SetBinding(Label.TextProperty, "Quantity");

                horizontalStack.Children.Add(productNameLabel);
                horizontalStack.Children.Add(productPriceLabel);
                horizontalStack.Children.Add(quantityLabel);
                horizontalStack.Children.Add(quantityValueLabel);

                stackLayout.Children.Add(horizontalStack);
                frame.Content = stackLayout;

                return frame;
            });

            var verticalStackLayout = new VerticalStackLayout();
            verticalStackLayout.Children.Add(collectionView);

            Content = verticalStackLayout; // Sayfanın içeriğini ayarlıyoruz
        }

        public async void LoadProducts()
        {
            var databaseService = new DatabaseService();
            var products = await databaseService.GetProductsFromDatabase();
            foreach (var product in products)
            {
                stoklistesi.Add(product);
            }
        }
    }
}
