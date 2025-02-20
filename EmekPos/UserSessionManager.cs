using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmekPos
{
    public static class UserSessionManager
    {
        public static string LoggedInUsername { get; private set; }

        public static void Login(string username)
        {
            LoggedInUsername = username;
        }

        public static void Logout()
        {
            LoggedInUsername = null;
        }

        public static bool IsUserLoggedIn => !string.IsNullOrEmpty(LoggedInUsername);


        /*private const string connectionString = "Server=10.0.2.2;Port=3306;Database=emekpos;User ID=root;Password=;SslMode=none;";

        public static string LoggedInUsername { get; private set; }

        public static void Login(string username)
        {
            LoggedInUsername = username;
        }

        public static void Logout()
        {
            LoggedInUsername = null;
        }

        public static bool IsUserLoggedIn => !string.IsNullOrEmpty(LoggedInUsername);

        public static int? GetLoggedInUserId()
        {
            if (!IsUserLoggedIn)
            {
                return null;
            }

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "SELECT kullanici_id FROM Kullanicilar WHERE kullanici_adi = @username LIMIT 1";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", LoggedInUsername);

                object result = command.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int userId))
                {
                    return userId;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return null;
            }
        }*/



    }
}
