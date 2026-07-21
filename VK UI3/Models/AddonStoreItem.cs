using System;
using System.Collections.Generic;

namespace VK_UI3.Models
{
    /// <summary>
    /// Тип элемента в магазине дополнений
    /// </summary>
    public enum AddonStoreItemType
    {
        Addon,
        Theme
    }

    /// <summary>
    /// Модель элемента из репозитория Music-M_Addons
    /// </summary>
    public class AddonStoreItem
    {
        /// <summary>
        /// Название папки элемента (уникальный идентификатор)
        /// </summary>
        public string FolderName { get; set; }

        /// <summary>
        /// Тип: аддон или тема
        /// </summary>
        public AddonStoreItemType Type { get; set; }

        /// <summary>
        /// Название элемента (из README или манифеста)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание элемента (из README)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Версия элемента
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Автор элемента
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// URL сырой ссылки на иконку в GitHub
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// URL сырой ссылки на README в GitHub
        /// </summary>
        public string ReadmeUrl { get; set; }

        /// <summary>
        /// URL сырой ссылки на файл расширения/темы
        /// </summary>
        public string FileUrl { get; set; }

        /// <summary>
        /// URL на папку в GitHub (для скачивания архива)
        /// </summary>
        public string GitHubFolderUrl { get; set; }

        /// <summary>
        /// Скачан ли уже элемент
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Путь к локально установленному файлу (после скачивания)
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// Размер файла в байтах
        /// </summary>
        public long FileSize { get; set; }
    }

    /// <summary>
    /// Манифест расширения (парсится из файла расширения или README)
    /// </summary>
    public class AddonManifest
    {
        /// <summary>
        /// Уникальный идентификатор расширения
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Отображаемое название
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Версия
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Автор
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Минимальная версия приложения, с которой совместимо расширение
        /// </summary>
        public string MinAppVersion { get; set; }

        /// <summary>
        /// Тип расширения (например "visualizer", "lyrics_provider", "notification")
        /// </summary>
        public string AddonType { get; set; }

        /// <summary>
        /// DLL файл расширения (точка входа)
        /// </summary>
        public string EntryPoint { get; set; }
    }

    /// <summary>
    /// Манифест темы
    /// </summary>
    public class ThemeManifest
    {
        /// <summary>
        /// Уникальный идентификатор темы
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Название темы
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Автор
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Версия
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Файл темы (XAML ресурсный словарь)
        /// </summary>
        public string ThemeFile { get; set; }

        /// <summary>
        /// Является ли тёмной темой
        /// </summary>
        public bool IsDark { get; set; }
    }
}