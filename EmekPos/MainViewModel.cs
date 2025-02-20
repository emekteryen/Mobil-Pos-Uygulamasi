using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace EmekPos
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<BarcodeItem> ScannedProducts { get; set; }
        public ICommand UpdateQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand DeleteCommand { get; }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        private const string ConnectionString = "Server=10.0.2.2;Port=3306;Database=emekpos;User ID=root;Password=;SslMode=none;";

        public MainViewModel()
        {
            ScannedProducts = new ObservableCollection<BarcodeItem>();

            UpdateQuantityCommand = new Command<BarcodeItem>(UpdateQuantity);
            DecreaseQuantityCommand = new Command<BarcodeItem>(DecreaseQuantity);
            DeleteCommand = new Command<BarcodeItem>(DeleteProduct);
        }


        /*
        public async Task<List<ProductModel>> GetProductsFromDatabase()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM Products";
                return (await connection.QueryAsync<ProductModel>(query)).ToList();
            }
        }

        public void AddProductToCart(ProductModel product)
        {
            var existingProduct = ScannedProducts.FirstOrDefault(p => p.ProductName == product.ProductName);
            if (existingProduct != null)
            {
                existingProduct.Quantity++;
                existingProduct.TotalPrice = existingProduct.Quantity * existingProduct.ProductPrice;
            }
            else
            {
                ScannedProducts.Add(new ProductModel
                {
                    ProductName = product.ProductName,
                    ProductPrice = product.ProductPrice,
                    Quantity = 1,
                    TotalPrice = product.ProductPrice
                });
            }
            OnPropertyChanged(nameof(ScannedProducts));
            OnPropertyChanged(nameof(TotalPrice));
        }
        */


        public async Task AddBarcodeAsync(string barcodeValue)
        {
            var product = await FetchProductDetailsFromDatabase(barcodeValue);

            if (product != null)
            {
                var existingProduct = ScannedProducts.FirstOrDefault(p => p.Barcode == barcodeValue);

                if (existingProduct != null)
                {
                    existingProduct.Quantity++;
                    existingProduct.TotalPrice = existingProduct.Quantity * existingProduct.ProductPrice;
                }
                else
                {
                    ScannedProducts.Add(new BarcodeItem
                    {
                        Barcode = barcodeValue,
                        ProductName = product.Value.Name,
                        ProductPrice = product.Value.Price,
                        Quantity = 1,
                        TotalPrice = product.Value.Price
                    });
                }

                UpdateTotalPrice();
            }
        }

        private async Task<(string Name, decimal Price)?> FetchProductDetailsFromDatabase(string barcode)
        {
            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            const string query = "SELECT urun_adi, urun_fiyati FROM urun WHERE barkod = @barkod";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@barkod", barcode);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var name = reader.GetString("urun_adi");
                var price = reader.GetDecimal("urun_fiyati");
                return (name, price);
            }

            return null;
        }

        private void UpdateQuantity(BarcodeItem item)
        {
            if (item == null) return;
            item.Quantity++;
            item.TotalPrice = item.Quantity * item.ProductPrice;
            UpdateTotalPrice();
        }

        private void DecreaseQuantity(BarcodeItem item)
        {
            if (item == null) return;

            if (item.Quantity > 1)
            {
                item.Quantity--;
                item.TotalPrice = item.Quantity * item.ProductPrice;
            }
            else
            {
                ScannedProducts.Remove(item);
            }

            UpdateTotalPrice();
        }

        private void DeleteProduct(BarcodeItem item)
        {
            if (item == null) return;
            ScannedProducts.Remove(item);
            UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            TotalPrice = ScannedProducts.Sum(p => p.TotalPrice);
        }

        public void CompleteSale()
        {
            // Satışı veritabanına kaydetme mantığı buraya eklenebilir.
            ScannedProducts.Clear();
            UpdateTotalPrice();
        }



        public async Task CompleteSaleAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    
                    decimal toplamFiyat = ScannedProducts.Sum(p => p.Quantity * p.ProductPrice);
                    int kullaniciId = 1; //GetLoggedInUserId();
                    string insertSaleQuery = "INSERT INTO satis (kullanici_id, toplam_fiyat, tarih) VALUES (@kullanici_id, @toplam_fiyat, NOW());";

                    int satisId;
                    using (var cmd = new MySqlCommand(insertSaleQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@kullanici_id", kullaniciId);
                        cmd.Parameters.AddWithValue("@toplam_fiyat", toplamFiyat);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 2. Eklenen satis_id'yi alın
                    string getLastSaleIdQuery = "SELECT LAST_INSERT_ID();";
                    using (var cmd = new MySqlCommand(getLastSaleIdQuery, connection))
                    {
                        satisId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    // 3. SatisDetaylari tablosuna ürünler eklenir
                    string insertSaleDetailsQuery = "INSERT INTO satisdetay (satis_id, urun_id, adet, fiyat) VALUES (@satis_id, @urun_id, @adet, @fiyat);";
                    foreach (var product in ScannedProducts)
                    {
                        using (var cmd = new MySqlCommand(insertSaleDetailsQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@satis_id", satisId);
                            cmd.Parameters.AddWithValue("@urun_id", product.Barcode);
                            cmd.Parameters.AddWithValue("@adet", product.Quantity);
                            cmd.Parameters.AddWithValue("@fiyat", product.ProductPrice);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 4. Listeyi ve toplam fiyatı temizleyin
                ScannedProducts.Clear();
                TotalPrice = 0;
                OnPropertyChanged(nameof(ScannedProducts));
                OnPropertyChanged(nameof(TotalPrice));
            }
            catch (Exception ex)
            {
                throw new Exception($"Satış kaydedilirken bir hata oluştu: {ex.Message}");
            }
        }



    }



    public class BarcodeItem : BaseViewModel
    {
        private string _barcode;
        private string _productName;
        private decimal _productPrice;
        private int _quantity;
        private decimal _totalPrice;

        public string Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public decimal ProductPrice
        {
            get => _productPrice;
            set => SetProperty(ref _productPrice, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    // Quantity değiştiğinde TotalPrice'ı da güncelle
                    TotalPrice = _quantity * ProductPrice;
                }
            }
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }
    }

}
