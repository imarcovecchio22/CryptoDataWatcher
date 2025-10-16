using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CryptoDataWatcher.Services
{
    // Logger que escribe en archivo
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private static readonly ConcurrentQueue<string> _logQueue = new();

        public FileLogger(string filePath)
        {
            _filePath = filePath;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
            _logQueue.Enqueue(message);

            try
            {
                System.IO.File.AppendAllLines(_filePath, _logQueue);
                _logQueue.Clear();
            }
            catch
            {
                // Ignorar errores de escritura
            }
        }
    }

    // Proveedor de logger
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(_filePath);

        public void Dispose() { }
    }
}
