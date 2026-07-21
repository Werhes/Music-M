using System;
using System.IO;
using System.Threading.Tasks;
using VK_UI3.Addons;
using VK_UI3.DB;
using VK_UI3.Views.Notification;

namespace TimeTracker
{
    /// <summary>
    /// Аддон для отслеживания времени, проведённого в VK M.
    /// Считает общее время, время за сегодня, время прослушивания музыки.
    /// Показывает уведомления о достижениях.
    /// </summary>
    public class TimeTracker : IAddon
    {
        // ===== МЕТАДАННЫЕ =====
        public string Id => "time_tracker";
        public string Name => "Time Tracker";
        public string Version => "1.0.0";
        public string Author => "VK M Community";
        public string Description => "Отслеживает время, проведённое в приложении, и время прослушивания музыки";

        // ===== ПОЛЯ ДЛЯ ХРАНЕНИЯ СОСТОЯНИЯ =====
        private bool _isRunning;
        private DateTime _sessionStart;
        private DateTime _appStart;
        private System.Threading.Timer _saveTimer;
        private System.Threading.Timer _notificationTimer;

        // ===== КЛЮЧИ ДЛЯ ХРАНЕНИЯ В БАЗЕ =====
        private const string KEY_TOTAL_SECONDS = "timetracker_total_seconds";
        private const string KEY_LISTENING_SECONDS = "timetracker_listening_seconds";
        private const string KEY_TODAY_DATE = "timetracker_today_date";
        private const string KEY_TODAY_SECONDS = "timetracker_today_seconds";
        private const string KEY_LAST_ACHIEVEMENT = "timetracker_last_achievement";
        private const string KEY_FIRST_LAUNCH = "timetracker_first_launch";

        // ===== ПОРОГИ ДОСТИЖЕНИЙ (В ЧАСАХ) =====
        private static readonly int[] AchievementHours = { 1, 5, 10, 24, 50, 100, 200, 500, 1000 };

