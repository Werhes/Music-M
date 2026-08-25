using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using WinRT.Interop;

namespace VK_UI3.Views
{
    public sealed partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            this.InitializeComponent();

            // Настраиваем окно без рамки
            var appWindow = this.AppWindow;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            // Убираем кнопки закрытия/сворачивания/разворачивания
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }

            // Размер окна под размер изображения startup.png (1198x736)
            // + 4px на прогресс-бар
            appWindow.Resize(new Windows.Graphics.SizeInt32(1198, 740));

            // Центрируем окно на экране
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                var centerX = (displayArea.WorkArea.Width - 1198) / 2;
                var centerY = (displayArea.WorkArea.Height - 740) / 2;
                appWindow.Move(new Windows.Graphics.PointInt32((int)centerX, (int)centerY));
            }
        }

        /// <summary>
        /// Показывает splash screen и ждёт указанное количество миллисекунд,
        /// затем закрывается и возвращает управление.
        /// </summary>
        public async Task ShowAndWaitAsync(int delayMs = 3000)
        {
            this.Activate();

            // Ждём указанное время
            await Task.Delay(delayMs);
        }
    }
}