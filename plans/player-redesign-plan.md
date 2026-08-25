# План редизайна AudioPlayer

## Цель
Сделать плеер в VK UI3 таким же, как в MusicX-WPF:
1. **Закруглённый корпус** — скруглённые углы (CornerRadius ~8-12px) с тенью
2. **Адаптация под обложку трека** — фон плеера использует обложку трека с blur-эффектом
3. **Появление только при воспроизведении** — плеер скрыт, пока не начнёт играть музыка

## Текущее состояние

### AudioPlayer.xaml (VK UI3)
- Прямоугольный плеер без скруглений
- Фон — системный (Acrylic/Transparent)
- Всегда видим внизу MainView
- Обложка трека — только маленькая иконка 60×60 в Grid.Column="0"
- Красная полоса прогресса (RedRectangle) — нестандартный элемент

### PlayerControl.xaml (MusicX-WPF)
- `CornerRadius="8"` на ShadowShape и BlurringShape
- Фон — blur-изображение обложки трека (BackgroundCard с BlurEffect)
- Появляется/скрывается через Visibility и анимации
- Обложка трека 50×50 с скруглением 5px
- Тень (DropShadowEffect)

## План изменений

### Шаг 1: Изменить AudioPlayer.xaml — закруглённый корпус

**Файл:** `VK UI3/Controllers/AudioPlayer.xaml`

1. Оборачиваем весь RootGrid в Border с `CornerRadius="8"` и тенью:
   ```xml
   <Border CornerRadius="8" Background="{ThemeResource CardBackgroundFillColorDefault}">
       <Grid x:Name="RootGrid" ...>
           <!-- существующий контент -->
       </Grid>
   </Border>
   ```

2. Добавляем `ThemeShadow` для тени:
   ```xml
   <Border.Resources>
       <ThemeShadow x:Name="PlayerShadow" />
   </Border.Resources>
   ```

3. Убираем RedRectangle (красная полоса) — в MusicX-WPF её нет

### Шаг 2: Добавить blur-фон из обложки трека

**Файл:** `VK UI3/Controllers/AudioPlayer.xaml`

1. Добавляем слой с blur-изображением обложки под основным контентом:
   ```xml
   <!-- Blur-фон из обложки -->
   <Grid x:Name="BlurBackground" Opacity="0.4">
       <Image x:Name="BlurImage" Stretch="UniformToFill" />
   </Grid>
   ```

2. В WinUI 3 нет встроенного BlurEffect, но можно использовать:
   - `LuminosityBlendEffect` из Win2D
   - Или просто полупрозрачный акриловый фон `AcrylicInAppFillColorBaseBrush`
   - **Рекомендация:** использовать `AcrylicInAppFillColorBaseBrush` как фон плеера — это даст эффект матового стекла, похожий на blur

3. Фоновая картинка обложки загружается из `ImageThumb.Source` и дублируется в `BlurImage`

### Шаг 3: Скрывать плеер когда нет трека

**Файл:** `VK UI3/Controllers/AudioPlayer.xaml.cs`

1. Добавить проверку в `AudioPlayer_oniVKUpdate` и `AudioPlayer_AudioPlayedChange`:
   - Если `MediaPlayerService.PlayingTrack == null` → `Visibility = Visibility.Collapsed`
   - Если трек есть → `Visibility = Visibility.Visible`

2. В `MainView.xaml` убрать `Frame` вокруг плеера (или оставить, но управлять видимостью через AudioPlayer)

**Файл:** `VK UI3/Views/MainView.xaml`

3. Убрать `BorderThickness="0,1,0,0"` у Frame плеера — граница не нужна при скруглённом плеере

### Шаг 4: Обновить стиль кнопок и элементов

**Файл:** `VK UI3/Controllers/AudioPlayer.xaml`

1. Увеличить обложку трека с 60×60 до 50×50 (как в MusicX) с `CornerRadius="6"`
2. Сделать кнопки управления более компактными
3. Обновить слайдер позиции — убрать красную полосу, использовать стандартный Slider

### Шаг 5: Анимация появления/скрытия

**Файл:** `VK UI3/Controllers/AudioPlayer.xaml`

1. Добавить Storyboard для анимации появления (SlideIn) и скрытия (SlideOut)
2. При появлении трека — плеер выезжает снизу с анимацией

## Файлы для изменений

| Файл | Изменения |
|------|-----------|
| `VK UI3/Controllers/AudioPlayer.xaml` | Закруглённый корпус, blur-фон, скрытие/показ, анимации |
| `VK UI3/Controllers/AudioPlayer.xaml.cs` | Логика скрытия/показа, загрузка blur-фона |
| `VK UI3/Views/MainView.xaml` | Убрать границу у Frame плеера |

## Приоритет выполнения

1. **Скругление корпуса** — самое простое и заметное изменение
2. **Скрытие плеера** — важная функциональность
3. **Blur-фон из обложки** — визуальное улучшение
4. **Анимации** — полировка

## Технические ограничения WinUI 3

- В WinUI 3 нет `BlurEffect` как в WPF. Вместо этого:
  - Использовать `AcrylicInAppFillColorBaseBrush` для эффекта матового стекла
  - Или использовать Win2D `GaussianBlurEffect` через `Microsoft.Graphics.Canvas`
- `ThemeShadow` работает только через `DropShadow` на `Rectangle` с `Translation`
- Для скрытия плеера использовать `Visibility` с `Collapsed` (не `Hidden`, чтобы не занимал место)