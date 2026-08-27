using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using VK_UI3.Helpers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VK_UI3.Views
{
    public sealed partial class LockScreenWindow : Window
    {
        private AppWindow _appWindow;

        public LockScreenWindow()
        {
            this.InitializeComponent();

            // Делаем окно таким же, как главное окно приложения:
            // название, иконка и кастомный заголовок с системными кнопками (свернуть/развернуть/закрыть).
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Название как у приложения
            _appWindow.Title = "Music M";

            // Иконка как у приложения
            TrySetIcon();

            // Кастомный заголовок как в главном окне (системные кнопки справа подтянутся автоматически)
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            // Цвет кнопок окна под тему приложения
            try
            {
                var titleBar = _appWindow.TitleBar;
                titleBar.ButtonForegroundColor = (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    ? Colors.White : Colors.Black;
            }
            catch { }
        }

        private void TrySetIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
                if (!File.Exists(iconPath))
                    iconPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");

                if (File.Exists(iconPath))
                    _appWindow.SetIcon(iconPath);
            }
            catch { }
        }

        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            TryUnlock();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Оставляем приложение заблокированным (скрытым в трее)
            this.Close();
        }

        private void PinBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                TryUnlock();
            }
        }

        private void TryUnlock()
        {
            if (LockManager.VerifyPin(PinBox.Password))
            {
                LockManager.IsLocked = false;
                // Показываем главное окно и закрываем экран блокировки
                MainWindow.mainWindow?.ShowWindowAgain();
                this.Close();
            }
            else
            {
                ErrorText.Text = "Неверный код. Попробуйте ещё раз.";
                ErrorText.Visibility = Visibility.Visible;
                PinBox.Password = string.Empty;
                PinBox.Focus(FocusState.Programmatic);
            }
        }
    }
}