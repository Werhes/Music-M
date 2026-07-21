using System;
using System.Threading.Tasks;

namespace VK_UI3.Addons
{
    /// <summary>
    /// Интерфейс, который должно реализовывать каждое расширение (аддон).
    /// Расширение представляет собой DLL, реализующую этот интерфейс.
    /// </summary>
    public interface IAddon
    {
        /// <summary>
        /// Уникальный идентификатор расширения
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Отображаемое название расширения
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Версия расширения
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Автор расширения
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Описание расширения
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Вызывается при инициализации расширения.
        /// Здесь расширение должно зарегистрировать свои хуки и подписки.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Вызывается при выгрузке расширения.
        /// Здесь расширение должно отчистить все свои ресурсы.
        /// </summary>
        Task ShutdownAsync();
    }
}