using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.Win32;
using Windows.UI.Notifications;

namespace VK_UI3.Helpers
{
    /// <summary>
    /// Отправляет системный тост-уведомление для unpkg (unpackaged) приложения.
    /// Для корректной работы регистрирует AUMID в реестре и создаёт ярлык в меню «Пуск».
    /// </summary>
    internal static class SystemToastHelper
    {
        public const string Aumid = "MusicM.MusicM";

        private static readonly Guid CLSID_ShellLink = new Guid("00021401-0000-0000-C000-000000000046");
        private static readonly Guid IID_IShellLink = new Guid("000214F9-0000-0000-C000-000000000046");
        private static readonly Guid IID_IPropertyStore = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

        private static bool _registered;

        private static void EnsureRegistered()
        {
            if (_registered)
                return;

            string exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe))
                return;

            // 1. Регистрация в реестре
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{Aumid}"))
                {
                    key?.SetValue("DisplayName", "Music M");
                    if (!string.IsNullOrEmpty(exe))
                        key?.SetValue("IconUri", exe);
                }
            }
            catch
            {
                // игнорируем
            }

            // 2. Ярлык в меню «Пуск» с AppUserModelID (обязателен для тостов unpkg)
            try
            {
                string startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft\\Windows\\Start Menu\\Programs");

                string shortcutPath = Path.Combine(startMenu, "Music M.lnk");
                if (!File.Exists(shortcutPath))
                    CreateShortcutWithAumid(shortcutPath, exe);
            }
            catch
            {
                // игнорируем
            }

            _registered = true;
        }

        private static void CreateShortcutWithAumid(string shortcutPath, string exe)
        {
            object link = null;
            try
            {
                Type shellLinkType = Type.GetTypeFromCLSID(CLSID_ShellLink);
                if (shellLinkType == null)
                    return;

                link = Activator.CreateInstance(shellLinkType);

                var shellLink = (IShellLinkW)link;
                shellLink.SetPath(exe);
                shellLink.SetWorkingDirectory(Path.GetDirectoryName(exe));
                shellLink.SetIconLocation(exe, 0);
                shellLink.SetDescription("Music M");

                // Сохраняем файл ярлыка
                var persistFile = (IPersistFile)link;
                persistFile.Save(shortcutPath, true);

                // Устанавливаем AppUserModelID через IPropertyStore
                IntPtr propertyStorePtr;
                var propertyStoreIid = IID_IPropertyStore;
                int hr = SHGetPropertyStoreFromParsingName(
                    shortcutPath, IntPtr.Zero, 0, ref propertyStoreIid, out propertyStorePtr);
                if (hr == 0 && propertyStorePtr != IntPtr.Zero)
                {
                    var propertyStore = (IPropertyStore)Marshal.GetObjectForIUnknown(propertyStorePtr);
                    try
                    {
                        var key = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5); // PKEY_AppUserModel_ID
                        var propVar = new PROPVARIANT { vt = (ushort)VarEnum.VT_LPWSTR, pointer = Marshal.StringToCoTaskMemUni(Aumid) };
                        propertyStore.SetValue(ref key, ref propVar);
                        propertyStore.Commit();
                        Marshal.FreeCoTaskMem(propVar.pointer);
                    }
                    finally
                    {
                        Marshal.Release(propertyStorePtr);
                    }
                }
            }
            finally
            {
                if (link is IDisposable d)
                    d.Dispose();
            }
        }

        public static void Show(string title, string message)
        {
            EnsureRegistered();

            var toastXml = new Windows.Data.Xml.Dom.XmlDocument();
            toastXml.LoadXml(
                "<toast>" +
                "<visual><binding template='ToastGeneric'>" +
                $"<text>{Escape(title)}</text>" +
                $"<text>{Escape(message)}</text>" +
                "</binding></visual>" +
                "</toast>");

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier(Aumid).Show(toast);
        }

        private static string Escape(string text)
        {
            return System.Security.SecurityElement.Escape(text ?? string.Empty);
        }

        #region Interop

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PropertyKey pkey);
            void GetValue(ref PropertyKey key, out PROPVARIANT pv);
            void SetValue(ref PropertyKey key, ref PROPVARIANT pv);
            void Commit();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
            public PropertyKey(Guid fmtid, uint pid)
            {
                this.fmtid = fmtid;
                this.pid = pid;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROPVARIANT
        {
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public IntPtr pointer;
            public IntPtr reserved;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetPropertyStoreFromParsingName(
            string pszPath, IntPtr pbc, uint flags, ref Guid riid, out IntPtr ppv);

        #endregion
    }
}