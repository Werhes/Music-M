using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using MusicX.Services;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VK_UI3.Services;
using VK_UI3.Views.ModalsPages;
using VkNet.Extensions.DependencyInjection;
using static VK_UI3.Views.SectionView;

namespace VK_UI3.Views
{
    /// <summary>
    /// Страница с dev-функциями. Отображается только при включённых dev-функциях.
    /// </summary>
    public sealed partial class DevFeaturesPage : Page
    {
        public DevFeaturesPage()
        {
            this.InitializeComponent();
            BuildPlayerButtons();
            BuildTrayButtons();
            BetterPlayerToggle.IsChecked = VK_UI3.Services.CacheSettingsManager.IsBetterPlayerEnabled();
        }

        private void BetterPlayerToggle_Changed(object sender, RoutedEventArgs e)
        {
            bool enabled = BetterPlayerToggle.IsChecked == true;
            VK_UI3.Services.CacheSettingsManager.SetBetterPlayerEnabled(enabled);
            Views.MainView.mainView?.UpdateBetterPlayerVisibility();
        }

        private static readonly (string id, string name)[] TrayButtons =
        {
            ("playPause", "Плей / Пауза"),
            ("next", "Следующий трек"),
            ("previous", "Предыдущий трек"),
            ("expand", "Развернуть"),
            ("settings", "Открыть настройки")
        };

        private void BuildTrayButtons()
        {
            TrayButtonsPanel.Children.Clear();
            var disabled = VK_UI3.Services.CacheSettingsManager.GetDisabledTrayButtons();

            foreach (var (id, name) in TrayButtons)
            {
                bool isDisabled = disabled.Contains(id);

                var status = new TextBlock
                {
                    Text = isDisabled ? "Выключено" : "Включено",
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.7,
                    MinWidth = 110
                };

                var toggleBtn = new Button
                {
                    Content = name,
                    Tag = id,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                toggleBtn.Click += (s, e) => ToggleTrayButton((string)((Button)s).Tag, status);

                var row = new Grid
                {
                    ColumnSpacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(toggleBtn, 0);
                Grid.SetColumn(status, 1);
                row.Children.Add(toggleBtn);
                row.Children.Add(status);

                TrayButtonsPanel.Children.Add(row);
            }
        }

        private void ToggleTrayButton(string id, TextBlock status)
        {
            var disabled = VK_UI3.Services.CacheSettingsManager.GetDisabledTrayButtons();
            bool isDisabled = disabled.Contains(id);

            VK_UI3.Services.CacheSettingsManager.SetTrayButtonDisabled(id, !isDisabled);
            status.Text = isDisabled ? "Включено" : "Выключено";
        }

        private static readonly (string id, string name)[] PlayerButtons =
        {
            ("fullscreen", "Открыть полноэкранный плеер"),
            ("lyrics", "Открыть текст песни"),
            ("equalizer", "Эквалайзер"),
            ("playlist", "Текущий список воспроизведения"),
            ("status", "Транслировать в статус ВК"),
            ("trackActions", "Действия с треком"),
            ("listenTogether", "Совместное прослушивание"),
            ("repeat", "Повтор")
        };

        private void BuildPlayerButtons()
        {
            PlayerButtonsPanel.Children.Clear();
            var hidden = VK_UI3.Services.CacheSettingsManager.GetHiddenPlayerButtons();

            foreach (var (id, name) in PlayerButtons)
            {
                var status = new TextBlock
                {
                    Text = hidden.Contains(id) ? "Выключено" : "Включено",
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.7,
                    MinWidth = 110
                };

                var toggleBtn = new Button
                {
                    Content = name,
                    Tag = id,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                toggleBtn.Click += (s, e) => TogglePlayerButton((string)((Button)s).Tag, status);

                var row = new Grid
                {
                    ColumnSpacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(toggleBtn, 0);
                Grid.SetColumn(status, 1);
                row.Children.Add(toggleBtn);
                row.Children.Add(status);

                PlayerButtonsPanel.Children.Add(row);
            }
        }

        private void TogglePlayerButton(string id, TextBlock status)
        {
            var hidden = VK_UI3.Services.CacheSettingsManager.GetHiddenPlayerButtons();
            bool isHidden = hidden.Contains(id);

            VK_UI3.Services.CacheSettingsManager.SetPlayerButtonHidden(id, !isHidden);
            status.Text = isHidden ? "Включено" : "Выключено";

            Controllers.AudioPlayer.Current?.ApplyHiddenButtons();
        }

        private async void OpenModal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Transitions = new TransitionCollection
            {
                new PopupThemeTransition()
            };
            dialog.Content = new TextBlock
            {
                Text = "Тестовая модалка",
                FontSize = 18,
                Margin = new Thickness(16)
            };
            dialog.CloseButtonText = "Закрыть";
            await dialog.ShowAsync();
        }

        private void GetTrackId_Click(object sender, RoutedEventArgs e)
        {
            var audio = MediaPlayerService.PlayingTrack?.audio;
            if (audio == null)
            {
                new VK_UI3.Views.Notification.Notification("Трек не играет", "Сейчас ничего не воспроизводится");
                return;
            }

            string id = $"{audio.OwnerId}_{audio.Id}";
            string link = audio.Url?.ToString() ?? $"https://vk.ru/audio{id}";

            string message = $"ID: {id}\nСсылка: {link}";

            try
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(message);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                message += "\n\nСкопировано в буфер обмена";
            }
            catch
            {
                // Буфер обмена может быть недоступен — пропускаем
            }

            new VK_UI3.Views.Notification.Notification("Трек", message);
        }

        private async void OpenCreatePlaylistModal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Transitions = new TransitionCollection
            {
                new PopupThemeTransition()
            };
            dialog.Content = new CreatePlayList();
            dialog.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            await dialog.ShowAsync();
        }

        private void OpenGeneratePlaylistModal_Click(object sender, RoutedEventArgs e)
        {
            var iVKGetAudio = VK_UI3.Services.MediaPlayerService.iVKGetAudio;
            if (iVKGetAudio == null)
            {
                new VK_UI3.Views.Notification.Notification("Нет источника", "Сейчас нет активного источника аудио для генерации");
                return;
            }

            MainView.mainView?.openGenerator(iVKGetAudio: iVKGetAudio, unicID: "dev_generator", genBy: "genBy");
        }

        private void HideToTray_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.mainWindow?.HideFromTaskbar();
        }

