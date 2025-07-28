using Microsoft.VisualStudio.TestTools.UnitTesting;
using LocalMessenger;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;

namespace LocalMessenger.Tests
{
    [TestClass]
    public class KeyManagerTests
    {
        private static readonly string KeyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LocalMessenger", "key.bin");
        private const string TestPassword = "test123";

        [TestInitialize]
        public void Setup()
        {
            // Удаляем тестовый файл перед каждым тестом
            if (File.Exists(KeyFilePath))
            {
                File.Delete(KeyFilePath);
            }
            // Очищаем логгер перед тестом, если он статический
            ClearLogger();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Удаляем тестовый файл после каждого теста
            if (File.Exists(KeyFilePath))
            {
                File.Delete(KeyFilePath);
            }
        }

        [TestMethod]
        public void GenerateAndSaveKey_WithValidPassword_ShouldSaveKey()
        {
            // Arrange
            string password = TestPassword;

            // Act
            KeyManager.GenerateAndSaveKey(password);

            // Assert
            Assert.IsTrue(File.Exists(KeyFilePath), "Key file should be created.");
        }

        [TestMethod]
        public void GenerateAndLoadKey_ShouldReturnSameKey()
        {
            // Arrange
            string password = TestPassword;

            // Act
            var key = KeyManager.GenerateAndSaveKey(password);
            var loadedKey = KeyManager.LoadKey(password);

            // Assert
            CollectionAssert.AreEqual(key, loadedKey, "Loaded key should match the generated key.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GenerateAndSaveKey_WithNullPassword_ShouldThrowArgumentNullException()
        {
            // Act
            KeyManager.GenerateAndSaveKey(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenerateAndSaveKey_WithEmptyPassword_ShouldThrowArgumentException()
        {
            // Act
            KeyManager.GenerateAndSaveKey("");
        }

        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void LoadKey_WithWrongPassword_ShouldThrowCryptographicException()
        {
            // Arrange
            string correctPassword = TestPassword;
            string wrongPassword = "wrong123";
            KeyManager.GenerateAndSaveKey(correctPassword);

            // Act
            KeyManager.LoadKey(wrongPassword);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void LoadKey_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Act
            KeyManager.LoadKey(TestPassword);
        }

        [TestMethod]
        public void GenerateAndSaveKey_ShouldLogSuccess()
        {
            // Arrange
            string password = TestPassword;
            string logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LocalMessenger", "logs", "log.txt");
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            // Act
            KeyManager.GenerateAndSaveKey(password);

            // Assert
            string logContent = File.Exists(logFilePath) ? File.ReadAllText(logFilePath) : "";
            Assert.IsTrue(logContent.Contains("Encryption key generated and saved"),
                "Log should contain success message for key generation.");
        }

        [TestMethod]
        public void LoadKey_WithValidPassword_ShouldLogSuccess()
        {
            // Arrange
            string password = TestPassword;
            string logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LocalMessenger", "logs", "log.txt");
            KeyManager.GenerateAndSaveKey(password);
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            // Act
            KeyManager.LoadKey(password);

            // Assert
            string logContent = File.Exists(logFilePath) ? File.ReadAllText(logFilePath) : "";
            Assert.IsTrue(logContent.Contains("Encryption key loaded successfully"),
                "Log should contain success message for key loading.");
        }

        [TestMethod]
        public void GenerateAndSaveKey_ShouldCreateValidEncryptedFile()
        {
            // Arrange
            string password = TestPassword;

            // Act
            KeyManager.GenerateAndSaveKey(password);

            // Assert
            byte[] fileData = File.ReadAllBytes(KeyFilePath);
            Assert.IsTrue(fileData.Length >= 16 + 32, "File should contain IV (16 bytes) and encrypted key (at least 32 bytes).");
        }

        private void ClearLogger()
        {
            // Очистка логгера, если он статический
            // Предполагаем, что Logger имеет статическое поле или метод для очистки
            // Если Logger не предоставляет такого метода, пропустите или замените на реальную реализацию
            try
            {
                var loggerType = Type.GetType("LocalMessenger.Logger, LocalMessenger");
                if (loggerType != null)
                {
                    // Если есть метод очистки или статическое поле, очистите его
                    // Здесь пример, если Logger имеет метод Clear или поле для логов
                    // loggerType.GetMethod("Clear")?.Invoke(null, null);
                }
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки очистки логгера
                Console.WriteLine($"Failed to clear logger: {ex.Message}");
            }
        }
    }
}