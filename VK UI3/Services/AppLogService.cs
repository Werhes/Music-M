using System;
using System.Collections.Generic;
using System.Text;

namespace VK_UI3.Services
{
    public class AppLogService
    {
        private static readonly Lazy<AppLogService> _instance = new(() => new AppLogService());
        public static AppLogService Instance => _instance.Value;

        private readonly List<string> _logs = new();
        private readonly object _lock = new();

        public event Action<string> LogAdded;

        public IReadOnlyList<string> Logs
        {
            get
            {
                lock (_lock)
                {
                    return _logs.ToArray();
                }
            }
        }

        public void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}";

            lock (_lock)
            {
                _logs.Add(logEntry);
                if (_logs.Count > 1000)
                    _logs.RemoveAt(0);
            }

            LogAdded?.Invoke(logEntry);
        }

        public void Log(string category, string message)
        {
            Log($"[{category}] {message}");
        }

        public void Log(Exception ex, string context = null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(context))
                sb.AppendLine($"[{context}]");

            sb.AppendLine($"Исключение: {ex.GetType().FullName}");
            sb.AppendLine($"Сообщение: {ex.Message}");

            if (ex.InnerException != null)
            {
                sb.AppendLine($"Внутреннее: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
            }

            sb.AppendLine($"Stack Trace: {ex.StackTrace}");

            Log(sb.ToString());
        }

        public string GetAllLogs()
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Логи приложения VK UI3 ===");
                sb.AppendLine($"Сгенерировано: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"ОС: {Environment.OSVersion}");
                sb.AppendLine();
                foreach (var log in _logs)
                {
                    sb.AppendLine(log);
                }
                return sb.ToString();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }
    }
}