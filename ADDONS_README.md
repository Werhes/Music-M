# Music-M Addons

Репозиторий дополнений и тем для приложения **VK M** (Music-M).

## Структура репозитория

```
Music-M_Addons/
├── Addons/              # Папка с аддонами (расширениями)
│   └── AddonName/       # Папка аддона (название папки = название аддона)
│       ├── addon.json   # Манифест аддона (метаданные)
│       ├── icon.png     # Иконка аддона (64x64, рекомендуется)
│       ├── README.md    # Описание аддона (отображается в приложении)
│       └── AddonName.dll # DLL файл расширения
│
└── Themes/              # Папка с темами
    └── ThemeName/       # Папка темы (название папки = название темы)
        ├── theme.json   # Манифест темы (метаданные)
        ├── icon.png     # Иконка темы (64x64, рекомендуется)
        ├── README.md    # Описание темы (отображается в приложении)
        └── ThemeName.xaml # XAML ResourceDictionary файл темы
```

## Как создать аддон (расширение)

### 1. Создайте папку аддона

Создайте папку в `Addons/` с названием вашего аддона (например, `Addons/MyAwesomeAddon/`).

### 2. Создайте манифест `addon.json`

```json
{
  "id": "my_awesome_addon",
  "name": "My Awesome Addon",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Краткое описание вашего аддона",
  "minAppVersion": "1.0.0",
  "addonType": "visualizer",
  "entryPoint": "MyAwesomeAddon.dll"
}
```

Поля:
- `id` — уникальный идентификатор (латиница, без пробелов)
- `name` — отображаемое название
- `version` — версия в формате SemVer
- `author` — автор
- `description` — краткое описание
- `minAppVersion` — минимальная версия приложения
- `addonType` — тип аддона (см. ниже)
- `entryPoint` — имя DLL файла

### 3. Реализуйте интерфейс IAddon

Создайте проект библиотеки классов (.NET), добавьте ссылку на `VK UI3` и реализуйте интерфейс `VK_UI3.Addons.IAddon`:

```csharp
using System;
using System.Threading.Tasks;
using VK_UI3.Addons;

namespace MyAwesomeAddon
{
    public class MyAddon : IAddon
    {
        public string Id => "my_awesome_addon";
        public string Name => "My Awesome Addon";
        public string Version => "1.0.0";
        public string Author => "Your Name";
        public string Description => "Краткое описание вашего аддона";

        public Task InitializeAsync()
        {
            // Здесь регистрируем хуки, подписки на события
            // Например, подписка на смену трека:
            // MusicX.Services.MediaPlayerService.OnTrackChanged += OnTrackChanged;
            
            Console.WriteLine($"{Name} инициализирован!");
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            // Здесь отписываемся от всех событий и освобождаем ресурсы
            Console.WriteLine($"{Name} выгружен!");
            return Task.CompletedTask;
        }
    }
}
```

### 4. Соберите DLL и добавьте в папку

Скомпилируйте проект и поместите DLL в папку аддона.

### 5. Добавьте иконку

Поместите файл `icon.png` (рекомендуемый размер 64x64) в папку аддона.

### 6. Добавьте README.md

Создайте файл `README.md` с подробным описанием вашего аддона. Этот текст будет отображаться в приложении в разделе "Об расширении".

## Типы аддонов

| Тип | Описание |
|-----|----------|
| `visualizer` | Визуализация аудио |
| `lyrics_provider` | Провайдер текстов песен |
| `notification` | Кастомные уведомления |
| `integration` | Интеграция с внешними сервисами |
| `ui` | Модификация интерфейса |
| `other` | Другое |

## Как создать тему

### 1. Создайте папку темы

Создайте папку в `Themes/` с названием вашей темы (например, `Themes/DarkAmber/`).

### 2. Создайте манифест `theme.json`

```json
{
  "id": "dark_amber",
  "name": "Dark Amber",
  "author": "Your Name",
  "version": "1.0.0",
  "description": "Тёмная тема в янтарных тонах",
  "themeFile": "DarkAmber.xaml",
  "isDark": true
}
```

### 3. Создайте XAML файл темы

Создайте XAML ResourceDictionary с переопределением цветов и стилей:

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Переопределение цветов -->
    <Color x:Key="SystemAccentColor">#FFB8860B</Color>
    <Color x:Key="SystemAccentColorDark1">#FF8B6508</Color>
    <Color x:Key="SystemAccentColorDark2">#FF5C4305</Color>
    <Color x:Key="SystemAccentColorDark3">#FF2E2203</Color>
    <Color x:Key="SystemAccentColorLight1">#FFD4A82E</Color>
    
    <!-- Фоновые цвета -->
    <Color x:Key="ApplicationPageBackgroundThemeBrush">#FF1A1A1A</Color>
    <Color x:Key="CardBackgroundFillColorDefault">#FF2D2D2D</Color>
    
    <!-- Текстовые цвета -->
    <Color x:Key="TextFillColorPrimary">#FFE0E0E0</Color>
    <Color x:Key="TextFillColorSecondary">#FFB0B0B0</Color>
</ResourceDictionary>
```

### 4. Добавьте иконку

Поместите файл `icon.png` (рекомендуемый размер 64x64) в папку темы.

### 5. Добавьте README.md

Создайте файл `README.md` с описанием темы. Этот текст будет отображаться в приложении в разделе "Об теме".

## Пример структуры аддона

```
Addons/EqualizerVisualizer/
├── addon.json
├── icon.png
├── README.md
└── EqualizerVisualizer.dll
```

## Пример структуры темы

```
Themes/OceanBlue/
├── theme.json
├── icon.png
├── README.md
└── OceanBlue.xaml
```

## Установка

Аддоны и темы устанавливаются через встроенный **Магазин дополнений** в приложении VK M. Приложение само скачивает файлы из этого репозитория и устанавливает их.

## Лицензия

Все материалы в этом репозитории предоставляются "как есть". Авторы сохраняют права на свои работы.