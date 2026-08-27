using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using VK_UI3.DB;

namespace VK_UI3.Services
{
    /// <summary>
    /// Сервис "Режим мессенджера (Music M)".
    /// Запускает собранный отдельный мессенджер (Laney-Avalonia) и сразу
    /// передаёт ему токен текущего аккаунта Music M через аргументы командной строки,
    /// чтобы он открылся уже авторизованным.
    ///
    /// Мессенджер хранит данные в отдельной локальной папке (через аргумент -ldp=),
    /// поэтому не конфликтует с обычной установкой Laney и не затирает чужие аккаунты.
    /// </summary>
    public static class MessengerModeService
    {
        /// <summary>Ключ настройки с путём к исполняемому файлу мессенджера.</summary>
        public const string SettingKeyExePath = "messengerExePath";

        /// <summary>Имя исполняемого файла мессенджера после сборки.</summary>
        public const string ExeFileName = "laney.exe";

        /// <summary>Имя исполняемого файла при переименовании сборки мессенджера.</summary>
        public const string RebrandedExeFileName = "MusicMMessenger.exe";

        /// <summary>Локальная папка, где мессенджер хранит токен, настройки и кэш.</summary>
        public static string GetMessengerDataDir()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "MusicM", "Messenger");
        }

        /// <summary>Пытается найти собранный exe мессенджера (настроенный или стандартные пути).</summary>
        public static string FindMessengerExe()
        {
            // 1) Пользовательский путь из настроек.
            var configured = SettingsTable.GetSetting(SettingKeyExePath);
            if (configured != null && !string.IsNullOrEmpty(configured.settingValue))
            {
                try { if (File.Exists(configured.settingValue)) return configured.settingValue; } catch { }
            }

            // 2) Стандартные пути относительно каталога сборки Music-M.
            string baseDir = AppContext.BaseDirectory;
            string[] candidates =
            {
                // В подпапке Messenger рядом с приложением (так он кладётся в релиз, чтобы не смешивать DLL).
                Path.Combine(baseDir, "Messenger", RebrandedExeFileName),
                Path.Combine(baseDir, "Messenger", ExeFileName),
                // Рядом с самим приложением.
                Path.Combine(baseDir, RebrandedExeFileName),
                Path.Combine(baseDir, ExeFileName),
                Path.Combine(baseDir, @"..\..\..\..\Messenger\L2\bin\Release\net10.0", ExeFileName),
                Path.Combine(baseDir, @"..\..\..\..\Messenger\L2\bin\Debug\net10.0", ExeFileName),
                Path.Combine(GetMessengerDataDir(), RebrandedExeFileName),
                Path.Combine(GetMessengerDataDir(), ExeFileName)
            };

            foreach (string c in candidates)
            {
                try { if (File.Exists(c)) return c; } catch { }
            }

            return null;
        }

        /// <summary>
        /// Запускает мессенджер, передав токен и id текущего аккаунта.
        /// Возвращает строку статуса для отображения в интерфейсе.
        /// </summary>
        public static async Task<string> LaunchMessengerAsync()
        {
            var acc = AccountsDB.activeAccount;
            if (acc == null || string.IsNullOrEmpty(acc.Token))
                return "Аккаунт не авторизован или у него нет токена. Сначала войдите в Music M.";

            string exe = FindMessengerExe();
            if (exe == null)
            {
                return "Не найден исполняемый файл мессенджера. Соберите проект Messenger/L2 " +
                       "(pwsh Messenger/L2/build_aot.ps1) либо укажите путь к exe в настройках.";
            }

            string dataDir = GetMessengerDataDir();
            try { Directory.CreateDirectory(dataDir); } catch { }

            // Для аргумента -ldp= Laney требует завершающий разделитель каталога.
            if (!dataDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                dataDir += Path.DirectorySeparatorChar;

            // Путь к самому Music M — мессенджер использует его для кнопки «Вернуться в Music-M».
            string returnExe = "";
            try { returnExe = Environment.ProcessPath ?? ""; } catch { }

            string args = $"-ldp=\"{dataDir}\" -token={acc.Token} -userid={acc.id} -returnexe=\"{returnExe}\"";
            try
            {
                await Task.Yield();
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = true
                });
                return $"Мессенджер «Music M (messenger-mode)» запущен (аккаунт #{acc.id}).";
            }
            catch (Exception ex)
            {
                return "Ошибка запуска мессенджера: " + ex.Message;
            }
        }
    }
}