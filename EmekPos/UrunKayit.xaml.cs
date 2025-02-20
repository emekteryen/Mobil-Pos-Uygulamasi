using MySql.Data.MySqlClient;
using ZXing.Net.Maui;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace EmekPos
{
    public partial class UrunKayit : ContentPage
    {
        private const string connectionString = "Server=10.0.2.2;Port=3306;Database=emekpos;User ID=root;Password=;SslMode=none;";
        private bool isCameraOpen = false;

        public UrunKayit()
        {
            InitializeComponent();
        }

        // Kameray� a�ma
        private void OpenCamera_Clicked(object sender, EventArgs e)
        {
            CameraGrid.IsVisible = true;
            isCameraOpen = true;
        }

        // Kameray� kapatma
        private void CloseCamera_Clicked(object sender, EventArgs e)
        {
            CameraGrid.IsVisible = false;
            isCameraOpen = false;
        }

        // Barkod tarama sonucu
        private async void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (e.Results != null && e.Results.Any())
            {
                string barcode = e.Results.First().Value;

                // UI de�i�ikli�ini ana i� par�ac���na ta��yoruz.
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    barcodeEntry.Text = barcode;
                });

                await SearchProductByBarcode(barcode);
            }
        }

        // �r�n� barkod ile arama ve doldurma
        private async Task SearchProductByBarcode(string barcode)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM urun WHERE barkod = @barkod";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@barkod", barcode);

                using var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    // UI ��elerini ana i� par�ac���na ta��yoruz.
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        productNameEntry.Text = reader["urun_adi"].ToString();
                        priceEntry.Text = reader["urun_fiyati"].ToString();
                        quantityEntry.Text = reader["urunAdedi"].ToString();
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("�r�n Bulunamad�", "Yeni �r�n kayd� yap�lacak.", "Tamam");
                    });
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Hata", $"Bir hata olu�tu: {ex.Message}", "Tamam");
                });
            }
        }

        // �r�n kaydetme veya g�ncelleme
        private async void SaveProduct_Clicked(object sender, EventArgs e)
        {
            string barcode = barcodeEntry.Text;
            string productName = productNameEntry.Text;
            string priceText = priceEntry.Text;
            string quantityText = quantityEntry.Text;

            if (string.IsNullOrWhiteSpace(barcode) || string.IsNullOrWhiteSpace(productName) ||
                !decimal.TryParse(priceText, out decimal price) || !int.TryParse(quantityText, out int quantity))
            {
                await DisplayAlert("Hata", "L�tfen t�m bilgileri do�ru girin.", "Tamam");
                return;
            }

            int kullaniciId = 1;//GetLoggedInUserId(); // Oturum a�an kullan�c� ID'si
            await SaveOrUpdateProduct(barcode, productName, price, quantity, kullaniciId);

        }

        // Kaydetme veya g�ncelleme i�lemi
        private async Task SaveOrUpdateProduct(string barcode, string productName, decimal price, int quantity, int kullaniciId)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM urun WHERE barkod = @barkod";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@barkod", barcode);

                int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                query = count > 0
                    ? "UPDATE urun SET urun_adi = @urunAdi, urun_fiyati = @urunFiyati, urunAdedi = @urunAdedi, kullanici_id = @kullaniciId WHERE barkod = @barkod"
                    : "INSERT INTO urun (barkod, urun_adi, urun_fiyati, Stok, kullanici_id) VALUES (@barkod, @urunAdi, @urunFiyati, @urunAdedi, @kullaniciId)";

                using var updateCmd = new MySqlCommand(query, connection);
                updateCmd.Parameters.AddWithValue("@barkod", barcode);
                updateCmd.Parameters.AddWithValue("@urunAdi", productName);
                updateCmd.Parameters.AddWithValue("@urunFiyati", price);
                updateCmd.Parameters.AddWithValue("@urunAdedi", quantity);
                updateCmd.Parameters.AddWithValue("@kullaniciId", kullaniciId);

                await updateCmd.ExecuteNonQueryAsync();
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ba�ar�l�", "�r�n kaydedildi veya g�ncellendi.", "Tamam");
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Hata", $"Bir hata olu�tu: {ex.Message}", "Tamam");
                });
            }
        }

    }
}
