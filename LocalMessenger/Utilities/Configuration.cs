using System;
using System.IO;

namespace LocalMessenger.Utilities
{
    public static class Configuration
    {
        public static string AppDataPath { get; }
        public static string AttachmentsPath { get; }
        public static string HistoryPath { get; }
        public static string SettingsFile { get; }

        static Configuration()
        {
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger");
            AttachmentsPath = Path.Combine(AppDataPath, "attachments");
            HistoryPath = Path.Combine(AppDataPath, "history");
            SettingsFile = Path.Combine(AppDataPath, "settings.json");

            Directory.CreateDirectory(AppDataPath);
            Directory.CreateDirectory(AttachmentsPath);
            Directory.CreateDirectory(HistoryPath);
        }
    }

    #region MainForm

    //private void InitializePaths()
    //{
    //    AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger");
    //    AttachmentsPath = Path.Combine(AppDataPath, "attachments");
    //    HistoryPath = Path.Combine(AppDataPath, "history");
    //    SettingsFile = Path.Combine(AppDataPath, "settings.json");
    //    Logger.Log($"Paths initialized: AppData={AppDataPath}, Settings={SettingsFile}");
    //}

    //private void InitializeDirectories()
    //{
    //    Directory.CreateDirectory(AppDataPath);
    //    Directory.CreateDirectory(AttachmentsPath);
    //    Directory.CreateDirectory(HistoryPath);
    //    Logger.Log("Directories created or verified");
    //}

    #endregion

}