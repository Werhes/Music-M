using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VK_UI3.Services;

namespace VK_UI3.Views.Settings
{
    /// <summary>
    /// Кнопка для отображения списка кешированных треков в диалоговом окне.
    /// </summary>
    public sealed class ShowCachedTracksButton : Button
    {
        public ShowCachedTracksButton()
        {
            this.Content = "Показать кешированные треки";
            this.Click += OnClick;
            this.Loaded += OnLoaded;
            this.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;

            AutomationProperties.SetName(this, "Показать кешированные треки");
            AutomationProperties.SetHelpText(this, "Открывает окно со списком всех кешированных треков");
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await UpdateButtonTextAsync();
        }

        private async void OnClick(object sender, RoutedEventArgs e)
        {
            await ShowCachedTracksDialogAsync();
        }

        private async Task UpdateButtonTextAsync()
        {
            await Task.Run(() =>
            {
                int fileCount = TrackCacheManager.GetCacheFileCount();
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.Content = $"Показать кешированные треки ({fileCount})";
                });
            });
        }

        private async Task ShowCachedTracksDialogAsync()
        {
            // Собираем данные в фоне
            var tracks = await Task.Run(() => TrackCacheManager.GetCachedTracks());

            var viewModel = new ObservableCollection<CachedTrackItem>();
            foreach (var track in tracks)
            {
                viewModel.Add(new CachedTrackItem
                {
                    OwnerId = track.OwnerId,
                    AudioId = track.AudioId,
                    FileName = track.FileName,
                    SizeStr = FormatSize(track.SizeBytes),
                    CachedDateStr = track.CachedDate.ToString("dd.MM.yyyy HH:mm")
                });
            }

            long totalSize = tracks.Sum(t => t.SizeBytes);

            // Создаём содержимое диалога
            var stackPanel = new StackPanel
            {
                Spacing = 8,
                MinWidth = 400,
                MaxWidth = 600
            };

            // Заголовок с общей информацией
            var infoText = new TextBlock
            {
                Text = $"Всего треков: {tracks.Count}, общий размер: {FormatSize(totalSize)}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(infoText);

            if (tracks.Count > 0)
            {
                var listView = new ListView
                {
                    ItemsSource = viewModel,
                    MaxHeight = 400,
                    SelectionMode = ListViewSelectionMode.None
                };

                // Создаём DataTemplate программно через XAML-загрузчик
                listView.ItemTemplate = CreateDataTemplate();
                stackPanel.Children.Add(listView);
            }
            else
            {
                var emptyText = new TextBlock
                {
                    Text = "Кеш треков пуст",
                    Opacity = 0.6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };
                stackPanel.Children.Add(emptyText);
            }

            var dialog = new ContentDialog
            {
                Title = "Кешированные треки",
                Content = stackPanel,
                PrimaryButtonText = "Закрыть",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            dialog.Resources["ContentDialogMaxWidth"] = double.PositiveInfinity;

            await dialog.ShowAsync();
        }

        private static DataTemplate CreateDataTemplate()
        {
            // Создаём DataTemplate через XAML
            string xaml = @"
                <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <StackPanel Orientation='Horizontal' Spacing='8'>
                        <TextBlock Text='{Binding FileName}' 
                                   VerticalAlignment='Center' 
                                   MinWidth='200' />
                        <TextBlock Text='{Binding SizeStr}' 
                                   VerticalAlignment='Center' 
                                   Opacity='0.6' 
                                   MinWidth='80' />
                        <TextBlock Text='{Binding CachedDateStr}' 
                                   VerticalAlignment='Center' 
                                   Opacity='0.5' />
                    </StackPanel>
                </DataTemplate>";
            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} КБ";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} МБ";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";
        }
    }

    /// <summary>
    /// Модель для отображения кешированного трека в списке.
    /// </summary>
    public class CachedTrackItem
    {
        public long OwnerId { get; set; }
        public long AudioId { get; set; }
        public string FileName { get; set; }
        public string SizeStr { get; set; }
        public string CachedDateStr { get; set; }
    }
}