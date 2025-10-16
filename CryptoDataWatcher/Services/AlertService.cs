namespace CryptoDataWatcher.Services
{
    public class AlertService
    {
        public void Notify(string symbol, decimal oldPrice, decimal newPrice)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[ALERTA] {symbol} cambió de {oldPrice} → {newPrice} ({Math.Round((newPrice - oldPrice) / oldPrice * 100, 2)}%)");
            Console.ResetColor();
        }
    }
}
