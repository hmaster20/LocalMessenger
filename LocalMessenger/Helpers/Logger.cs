using System;
using System.IO;
using System.Text;

namespace LocalMessenger
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LocalMessenger", "logs");
        private static readonly string LogFile = Path.Combine(LogDirectory, "log.txt");
        private static readonly string HeartbeatLogFile = Path.Combine(LogDirectory, "heartbeat.log");
        private const long MaxLogSizeBytes = 10 * 1024 * 1024; // 10 MB

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                RotateLogIfNeeded(LogFile);
                RotateLogIfNeeded(HeartbeatLogFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Log(string message, bool isHeartbeat = false)
        {
            try
            {
                DateTime utcTime = DateTime.UtcNow;
                TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                DateTime moscowTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, moscowTimeZone);
                var logEntry = $"{moscowTime:yyyy-MM-dd HH:mm:ss.fff} | {message}\n";

                File.AppendAllText(isHeartbeat ? HeartbeatLogFile : LogFile, logEntry, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Игнорируем ошибки логирования, чтобы не прерывать приложение
            }
        }

        private static void RotateLogIfNeeded(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > MaxLogSizeBytes)
                    {
                        string archiveFile = Path.Combine(LogDirectory,
                            $"log_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(filePath)}");
                        File.Move(filePath, archiveFile);
                    }
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки ротации
            }
        }
    }
}