using System;
using System.IO;
using System.Xml.Linq;
using MySql.Data.MySqlClient;
using Microsoft.Maui.Storage;
using System.Data;

namespace EmekPos
{
    public partial class Yedekleme : ContentPage
    {
        private const string connectionString = "Server=localhost;Database=EmekPos;User ID=root;Password=;";
        private readonly string backupFilePath;

        public Yedekleme()
        {
            InitializeComponent();

            // XML dosyas� i�in uygulama veri dizinini kullan�yoruz
            backupFilePath = Path.Combine(FileSystem.AppDataDirectory, "yedekleme.xml");
        }

        // Verileri XML'e yedekleme
        private async void BackupToXml_Clicked(object sender, EventArgs e)
        {
            try
            {
                var document = new XDocument(new XElement("Sat��lar"));
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                string query = "SELECT satis_id, kullanici_id, toplam_fiyat, tarih FROM Satislar";
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (reader.Read())
                {
                    var saleElement = new XElement("Satis",
                        new XElement("SatisId", reader.GetInt32("satis_id")),
                        new XElement("KullaniciId", reader.GetInt32("kullanici_id")),
                        new XElement("ToplamFiyat", reader.GetDecimal("toplam_fiyat")),
                        new XElement("Tarih", reader.GetDateTime("tarih").ToString("yyyy-MM-dd"))
                    );
                    document.Root.Add(saleElement);
                }

                // XML dosyas�n� uygulama veri dizinine kaydet
                document.Save(backupFilePath);
                StatusLabel.Text = $"Yedekleme ba�ar�l�! Dosya kaydedildi: {backupFilePath}";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Yedekleme s�ras�nda bir hata olu�tu: {ex.Message}", "Tamam");
            }
        }

        // XML'den veritaban�na geri y�kleme
        private async void RestoreFromXml_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    await DisplayAlert("Hata", "Yedekleme dosyas� bulunamad�.", "Tamam");
                    return;
                }

                var document = XDocument.Load(backupFilePath);
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                foreach (var saleElement in document.Root.Elements("Satis"))
                {
                    string query = @"
                        INSERT INTO Satislar (satis_id, kullanici_id, toplam_fiyat, tarih)
                        VALUES (@satisId, @kullaniciId, @toplamFiyat, @tarih)
                        ON DUPLICATE KEY UPDATE 
                        toplam_fiyat = @toplamFiyat, tarih = @tarih";

                    using var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@satisId", (int)saleElement.Element("SatisId"));
                    command.Parameters.AddWithValue("@kullaniciId", (int)saleElement.Element("KullaniciId"));
                    command.Parameters.AddWithValue("@toplamFiyat", (decimal)saleElement.Element("ToplamFiyat"));
                    command.Parameters.AddWithValue("@tarih", DateTime.Parse((string)saleElement.Element("Tarih")));

                    await command.ExecuteNonQueryAsync();
                }

                StatusLabel.Text = "Veriler ba�ar�yla geri y�klendi!";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Geri y�kleme s�ras�nda bir hata olu�tu: {ex.Message}", "Tamam");
            }
        }
    }
}
