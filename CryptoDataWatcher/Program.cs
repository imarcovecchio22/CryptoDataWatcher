using System.Text.Json;
using CryptoDataWatcher.Models;
using CryptoDataWatcher.Services;
using Microsoft.Extensions.Logging;

var logDir = Path.Combine("logs");
Directory.CreateDirectory(logDir);

// Limpiar logs antiguos (mantener solo últimos 7 días)
void CleanOldLogs(string logDirectory, int daysToKeep = 7)
{
    if (!Directory.Exists(logDirectory)) return;

    var files = Directory.GetFiles(logDirectory, "crypto_watcher_*.txt");
    foreach (var file in files)
    {
        try
        {
            var creationDate = File.GetCreationTime(file);
            if ((DateTime.Now - creationDate).TotalDays > daysToKeep)
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Ignorar errores de borrado
        }
    }
}

CleanOldLogs(logDir, 7);

var logFilePath = Path.Combine(logDir, $"crypto_watcher_{DateTime.Today:yyyyMMdd}.txt");
var alertsFilePath = Path.Combine(logDir, "alerts.txt");

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .AddProvider(new FileLoggerProvider(logFilePath))
        .SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("CryptoDataWatcher");

try
{
    // Cargar configuración
    var settingsPath = Path.Combine("Config", "settings.json");
    using var fs = File.OpenRead(settingsPath);
    var config = await JsonSerializer.DeserializeAsync<JsonElement>(fs);

    if (!config.TryGetProperty("pairs", out var pairsJson) ||
        !config.TryGetProperty("intervalSeconds", out var intervalJson) ||
        !config.TryGetProperty("alertThreshold", out var thresholdJson))
    {
        logger.LogCritical("settings.json no contiene las claves necesarias.");
        return;
    }

    var pairs = pairsJson.EnumerateArray()
        .Select(p => p.GetString())
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => p!) // List<string>
        .ToList();

    int intervalSeconds = intervalJson.GetInt32();
    decimal threshold = thresholdJson.GetDecimal();

    if (pairs.Count == 0)
    {
        logger.LogCritical("No se encontraron pares válidos en settings.json");
        return;
    }

    var binance = new BinanceApiService();
    var storage = new DataStorageService();
    var alerts = new AlertService();

    logger.LogInformation("=== Crypto Data Watcher iniciado ===");
    logger.LogInformation("Monitoreando: {Pairs} cada {Interval}s (Umbral de alerta: {Threshold:P})",
        string.Join(", ", pairs), intervalSeconds, threshold);

    while (true)
    {
        try
        {
            var prices = await binance.GetPricesAsync(pairs);

            foreach (var p in prices)
            {
                if (string.IsNullOrWhiteSpace(p.Symbol)) continue;

                var last = storage.GetLastPrice(p.Symbol);
                storage.SavePrice(p);

                // Mostrar precio actual
                logger.LogInformation("{Symbol}: {Price}", p.Symbol, p.Price);

                if (last != null)
                {
                    decimal change = Math.Abs(p.Price - last.Price) / last.Price;
                    if (change >= threshold)
                    {
                        alerts.Notify(p.Symbol, last.Price, p.Price);
                        logger.LogWarning("Cambio detectado en {Symbol}: {OldPrice} → {NewPrice} ({Change:P})",
                            p.Symbol, last.Price, p.Price, change);

                        // Guardar alerta en archivo
                        File.AppendAllText(alertsFilePath,
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {p.Symbol}: {last.Price} → {p.Price} ({change:P}){Environment.NewLine}");
                    }
                }
            }

            logger.LogInformation("Iteración completada correctamente a las {Time}", DateTime.Now);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la ejecución del ciclo principal");
        }

        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Error crítico al iniciar el programa");
}
