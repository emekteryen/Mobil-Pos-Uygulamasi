using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.Maui.Dispatching;
using System.Data;

namespace EmekPos
{
    public partial class SatisGecmisi : ContentPage
    {
        private const string connectionString = "Server=10.0.2.2;Port=3306;Database=emekpos;User ID=root;Password=;SslMode=none;";
        private int currentFisIndex = 0; // �u anki fi�in s�ras�
        private string currentDate = ""; // Kullan�c�n�n girdi�i tarih
        private int userId = 1; // �rnek olarak sabit bir kullan�c� ID'si

        public SatisGecmisi()
        {
            InitializeComponent();
        }

        // Tarih ile fi� arama
        private async void SearchFisByDate_Clicked(object sender, EventArgs e)
        {
            currentDate = DateEntry.Text;

            if (DateTime.TryParse(currentDate, out DateTime parsedDate))
            {
                currentFisIndex = 0; // �lk fi�ten ba�la
                await LoadFisByDateAndIndex(parsedDate, currentFisIndex);
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Hata", "L�tfen ge�erli bir tarih girin.", "Tamam"));
            }
        }

        // Fi� bilgilerini y�kleme ve �r�n detaylar�n� listeleme
        private async Task LoadFisByDateAndIndex(DateTime date, int index)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                string fisQuery = @"
                    SELECT satis_id, toplam_fiyat 
                    FROM Satislar 
                    WHERE kullanici_id = @userId AND DATE(tarih) = @date
                    ORDER BY tarih ASC
                    LIMIT 1 OFFSET @index";

                using var fisCmd = new MySqlCommand(fisQuery, connection);
                fisCmd.Parameters.AddWithValue("@userId", userId);
                fisCmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
                fisCmd.Parameters.AddWithValue("@index", index);

                using var fisReader = await fisCmd.ExecuteReaderAsync();
                if (fisReader.Read())
                {
                    int fisId = fisReader.GetInt32("satis_id");
                    decimal totalAmount = fisReader.GetDecimal("toplam_fiyat");

                    await MainThread.InvokeOnMainThreadAsync(() =>
                        FisInfoLabel.Text = $"Fi� ID: {fisId}, Toplam Tutar: {totalAmount:C}");

                    fisReader.Close();
                    await LoadProductsForFis(connection, fisId);
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Sonu� Yok", "Bu tarihe ait ba�ka fi� bulunamad�.", "Tamam"));
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Hata", $"Bir hata olu�tu: {ex.Message}", "Tamam"));
            }
        }

        // Se�ilen fi�e ait �r�nleri y�kleme
        private async Task LoadProductsForFis(MySqlConnection connection, int fisId)
        {
            try
            {
                string productQuery = @"
                    SELECT d.urun_id, d.adet, d.fiyat, u.urun_adi 
                    FROM SatisDetaylari d
                    JOIN Urunler u ON d.urun_id = u.barkod
                    WHERE d.satis_id = @fisId";

                using var productCmd = new MySqlCommand(productQuery, connection);
                productCmd.Parameters.AddWithValue("@fisId", fisId);

                using var productReader = await productCmd.ExecuteReaderAsync();
                var productList = new List<Product>();

                while (productReader.Read())
                {
                    productList.Add(new Product
                    {
                        UrunAdi = productReader.GetString("urun_adi"),
                        Adet = productReader.GetInt32("adet"),
                        BirimFiyat = productReader.GetDecimal("fiyat")
                    });
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                    ProductsCollectionView.ItemsSource = productList);
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Hata", $"�r�nler y�klenirken bir hata olu�tu: {ex.Message}", "Tamam"));
            }
        }

        // Sonraki fi�e ge�i�
        private async void NextFis_Clicked(object sender, EventArgs e)
        {
            currentFisIndex++;
            if (DateTime.TryParse(currentDate, out DateTime parsedDate))
            {
                await LoadFisByDateAndIndex(parsedDate, currentFisIndex);
            }
        }

        // �nceki fi�e d�n��
        private async void PreviousFis_Clicked(object sender, EventArgs e)
        {
            if (currentFisIndex > 0)
            {
                currentFisIndex--;
                if (DateTime.TryParse(currentDate, out DateTime parsedDate))
                {
                    await LoadFisByDateAndIndex(parsedDate, currentFisIndex);
                }
            }
        }
    }

    public class Product
    {
        public string UrunAdi { get; set; }
        public int Adet { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal ToplamFiyat => Adet * BirimFiyat;
    }
}
