using System.Net.Http.Json;
using CryptoDataWatcher.Models;

namespace CryptoDataWatcher.Services
{
    public class BinanceApiService
    {
        private readonly HttpClient _http = new();

        // Modelo para deserializar la respuesta de Binance
        private class BinanceTicker
        {
            public string symbol { get; set; } = string.Empty;
            public string price { get; set; } = "0";
        }

        public async Task<List<CryptoPrice>> GetPricesAsync(List<string> pairs)
        {
            var response = await _http.GetFromJsonAsync<List<BinanceTicker>>("https://api.binance.com/api/v3/ticker/price");
            var list = new List<CryptoPrice>();

            if (response == null) return list;

            foreach (var item in response)
            {
                if (!pairs.Contains(item.symbol)) continue;

                if (!decimal.TryParse(item.price, out decimal price)) continue;

                list.Add(new CryptoPrice
                {
                    Symbol = item.symbol,
                    Price = price,
                    Timestamp = DateTime.UtcNow
                });
            }

            return list;
        }
    }
}
