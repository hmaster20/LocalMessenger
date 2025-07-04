﻿using System;
using System.IO;
using System.Text;

namespace LocalMessenger.Utilities
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LocalMessenger", "logs");
        private static readonly string LogFile = Path.Combine(LogDirectory, "log.txt");
        private const long MaxLogSizeBytes = 10 * 1024 * 1024; // 10 MB

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                RotateLogIfNeeded();
            }
            catch (Exception ex)
            {
                // Не прерываем работу приложения, если не удалось создать папку логов
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            try
            {
                RotateLogIfNeeded();
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}\n";
                File.AppendAllText(LogFile, logEntry, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Игнорируем ошибки логирования, чтобы не прерывать приложение
            }
        }

        private static void RotateLogIfNeeded()
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    var fileInfo = new FileInfo(LogFile);
                    if (fileInfo.Length > MaxLogSizeBytes)
                    {
                        string archiveFile = Path.Combine(LogDirectory,
                            $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        File.Move(LogFile, archiveFile);
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