        private void SendSystemToast_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Helpers.SystemToastHelper.Show("Music M", "Сообщение из dev-функций");
            }
            catch (Exception ex)
            {
                new VK_UI3.Views.Notification.Notification("Тост недоступен", ex.Message);
            }
        }

        private void OpenSectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(section.Text))
                return;

            MainView.OpenSection(section.Text.Trim());
        }

        private void OpenArtist_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(artist.Text))
                return;

            MainView.OpenSection(artist.Text.Trim(), SectionType.Artist);
        }

        private void ShowNotification_Click(object sender, RoutedEventArgs e)
        {
            new VK_UI3.Views.Notification.Notification("Заголовок", "Сообщение");
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(url.Text))
                return;

            try
            {
                download.IsEnabled = false;
                string input = url.Text.Trim();

                var folder = new DirectoryInfo(Path.Combine(StaticService.UserDataFolder.FullName, "devdownloads"));
                if (!folder.Exists)
                    folder.Create();

                string fileName = $"track_{DateTime.Now:yyyyMMdd_HHmmss}.mp3";
                string output = Path.Combine(folder.FullName, fileName);

                var ffmpegOptions = FfmpegSettingsManager.GetSettingsDictionary();
                await FFMediaToolkit.FFmpegLoader.DownloadAndConvertWithFFmpegAutogenWithOptions(input, output, ffmpegOptions);

                new VK_UI3.Views.Notification.Notification("Скачивание завершено", output);
            }
            catch (Exception ex)
            {
                new VK_UI3.Views.Notification.Notification("Ошибка скачивания", ex.Message);
            }
            finally
            {
                download.IsEnabled = true;
            }
        }

        private async void RaiseCaptcha_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var solver = StaticService.Container.GetRequiredService<IAsyncCaptchaSolver>();
                var result = await solver.SolveAsync("https://api.vk.com/captcha.php?sid=123456&s=1");

                new VK_UI3.Views.Notification.Notification("Капча", $"Вы ввели '{result ?? "ничего"}'");
            }
            catch (Exception ex)
            {
                new VK_UI3.Views.Notification.Notification("Ошибка капчи", ex.Message);
            }
        }

        private void Mixer_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(ValueMixer.Text.Replace(',', '.'), out float value))
            {
                MediaPlayerService.Volume = Math.Clamp(value, 0f, 1f);
                CurrentMixer.Text = $"Текущее значение: {MediaPlayerService.Volume:0.##}";
            }
        }

        private void MixerGet_Click(object sender, RoutedEventArgs e)
        {
            CurrentMixer.Text = $"Текущее значение: {MediaPlayerService.Volume:0.##}";
        }

        private void PlayerJson_Click(object sender, RoutedEventArgs e)
        {
            var playing = MediaPlayerService.PlayingTrack?.audio;
            var session = MediaPlayerService.MediaPlayer.PlaybackSession;

            var state = new
            {
                title = playing?.Title,
                artist = playing?.Artist,
                url = playing?.Url,
                duration = playing?.Duration,
                position = session?.Position.ToString(),
                playbackState = session?.PlaybackState.ToString(),
                volume = MediaPlayerService.Volume
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });

            PlayerJsonOutput.Text = json;
            PlayerJsonOutput.Visibility = Visibility.Visible;

            new VK_UI3.Views.Notification.Notification("JSON плеера", json);
        }
    }
}