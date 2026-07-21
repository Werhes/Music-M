# Music-M Addons — Полное руководство

Это руководство описывает, как создавать аддоны (расширения) и темы для приложения **VK M**, а также как настроить репозиторий [`Music-M_Addons`](https://github.com/Werhes/Music-M_Addons).

---

## 📁 Структура репозитория

Репозиторий [`Music-M_Addons`](https://github.com/Werhes/Music-M_Addons) должен иметь следующую структуру:

```
Music-M_Addons/
├── Addons/              # Папка с аддонами
│   ├── MyAddon/         # Папка конкретного аддона
│   │   ├── addon.json   # Манифест аддона (обязательно)
│   │   ├── icon.png     # Иконка 64x64 (рекомендуется)
│   │   ├── README.md    # Описание (отображается в приложении)
│   │   └── MyAddon.dll  # DLL с кодом расширения (обязательно)
│   │
│   └── AnotherAddon/
│       ├── addon.json
│       ├── icon.png
│       ├── README.md
│       └── AnotherAddon.dll
│
└── Themes/              # Папка с темами
    ├── MyTheme/         # Папка конкретной темы
    │   ├── theme.json   # Манифест темы (обязательно)
    │   ├── icon.png     # Иконка 64x64 (рекомендуется)
    │   ├── README.md    # Описание (отображается в приложении)
    │   └── MyTheme.xaml # XAML ResourceDictionary (обязательно)
    │
    └── AnotherTheme/
        ├── theme.json
        ├── icon.png
        ├── README.md
        └── AnotherTheme.xaml
```

**Важно:** Название папки должно совпадать с названием аддона/темы и названием DLL/XAML файла.

---

## 🧩 Как создать аддон (расширение)

### Шаг 1: Создайте проект библиотеки классов

1. Откройте Visual Studio
2. Создайте новый проект → **Библиотека классов (Class Library)**
3. Назовите проект, например, `MyCoolAddon`
4. Выберите .NET 6.0 или .NET 8.0 (ту же версию, что и VK M)

### Шаг 2: Добавьте ссылку на VK UI3

1. В обозревателе решений ПКМ по проекту → **Add** → **Project Reference**
2. Нажмите **Browse** и укажите путь к `VK UI3.csproj`
3. Или добавьте ссылку на собранную DLL: `VK UI3\bin\Debug\net6.0-windows10.0.19041.0\VK UI3.dll`

### Шаг 3: Реализуйте интерфейс IAddon

Создайте класс, реализующий интерфейс `VK_UI3.Addons.IAddon`:

```csharp
using System;
using System.Threading.Tasks;
using VK_UI3.Addons;

namespace MyCoolAddon
{
    public class MyCoolAddon : IAddon
    {
        // --- Обязательные свойства ---

        public string Id => "my_cool_addon";
        // Уникальный ID латиницей, без пробелов

        public string Name => "My Cool Addon";
        // Отображаемое название

        public string Version => "1.0.0";
        // Версия в формате SemVer

        public string Author => "Your Name";
        // Ваше имя или ник

        public string Description => "Описание того, что делает этот аддон";
        // Краткое описание

        // --- Обязательные методы ---

        public Task InitializeAsync()
        {
            // Этот метод вызывается при загрузке аддона
            // Здесь вы можете подписаться на события приложения

            Console.WriteLine($"[{Name}] Инициализирован!");

            // Пример: подписка на смену трека
            // MusicX.Services.MediaPlayerService.OnTrackChanged += OnTrackChanged;

            // Пример: подписка на воспроизведение/паузу
            // MusicX.Services.MediaPlayerService.OnPlayPause += OnPlayPause;

            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            // Этот метод вызывается при выгрузке аддона
            // Здесь нужно отписаться от всех событий и освободить ресурсы

            Console.WriteLine($"[{Name}] Выгружен!");

            // Пример: отписка от событий
            // MusicX.Services.MediaPlayerService.OnTrackChanged -= OnTrackChanged;
            // MusicX.Services.MediaPlayerService.OnPlayPause -= OnPlayPause;

            return Task.CompletedTask;
        }

        // --- Пример обработчика события ---
        /*
        private void OnTrackChanged(object sender, EventArgs e)
        {
            // Ваш код при смене трека
            var currentTrack = MusicX.Services.MediaPlayerService.CurrentTrack;
            Console.WriteLine($"Сейчас играет: {currentTrack.Title}");
        }
        */
    }
}
```

### Шаг 4: Соберите проект

1. Нажмите **Build** → **Build Solution** (Ctrl+Shift+B)
2. Найдите собранную DLL в папке `bin\Debug\net6.0\MyCoolAddon.dll`

### Шаг 5: Создайте структуру в репозитории

Создайте папку `Addons/MyCoolAddon/` в репозитории [`Music-M_Addons`](https://github.com/Werhes/Music-M_Addons) и поместите туда файлы:

#### addon.json (манифест)

```json
{
  "id": "my_cool_addon",
  "name": "My Cool Addon",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Краткое описание вашего аддона",
  "minAppVersion": "1.0.0",
  "addonType": "other",
  "entryPoint": "MyCoolAddon.dll"
}
```

**Поля манифеста:**

| Поле | Тип | Обязательное | Описание |
|------|-----|:---:|----------|
| `id` | string | ✅ | Уникальный идентификатор (латиница, без пробелов) |
| `name` | string | ✅ | Отображаемое название |
| `version` | string | ✅ | Версия (SemVer: `1.0.0`) |
| `author` | string | ❌ | Автор |
| `description` | string | ❌ | Краткое описание |
| `minAppVersion` | string | ❌ | Минимальная версия VK M |
| `addonType` | string | ❌ | Тип аддона (см. таблицу ниже) |
| `entryPoint` | string | ✅ | Имя DLL файла |

**Типы аддонов (`addonType`):**

| Тип | Описание |
|-----|----------|
| `visualizer` | Визуализация аудио |
| `lyrics_provider` | Провайдер текстов песен |
| `notification` | Кастомные уведомления |
| `integration` | Интеграция с внешними сервисами |
| `ui` | Модификация интерфейса |
| `other` | Другое |

#### icon.png

- Рекомендуемый размер: **64×64 пикселя**
- Формат: PNG с прозрачностью
- Эта иконка будет отображаться в магазине дополнений

#### README.md

Этот файл будет отображаться в приложении как **"Об расширении"**. Напишите подробное описание:

```markdown
# My Cool Addon

**Версия:** 1.0.0
**Автор:** Your Name

## Описание

My Cool Addon добавляет новые возможности в VK M...

## Возможности

- Функция 1
- Функция 2
- Функция 3

## Использование

После установки аддон автоматически активируется...

## Совместимость

Требуется VK M версии 1.0.0 или выше.
```

#### MyCoolAddon.dll

Собранная DLL вашего проекта.

---

## 🎨 Как создать тему

### Шаг 1: Создайте XAML ResourceDictionary

Создайте файл `MyTheme.xaml`:

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ===== АКЦЕНТНЫЙ ЦВЕТ ===== -->
    <Color x:Key="SystemAccentColor">#FF0078D4</Color>
    <Color x:Key="SystemAccentColorDark1">#FF005A9E</Color>
    <Color x:Key="SystemAccentColorDark2">#FF003D6B</Color>
    <Color x:Key="SystemAccentColorDark3">#FF00203B</Color>
    <Color x:Key="SystemAccentColorLight1">#FF4BA0E0</Color>
    <Color x:Key="SystemAccentColorLight2">#FF8BC4F0</Color>
    <Color x:Key="SystemAccentColorLight3">#FFC7E4F8</Color>

    <!-- ===== ФОНОВЫЕ ЦВЕТА ===== -->
    <Color x:Key="ApplicationPageBackgroundThemeBrush">#FF1A1A2E</Color>
    <Color x:Key="CardBackgroundFillColorDefault">#FF252540</Color>
    <Color x:Key="CardBackgroundFillColorSecondary">#FF2D2D4A</Color>
    <Color x:Key="LayerFillColorAltBrush">#FF1E1E38</Color>
    <Color x:Key="LayerOnAcrylicFillColorDefaultBrush">#FF2A2A48</Color>

    <!-- ===== ТЕКСТОВЫЕ ЦВЕТА ===== -->
    <Color x:Key="TextFillColorPrimary">#FFE0E0F0</Color>
    <Color x:Key="TextFillColorSecondary">#FFB0B0C8</Color>
    <Color x:Key="TextFillColorTertiary">#FF8080A0</Color>
    <Color x:Key="TextFillColorDisabled">#FF505070</Color>

    <!-- ===== ЦВЕТА ЭЛЕМЕНТОВ УПРАВЛЕНИЯ ===== -->
    <Color x:Key="ControlFillColorDefault">#FF35355A</Color>
    <Color x:Key="ControlFillColorSecondary">#FF3D3D62</Color>
    <Color x:Key="ControlFillColorDisabled">#FF252540</Color>
    <Color x:Key="ControlFillColorHover">#FF404068</Color>
    <Color x:Key="ControlFillColorPressed">#FF303050</Color>

    <!-- ===== ЦВЕТА ГРАНИЦ ===== -->
    <Color x:Key="ControlElevationBorderBrush">#FF404068</Color>
    <Color x:Key="CardStrokeColorDefault">#FF3A3A5C</Color>

    <!-- ===== ЦВЕТА КНОПОК ===== -->
    <Color x:Key="ButtonBackground">#FF0078D4</Color>
    <Color x:Key="ButtonBackgroundHover">#FF1A8CE0</Color>
    <Color x:Key="ButtonBackgroundPressed">#FF0060B0</Color>
    <Color x:Key="ButtonForeground">#FFFFFFFF</Color>

</ResourceDictionary>
```

**Как найти нужные цвета:**

1. Откройте файл `C:\Program Files (x86)\Windows Kits\10\DesignTime\CommonConfiguration\Neutral\UWP\XAML\generic.xaml`
2. Ищите `Brush` или `Color` ресурсы, которые хотите переопределить
3. Скопируйте ключ ресурса и задайте своё значение

### Шаг 2: Создайте манифест theme.json

```json
{
  "id": "my_theme",
  "name": "My Theme",
  "author": "Your Name",
  "version": "1.0.0",
  "description": "Описание вашей темы",
  "themeFile": "MyTheme.xaml",
  "isDark": true
}
```

**Поля манифеста:**

| Поле | Тип | Обязательное | Описание |
|------|-----|:---:|----------|
| `id` | string | ✅ | Уникальный идентификатор |
| `name` | string | ✅ | Отображаемое название |
| `author` | string | ❌ | Автор |
| `version` | string | ❌ | Версия |
| `description` | string | ❌ | Краткое описание |
| `themeFile` | string | ✅ | Имя XAML файла |
| `isDark` | bool | ❌ | true — тёмная тема, false — светлая |

### Шаг 3: Добавьте иконку

- Файл: `icon.png`
- Размер: **64×64 пикселя**
- Будет отображаться в магазине

### Шаг 4: Добавьте README.md

Будет отображаться как **"Об теме"**:

```markdown
# My Theme

**Версия:** 1.0.0
**Автор:** Your Name

## Описание

Тёмная тема в сине-фиолетовых тонах...

## Цветовая палитра

- Фон: #1A1A2E (тёмно-синий)
- Карточки: #252540
- Акцент: #0078D4 (голубой)
- Текст: #E0E0F0

## Совместимость

Требуется VK M версии 1.0.0 или выше.
```

### Шаг 5: Создайте структуру в репозитории

Создайте папку `Themes/MyTheme/` и поместите туда:
- `theme.json`
- `icon.png`
- `README.md`
- `MyTheme.xaml`

---

## 🚀 Как опубликовать аддон или тему

### Способ 1: Через Pull Request

1. Форкните репозиторий [`Music-M_Addons`](https://github.com/Werhes/Music-M_Addons)
2. Создайте папку с вашим аддоном/темой в соответствующей директории
3. Добавьте все необходимые файлы
4. Создайте Pull Request в основной репозиторий

### Способ 2: Напрямую (если есть доступ)

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/Werhes/Music-M_Addons.git
   ```
2. Создайте структуру папок
3. Добавьте файлы
4. Закоммитьте и запушьте:
   ```bash
   git add .
   git commit -m "Добавлен аддон MyCoolAddon"
   git push
   ```

---

## 📦 Как установить аддон/тему в VK M

1. Откройте **VK M**
2. Нажмите на **Магазин дополнений** в боковом меню
3. Переключитесь между вкладками **Аддоны** / **Темы**
4. Нажмите **Установить** на нужном элементе
5. После установки:
   - **Аддоны** активируются автоматически
   - **Темы** можно применить сразу или позже через настройки

### Управление установленными дополнениями

1. Откройте **Настройки** → **Магазин дополнений**
2. Здесь отображаются все установленные аддоны и темы
3. Можно **применить** тему или **удалить** аддон/тему

---

## 💡 Примеры готовых аддонов

### Пример 1: Простой аддон-логгер

```csharp
using System;
using System.Threading.Tasks;
using VK_UI3.Addons;

namespace TrackLogger
{
    public class TrackLogger : IAddon
    {
        public string Id => "track_logger";
        public string Name => "Track Logger";
        public string Version => "1.0.0";
        public string Author => "Developer";
        public string Description => "Логирует все прослушанные треки в файл";

        public Task InitializeAsync()
        {
            MusicX.Services.MediaPlayerService.OnTrackChanged += OnTrackChanged;
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            MusicX.Services.MediaPlayerService.OnTrackChanged -= OnTrackChanged;
            return Task.CompletedTask;
        }

        private void OnTrackChanged(object sender, EventArgs e)
        {
            var track = MusicX.Services.MediaPlayerService.CurrentTrack;
            if (track != null)
            {
                string log = $"[{DateTime.Now:HH:mm:ss}] {track.Title} - {track.Artist}\n";
                System.IO.File.AppendAllText("track_log.txt", log);
            }
        }
    }
}
```

### Пример 2: Аддон-уведомление о любимых треках

```csharp
using System;
using System.Threading.Tasks;
using VK_UI3.Addons;
using VK_UI3.Views.Notification;

namespace FavoriteTrackNotifier
{
    public class FavoriteNotifier : IAddon
    {
        public string Id => "favorite_notifier";
        public string Name => "Favorite Notifier";
        public string Version => "1.0.0";
        public string Author => "Developer";
        public string Description => "Показывает уведомление, когда играет трек из вашего плейлиста 'Любимые'";

        public Task InitializeAsync()
        {
            MusicX.Services.MediaPlayerService.OnTrackChanged += OnTrackChanged;
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            MusicX.Services.MediaPlayerService.OnTrackChanged -= OnTrackChanged;
            return Task.CompletedTask;
        }

        private void OnTrackChanged(object sender, EventArgs e)
        {
            var track = MusicX.Services.MediaPlayerService.CurrentTrack;
            if (track != null && track.IsLiked)
            {
                new Notification(
                    "♥ Играет любимый трек!",
                    $"{track.Title} - {track.Artist}"
                );
            }
        }
    }
}
```

---

## ❓ Часто задаваемые вопросы

**Вопрос:** Какую версию .NET использовать?
**Ответ:** Ту же, что и VK M — .NET 6.0 или .NET 8.0.

**Вопрос:** Можно ли использовать сторонние NuGet-пакеты?
**Ответ:** Да, но они должны быть совместимы с VK M.

**Вопрос:** Как отладить аддон?
**Ответ:** Используйте `System.Diagnostics.Debug.WriteLine()` — вывод будет в окно Output в Visual Studio.

**Вопрос:** Что делать, если аддон не загружается?
**Ответ:** Проверьте:
1. DLL реализует интерфейс `IAddon`?
2. Версия .NET совпадает с VK M?
3. Все зависимости аддона доступны?

**Вопрос:** Может ли аддон иметь настройки?
**Ответ:** Да, используйте `VK_UI3.DB.SettingsTable.SetSetting()` и `GetSetting()` для хранения настроек.

**Вопрос:** Как обновить аддон?
**Ответ:** Просто замените файлы в папке аддона в репозитории. Приложение будет скачивать новую версию при следующей установке.

---

## 🎯 Практический пример: Time Tracker

Давайте разберём создание полноценного аддона **Time Tracker**, который отслеживает время, проведённое в VK M, и показывает уведомления о достижениях.

### Что делает аддон

- Считает общее время в приложении
- Считает время за сегодня
- Автоматически сохраняет данные каждые 30 секунд
- Показывает уведомления при достижении 1, 5, 10, 24, 50, 100, 200, 500, 1000 часов
- Показывает приветственное уведомление при первом запуске

### Структура проекта

```
TimeTracker/
├── TimeTracker.cs       # Основной код аддона
├── TimeTracker.csproj   # Файл проекта
├── addon.json           # Манифест
├── icon.png             # Иконка
└── README.md            # Описание
```

### Шаг 1: Создайте проект

1. В Visual Studio: **File → New → Project → Class Library**
2. Название: `TimeTracker`
3. .NET версия: **6.0** (как у VK M)

### Шаг 2: Настройте .csproj

Замените содержимое `TimeTracker.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0-windows10.0.19041.0</TargetFramework>
    <RuntimeIdentifiers>win10-x86;win10-x64;win10-arm64</RuntimeIdentifiers>
    <UseWinUI>true</UseWinUI>
    <Platforms>x86;x64;arm64</Platforms>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <!-- Ссылка на VK UI3 -->
    <ProjectReference Include="..\..\VK UI3\VK UI3.csproj">
      <Private>true</Private>
      <CopyLocalSatelliteAssemblies>true</CopyLocalSatelliteAssemblies>
    </ProjectReference>
  </ItemGroup>

</Project>
```

### Шаг 3: Напишите код аддона

Полный код в файле [`AddonExamples/TimeTracker/TimeTracker.cs`](AddonExamples/TimeTracker/TimeTracker.cs):

```csharp
using System;
using System.Threading.Tasks;
using VK_UI3.Addons;
using VK_UI3.DB;
using VK_UI3.Views.Notification;

namespace TimeTracker
{
    public class TimeTracker : IAddon
    {
        // ===== МЕТАДАННЫЕ =====
        public string Id => "time_tracker";
        public string Name => "Time Tracker";
        public string Version => "1.0.0";
        public string Author => "VK M Community";
        public string Description => "Отслеживает время в приложении";

        // ===== СОСТОЯНИЕ =====
        private DateTime _sessionStart;
        private System.Threading.Timer _saveTimer;
        private System.Threading.Timer _achievementTimer;

        // ===== КЛЮЧИ БАЗЫ ДАННЫХ =====
        private const string KEY_TOTAL = "timetracker_total_seconds";
        private const string KEY_TODAY = "timetracker_today_seconds";
        private const string KEY_TODAY_DATE = "timetracker_today_date";
        private const string KEY_ACHIEVEMENT = "timetracker_last_achievement";
        private const string KEY_FIRST_LAUNCH = "timetracker_first_launch";

        // ===== ПОРОГИ ДОСТИЖЕНИЙ (часы) =====
        private static readonly int[] Achievements = { 1, 5, 10, 24, 50, 100, 200, 500, 1000 };

        public Task InitializeAsync()
        {
            _sessionStart = DateTime.Now;

            // Сброс дневного счётчика, если новый день
            ResetDailyIfNeeded();

            // Запоминаем первый запуск
            if (SettingsTable.GetSetting(KEY_FIRST_LAUNCH) == null)
                SettingsTable.SetSetting(KEY_FIRST_LAUNCH, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Подписка на закрытие приложения
            App.Current.Exit += (s, e) => SaveSession();

            // Таймер сохранения — каждые 30 секунд
            _saveTimer = new System.Threading.Timer(
                _ => SaveSession(), null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            // Таймер проверки достижений — каждые 60 секунд
            _achievementTimer = new System.Threading.Timer(
                _ => CheckAchievements(), null,
                TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

            // Приветственное уведомление
            ShowWelcome();

            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            _saveTimer?.Dispose();
            _achievementTimer?.Dispose();
            SaveSession();
            return Task.CompletedTask;
        }

        // ===== СОХРАНЕНИЕ =====
        private void SaveSession()
        {
            var seconds = (long)(DateTime.Now - _sessionStart).TotalSeconds;
            if (seconds < 1) return;

            AddSeconds(KEY_TOTAL, seconds);
            AddSeconds(KEY_TODAY, seconds);
            _sessionStart = DateTime.Now;
        }

        private void AddSeconds(string key, long seconds)
        {
            var current = GetSeconds(key);
            SettingsTable.SetSetting(key, (current + seconds).ToString());
        }

        private long GetSeconds(string key)
        {
            var s = SettingsTable.GetSetting(key);
            return (s != null && long.TryParse(s.settingValue, out var v)) ? v : 0;
        }

        // ===== ДНЕВНОЙ СБРОС =====
        private void ResetDailyIfNeeded()
        {
            var saved = SettingsTable.GetSetting(KEY_TODAY_DATE);
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (saved == null || saved.settingValue != today)
            {
                SettingsTable.SetSetting(KEY_TODAY_DATE, today);
                SettingsTable.SetSetting(KEY_TODAY, "0");
            }
        }

        // ===== ДОСТИЖЕНИЯ =====
        private void CheckAchievements()
        {
            var totalHours = GetSeconds(KEY_TOTAL) / 3600.0;
            var lastAchievement = SettingsTable.GetSetting(KEY_ACHIEVEMENT);
            var lastHours = lastAchievement != null ? int.Parse(lastAchievement.settingValue) : 0;

            foreach (var hours in Achievements)
            {
                if (totalHours >= hours && hours > lastHours)
                {
                    SettingsTable.SetSetting(KEY_ACHIEVEMENT, hours.ToString());
                    ShowAchievement(hours);
                    break;
                }
            }
        }

        // ===== УВЕДОМЛЕНИЯ =====
        private void ShowWelcome()
        {
            if (SettingsTable.GetSetting(KEY_FIRST_LAUNCH) != null) return;

            MainWindow.dispatcherQueue.TryEnqueue(() =>
            {
                new Notification(
                    "⏱ Time Tracker активирован!",
                    "Теперь я буду отслеживать время, проведённое в VK M.");
            });
        }

        private void ShowAchievement(int hours)
        {
            var titles = new Dictionary<int, string>
            {
                { 1, "Первый час в VK M! 🌱" },
                { 5, "5 часов! Начало положено! 🌿" },
                { 10, "10 часов! Уже привыкаешь? 🌳" },
                { 24, "Целые сутки в VK M! ⭐" },
                { 50, "50 часов! Настоящий меломан! 🌟" },
                { 100, "100 часов! Ты живёшь музыкой! 💫" },
                { 200, "200 часов! Легендарный слушатель! 👑" },
                { 500, "500 часов! VK M — твой второй дом! 💎" },
                { 1000, "1000 часов! Ты — икона стиля! 🔥" }
            };

            var total = FormatTime(GetSeconds(KEY_TOTAL));
            var today = FormatTime(GetSeconds(KEY_TODAY));
            var firstLaunch = SettingsTable.GetSetting(KEY_FIRST_LAUNCH)?.settingValue ?? "?";

            MainWindow.dispatcherQueue.TryEnqueue(() =>
            {
                new Notification(
                    $"🏆 {titles.GetValueOrDefault(hours, $"{hours} часов!")}",
                    $"Всего: {total}\nСегодня: {today}\nВ приложении с: {firstLaunch}");
            });
        }

        private static string FormatTime(long seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}д {ts.Hours}ч {ts.Minutes}мин";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}ч {ts.Minutes}мин";
            return $"{ts.Minutes}мин {ts.Seconds}сек";
        }
    }
}
```

### Шаг 4: Создайте манифест addon.json

```json
{
  "id": "time_tracker",
  "name": "Time Tracker",
  "version": "1.0.0",
  "author": "VK M Community",
  "description": "Отслеживает время, проведённое в приложении, и показывает уведомления о достижениях.",
  "minAppVersion": "1.0.0",
  "addonType": "other",
  "entryPoint": "TimeTracker.dll"
}
```

### Шаг 5: Соберите проект

1. ПКМ по проекту → **Build**
2. DLL появится в `bin\Debug\net6.0-windows10.0.19041.0\TimeTracker.dll`

### Шаг 6: Подготовьте файлы для репозитория

Создайте папку `Addons/TimeTracker/` и поместите:

| Файл | Откуда взять |
|------|-------------|
| `addon.json` | создали на шаге 4 |
| `TimeTracker.dll` | из `bin\Debug\...` |
| `icon.png` | создайте иконку 64×64 |
| `README.md` | напишите описание |

### Шаг 7: Опубликуйте

Сделайте Pull Request в [`Music-M_Addons`](https://github.com/Werhes/Music-M_Addons) или добавьте файлы напрямую.

### Что дальше?

После публикации аддон появится в **Магазине дополнений** VK M. Пользователи смогут установить его одной кнопкой.

**Идеи для улучшения аддона:**
- Добавить подсчёт времени прослушивания музыки (через `MediaPlayerService.PositionChanged`)
- Добавить график активности по дням недели
- Добавить экспорт статистики в JSON
- Добавить сравнение с друзьями (через VK API)
- Добавить звуковое оповещение при достижении

---

Полный код этого примера доступен в папке [`AddonExamples/TimeTracker/`](AddonExamples/TimeTracker/) этого репозитория.