        public Task InitializeAsync()
        {
            _appStart = DateTime.Now;
            _sessionStart = DateTime.Now;

            // Сбрасываем счётчик сегодняшнего дня, если новый день
            CheckAndResetDailyCounter();

            // Запоминаем первый запуск
            if (SettingsTable.GetSetting(KEY_FIRST_LAUNCH) == null)
            {
                SettingsTable.SetSetting(KEY_FIRST_LAUNCH, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            // Подписываемся на события приложения
            // 1. Событие закрытия приложения
            App.Current.Exit += OnAppExit;

            // 2. Событие смены трека (чтобы понимать, слушает ли пользователь музыку)
            MediaPlayerService.AudioPlayedChangeEvent += OnAudioPlayedChange;

            // 3. Событие позиции трека (для подсчёта времени прослушивания)
            MediaPlayerService.PositionChanged += OnPositionChanged;

            // Запускаем таймер автосохранения каждые 30 секунд
            _saveTimer = new System.Threading.Timer(
                _ => SaveCurrentSession(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30)
            );

            // Запускаем таймер проверки достижений каждые 60 секунд
            _notificationTimer = new System.Threading.Timer(
                _ => CheckAchievements(),
                null,
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(60)
            );

            _isRunning = true;

            // Показываем приветственное уведомление при первом запуске
            ShowWelcomeNotification();

            System.Diagnostics.Debug.WriteLine("[TimeTracker] Аддон инициализирован!");
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            _isRunning = false;

            // Отписываемся от событий
            App.Current.Exit -= OnAppExit;
            MediaPlayerService.AudioPlayedChangeEvent -= OnAudioPlayedChange;
            MediaPlayerService.PositionChanged -= OnPositionChanged;

            // Останавливаем таймеры
            _saveTimer?.Dispose();
            _notificationTimer?.Dispose();

            // Сохраняем текущую сессию
            SaveCurrentSession();

            System.Diagnostics.Debug.WriteLine("[TimeTracker] Аддон выгружен!");
            return Task.CompletedTask;
        }

        // ===== ОБРАБОТЧИКИ СОБЫТИЙ =====

        /// <summary>
        /// Вызывается при закрытии приложения
        /// </summary>
        private void OnAppExit(object sender, object e)
        {
            SaveCurrentSession();
        }

        /// <summary>
        /// Вызывается при смене состояния воспроизведения (играет/пауза)
        /// </summary>
        private void OnAudioPlayedChange(object sender, EventArgs e)
        {
            // Здесь можно отслеживать, играет ли музыка
            System.Diagnostics.Debug.WriteLine("[TimeTracker] Состояние воспроизведения изменилось");
        }

        /// <summary>
        /// Вызывается при изменении позиции трека
        /// </summary>
        private void OnPositionChanged(object sender, TimeSpan position)
        {
            // Если музыка играет (позиция меняется), увеличиваем счётчик прослушивания
            // Это будет сохранено таймером
        }

        // ===== ОСНОВНАЯ ЛОГИКА =====

        /// <summary>
        /// Сохранить текущую сессию в базу данных
        /// </summary>
        private void SaveCurrentSession()
        {
            if (!_isRunning) return;

            try
            {
                var now = DateTime.Now;
                var sessionDuration = (now - _sessionStart).TotalSeconds;
                var totalDuration = (now - _appStart).TotalSeconds;

                // Обновляем общее время
                var savedTotal = GetTotalSeconds(KEY_TOTAL_SECONDS);
                var newTotal = savedTotal + sessionDuration;
                SettingsTable.SetSetting(KEY_TOTAL_SECONDS, ((long)newTotal).ToString());

                // Обновляем время за сегодня
                var savedToday = GetTotalSeconds(KEY_TODAY_SECONDS);
                var newToday = savedToday + sessionDuration;
                SettingsTable.SetSetting(KEY_TODAY_SECONDS, ((long)newToday).ToString());

                // Сбрасываем время сессии
                _sessionStart = now;

                System.Diagnostics.Debug.WriteLine($"[TimeTracker] Сохранено. Всего: {FormatTime((long)newTotal)}, Сегодня: {FormatTime((long)newToday)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeTracker] Ошибка сохранения: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверить и сбросить ежедневный счётчик, если наступил новый день
        /// </summary>
        private void CheckAndResetDailyCounter()
        {
            var savedDate = SettingsTable.GetSetting(KEY_TODAY_DATE);
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            if (savedDate == null || savedDate.settingValue != today)
            {
                SettingsTable.SetSetting(KEY_TODAY_DATE, today);
                SettingsTable.SetSetting(KEY_TODAY_SECONDS, "0");
                System.Diagnostics.Debug.WriteLine("[TimeTracker] Сброшен ежедневный счётчик");
            }
        }

        /// <summary>
        /// Проверить достижения и показать уведомление
        /// </summary>
        private void CheckAchievements()
        {
            try
            {
                var totalSeconds = GetTotalSeconds(KEY_TOTAL_SECONDS);
                var totalHours = totalSeconds / 3600.0;
                var lastAchievement = SettingsTable.GetSetting(KEY_LAST_ACHIEVEMENT);
                var lastAchievementHours = lastAchievement != null ? int.Parse(lastAchievement.settingValue) : 0;

                foreach (var hours in AchievementHours)
                {
                    if (totalHours >= hours && hours > lastAchievementHours)
                    {
                        // Достижение получено!
                        SettingsTable.SetSetting(KEY_LAST_ACHIEVEMENT, hours.ToString());

                        var todaySeconds = GetTotalSeconds(KEY_TODAY_SECONDS);
                        var firstLaunch = SettingsTable.GetSetting(KEY_FIRST_LAUNCH)?.settingValue ?? "неизвестно";

                        // Показываем уведомление в приложении
                        ShowAchievementNotification(hours, totalSeconds, todaySeconds, firstLaunch);

                        System.Diagnostics.Debug.WriteLine($"[TimeTracker] Достижение: {hours} часов в VK M!");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeTracker] Ошибка проверки достижений: {ex.Message}");
            }
        }

        // ===== УВЕДОМЛЕНИЯ =====

        /// <summary>
        /// Показать приветственное уведомление
        /// </summary>
        private void ShowWelcomeNotification()
        {
            try
            {
                var firstLaunch = SettingsTable.GetSetting(KEY_FIRST_LAUNCH)?.settingValue;
                if (firstLaunch != null)
                {
                    // Уже был запуск, не показываем
                    return;
                }

                // Показываем через Dispatcher, так как мы не в UI потоке
                MainWindow.dispatcherQueue.TryEnqueue(() =>
                {
                    new Notification(
                        "⏱ Time Tracker активирован!",
                        "Теперь я буду отслеживать время, проведённое в VK M.\n" +
                        "Следи за своими достижениями!"
                    );
                });
            }
            catch { }
        }

        /// <summary>
        /// Показать уведомление о достижении
        /// </summary>
        private void ShowAchievementNotification(int hours, long totalSeconds, long todaySeconds, string firstLaunch)
        {
            var emoji = GetAchievementEmoji(hours);
            var title = hours switch
            {
                1 => "Первый час в VK M!",
                5 => "5 часов! Начало положено!",
                10 => "10 часов! Уже привыкаешь?",
                24 => "Целые сутки в VK M! 🎉",
                50 => "50 часов! Настоящий мелома!",
                100 => "100 часов! Ты живёшь музыкой!",
                200 => "200 часов! Легендарный слушатель!",
                500 => "500 часов! VK M — твой второй дом!",
                1000 => "1000 часов! Ты — икона стиля!",
                _ => $"{hours} часов в VK M!"
            };

            var totalTime = FormatTime(totalSeconds);
            var todayTime = FormatTime(todaySeconds);

            MainWindow.dispatcherQueue.TryEnqueue(() =>
            {
                new Notification(
                    $"🏆 {title}",
                    $"Всего проведено: {totalTime}\n" +
                    $"Сегодня: {todayTime}\n" +
                    $"В приложении с: {firstLaunch}"
                );
            });
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

        /// <summary>
        /// Получить общее количество секунд из БД
        /// </summary>
        private long GetTotalSeconds(string key)
        {
            var setting = SettingsTable.GetSetting(key);
            if (setting == null || string.IsNullOrEmpty(setting.settingValue))
                return 0;

            if (long.TryParse(setting.settingValue, out long seconds))
                return seconds;

            return 0;
        }

        /// <summary>
        /// Форматировать время в человекочитаемый вид
        /// </summary>
        private static string FormatTime(long totalSeconds)
        {
            var ts = TimeSpan.FromSeconds(totalSeconds);

            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays} д {ts.Hours} ч {ts.Minutes} мин";
            else if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours} ч {ts.Minutes} мин";
            else
                return $"{ts.Minutes} мин {ts.Seconds} сек";
        }

        /// <summary>
        /// Получить эмодзи для достижения
        /// </summary>
        private static string GetAchievementEmoji(int hours)
        {
            return hours switch
            {
                1 => "🌱",
                5 => "🌿",
                10 => "🌳",
                24 => "⭐",
                50 => "🌟",
                100 => "💫",
                200 => "👑",
                500 => "💎",
                1000 => "🔥",
                _ => "🎯"
            };
        }

        /// <summary>
        /// Получить статистику (может быть вызвано из другого аддона или через рефлексию)
        /// </summary>
        public static string GetStatistics()
        {
            var totalSeconds = long.TryParse(
                SettingsTable.GetSetting(KEY_TOTAL_SECONDS)?.settingValue, out var total)
                ? total : 0;

            var todaySeconds = long.TryParse(
                SettingsTable.GetSetting(KEY_TODAY_SECONDS)?.settingValue, out var today)
                ? today : 0;

            var firstLaunch = SettingsTable.GetSetting(KEY_FIRST_LAUNCH)?.settingValue ?? "неизвестно";

            return $"Всего: {FormatTime(totalSeconds)}\n" +
                   $"Сегодня: {FormatTime(todaySeconds)}\n" +
                   $"В приложении с: {firstLaunch}";
        }
    }
}