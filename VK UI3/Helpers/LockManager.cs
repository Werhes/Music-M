using System;
using System.Security.Cryptography;
using System.Text;
using VK_UI3.DB;

namespace VK_UI3.Helpers
{
    /// <summary>
    /// Управляет защитой приложения кодом-паролем.
    /// По умолчанию код выключен. Хранится в виде SHA-256 хэша.
    /// </summary>
    public static class LockManager
    {
        private const string PinKey = "lockPin";

        /// <summary>
        /// Признак того, что приложение сейчас заблокировано в текущей сессии.
        /// </summary>
        public static bool IsLocked { get; set; }

        /// <summary>
        /// Установлен ли код-пароль.
        /// </summary>
        public static bool IsPinEnabled()
        {
            try
            {
                var s = SettingsTable.GetSetting(PinKey);
                return s != null && !string.IsNullOrEmpty(s.settingValue);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Устанавливает (или заменяет) код-пароль.
        /// </summary>
        public static void SetPin(string pin)
        {
            SettingsTable.SetSetting(PinKey, Hash(pin));
        }

        /// <summary>
        /// Проверяет введённый код. Если код не установлен — возвращает true.
        /// </summary>
        public static bool VerifyPin(string pin)
        {
            if (!IsPinEnabled())
                return true;

            var s = SettingsTable.GetSetting(PinKey);
            if (s == null || string.IsNullOrEmpty(s.settingValue))
                return false;

            return Hash(pin) == s.settingValue;
        }

        /// <summary>
        /// Удаляет код-пароль (защита выключается).
        /// </summary>
        public static void ClearPin()
        {
            SettingsTable.SetSetting(PinKey, string.Empty);
            IsLocked = false;
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            return Convert.ToHexString(bytes);
        }
    }
}