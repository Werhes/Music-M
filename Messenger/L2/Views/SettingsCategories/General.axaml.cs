using Avalonia;
using Avalonia.Controls;
using ELOR.Laney.Core;
using ELOR.Laney.DataModels;
using ELOR.Laney.Views.Modals;
using System;

namespace ELOR.Laney.Views.SettingsCategories {
    public partial class General : UserControl {
        // Возврат в Music-M: запускаем Music M (он сам поднимает своё окно из трея)
        // и закрываем мессенджер.
        private void ReturnToMusicM_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            string exe = App.GetCmdLineValue("returnexe");
            if (string.IsNullOrEmpty(exe)) return;

            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = exe, UseShellExecute = true });
            } catch { }

            try {
                (Application.Current as App)?.DesktopLifetime?.Shutdown();
            } catch { }
        }
        string oldLangId;
        public General() {
            InitializeComponent();
            oldLangId = Settings.Get(Settings.LANGUAGE, Constants.DefaultLang);
        }

        private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
            Window window = TopLevel.GetTopLevel(this) as Window;
            if (window == null) return;

            if (e.AddedItems.Count == 1) {
                TwoStringTuple value = e.AddedItems[0] as TwoStringTuple;
                if (value.Item1 == oldLangId) return;

                new System.Action(async () => {
                    VKUIDialog alert = new VKUIDialog(Assets.i18n.Resources.restart_required, Assets.i18n.Resources.restart_required_ext);
                    await alert.ShowDialog(window);
                })();
            }
        }
    }
}