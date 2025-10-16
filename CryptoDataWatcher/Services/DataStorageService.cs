using System.Data.SQLite;
using CryptoDataWatcher.Models;

namespace CryptoDataWatcher.Services
{
    public class DataStorageService
    {
        private const string DbFile = "crypto.db";

        public DataStorageService()
        {
            if (!File.Exists(DbFile))
                SQLiteConnection.CreateFile(DbFile);

            using var conn = new SQLiteConnection($"Data Source={DbFile};Version=3;");
            conn.Open();

            string createTable = @"
                CREATE TABLE IF NOT EXISTS Prices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Symbol TEXT,
                    Price REAL,
                    Timestamp TEXT
                );";
            using var cmd = new SQLiteCommand(createTable, conn);
            cmd.ExecuteNonQuery();
        }

        public void SavePrice(CryptoPrice price)
        {
            using var conn = new SQLiteConnection($"Data Source={DbFile};Version=3;");
            conn.Open();

            var cmd = new SQLiteCommand("INSERT INTO Prices (Symbol, Price, Timestamp) VALUES (@s, @p, @t)", conn);
            cmd.Parameters.AddWithValue("@s", price.Symbol);
            cmd.Parameters.AddWithValue("@p", price.Price);
            cmd.Parameters.AddWithValue("@t", price.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public CryptoPrice? GetLastPrice(string symbol)
        {
            using var conn = new SQLiteConnection($"Data Source={DbFile};Version=3;");
            conn.Open();

            var cmd = new SQLiteCommand("SELECT Price, Timestamp FROM Prices WHERE Symbol = @s ORDER BY Id DESC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@s", symbol);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new CryptoPrice
                {
                    Symbol = symbol,
                    Price = reader.GetDecimal(0),
                    Timestamp = DateTime.Parse(reader.GetString(1))
                };
            }
            return null;
        }
    }
}
