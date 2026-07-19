using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VK_UI3.VKs;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using Windows.UI;

namespace VK_UI3.Views.Messenger
{
    /// <summary>
    /// Модель диалога для отображения в списке.
    /// </summary>
    public class DialogItem : INotifyPropertyChanged
    {
        private string _title;
        private string _lastMessage;
        private string _photoUrl;
        private long _peerId;
        private int _unreadCount;

        public long PeerId
        {
            get => _peerId;
            set { _peerId = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; OnPropertyChanged(); }
        }

        public string PhotoUrl
        {
            get => _photoUrl;
            set { _photoUrl = value; OnPropertyChanged(); }
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set { _unreadCount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Модель пересланного сообщения.
    /// </summary>
    public class ForwardedMessageItem
    {
        public string ForwardedFrom { get; set; }
        public string ForwardedText { get; set; }
    }

    /// <summary>
    /// Модель сообщения для отображения в переписке.
    /// </summary>
    public class MessageItem : INotifyPropertyChanged
    {
        private string _text;
        private string _time;
        private HorizontalAlignment _alignment;
        private Brush _bubbleBrush;
        private bool _isOutgoing;
        private string _senderPhotoUrl;
        private Visibility _senderAvatarVisibility;
        private Visibility _textVisibility;
        private ObservableCollection<string> _photoAttachments = new();
        private Visibility _photoAttachmentsVisibility;
        private ObservableCollection<string> _videoAttachments = new();
        private Visibility _videoAttachmentsVisibility;
        private ObservableCollection<ForwardedMessageItem> _forwardedMessages = new();
        private Visibility _forwardedMessagesVisibility;

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public string Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(); }
        }

        public HorizontalAlignment Alignment
        {
            get => _alignment;
            set { _alignment = value; OnPropertyChanged(); }
        }

        public Brush BubbleBrush
        {
            get => _bubbleBrush;
            set { _bubbleBrush = value; OnPropertyChanged(); }
        }

        public string SenderPhotoUrl
        {
            get => _senderPhotoUrl;
            set { _senderPhotoUrl = value; OnPropertyChanged(); }
        }

        public Visibility SenderAvatarVisibility
        {
            get => _senderAvatarVisibility;
            set { _senderAvatarVisibility = value; OnPropertyChanged(); }
        }

        public Visibility TextVisibility
        {
            get => _textVisibility;
            set { _textVisibility = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> PhotoAttachments
        {
            get => _photoAttachments;
            set { _photoAttachments = value; OnPropertyChanged(); }
        }

        public Visibility PhotoAttachmentsVisibility
        {
            get => _photoAttachmentsVisibility;
            set { _photoAttachmentsVisibility = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> VideoAttachments
        {
            get => _videoAttachments;
            set { _videoAttachments = value; OnPropertyChanged(); }
        }

        public Visibility VideoAttachmentsVisibility
        {
            get => _videoAttachmentsVisibility;
            set { _videoAttachmentsVisibility = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ForwardedMessageItem> ForwardedMessages
        {
            get => _forwardedMessages;
            set { _forwardedMessages = value; OnPropertyChanged(); }
        }

        public Visibility ForwardedMessagesVisibility
        {
            get => _forwardedMessagesVisibility;
            set { _forwardedMessagesVisibility = value; OnPropertyChanged(); }
        }

        public bool IsOutgoing
        {
            get => _isOutgoing;
            set
            {
                _isOutgoing = value;
                Alignment = value ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                BubbleBrush = value
                    ? new SolidColorBrush(Color.FromArgb(255, 48, 104, 208))
                    : new SolidColorBrush(Color.FromArgb(30, 128, 128, 128));
                SenderAvatarVisibility = value ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Страница мессенджера для управления переписками ВКонтакте.
    /// </summary>
    public sealed partial class MessengerView : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private ObservableCollection<DialogItem> _dialogs = new();
        private ObservableCollection<MessageItem> _messages = new();
        private DialogItem _selectedDialog;
        private bool _isLoading = false;
        private bool _isLoadingMore = false;
        private bool _hasMoreMessages = true;
        private long _currentPeerId;
        private int _currentOffset = 0;
        private const int MessagePageSize = 200;
        private readonly Dictionary<long, string> _userPhotoCache = new();

        // Свойства для закреплённого сообщения
        private string _pinnedMessageText;
        private Visibility _pinnedMessageVisibility = Visibility.Collapsed;

        public string PinnedMessageText
        {
            get => _pinnedMessageText;
            set { _pinnedMessageText = value; OnPropertyChanged(); }
        }

        public Visibility PinnedMessageVisibility
        {
            get => _pinnedMessageVisibility;
            set { _pinnedMessageVisibility = value; OnPropertyChanged(); }
        }

        public DialogItem SelectedDialog
        {
            get => _selectedDialog;
            set
            {
                _selectedDialog = value;
                OnPropertyChanged(nameof(SelectedDialog));
                OnPropertyChanged(nameof(SelectedDialogTitle));
                OnPropertyChanged(nameof(SelectedDialogPhotoUrl));
            }
        }

        public string SelectedDialogTitle => SelectedDialog?.Title ?? "";
        public string SelectedDialogPhotoUrl => SelectedDialog?.PhotoUrl ?? "";

        public MessengerView()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            DialogsList.ItemsSource = _dialogs;
            MessagesList.ItemsSource = _messages;

            this.Loaded += MessengerView_Loaded;
            this.Unloaded += MessengerView_Unloaded;
        }

        private async void MessengerView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDialogsAsync();
        }

        private void MessengerView_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MessengerView_Loaded;
            this.Unloaded -= MessengerView_Unloaded;
        }

        /// <summary>
        /// Загружает список диалогов через VK API.
        /// </summary>
        private async Task LoadDialogsAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            await _dispatcherQueue.TryEnqueueAsync(() =>
            {
                LoadingIndicator.IsActive = true;
            });

            try
            {
                var api = VK.api;

                var conversations = await api.Messages.GetConversationsAsync(new GetConversationsParams
                {
                    Count = 50,
                    Extended = true,
                    Fields = new[] { "photo_50", "photo_100", "first_name", "last_name" }
                });

                if (conversations?.Items == null) return;

                var dialogs = new List<DialogItem>();

                foreach (var conv in conversations.Items)
                {
                    var dialog = new DialogItem
                    {
                        PeerId = conv.Conversation.Peer.Id,
                        Title = GetDialogTitle(conv, conversations.Profiles, conversations.Groups),
                        LastMessage = conv.LastMessage?.Text ?? "",
                        UnreadCount = (int)(conv.Conversation.UnreadCount ?? 0)
                    };

                    dialog.PhotoUrl = GetDialogPhotoUrl(conv, conversations.Profiles, conversations.Groups);

                    dialogs.Add(dialog);
                }

                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    _dialogs.Clear();
                    foreach (var d in dialogs)
                    {
                        _dialogs.Add(d);
                    }
                });
            }
            catch (Exception ex)
            {
                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Ошибка загрузки диалогов",
                        Content = $"Не удалось загрузить диалоги: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                });
            }
            finally
            {
                _isLoading = false;
                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    LoadingIndicator.IsActive = false;
                });
            }
        }

        /// <summary>
        /// Загружает историю сообщений для выбранного диалога.
        /// </summary>
        private async Task LoadMessagesAsync(long peerId)
        {
            if (_isLoading) return;
            _isLoading = true;

            _currentPeerId = peerId;
            _currentOffset = 0;
            _hasMoreMessages = true;

            await _dispatcherQueue.TryEnqueueAsync(() =>
            {
                LoadingIndicator.IsActive = true;
                _messages.Clear();
                PinnedMessageVisibility = Visibility.Collapsed;
                PinnedMessageText = "";
            });

            try
            {
                var api = VK.api;

                // Загружаем историю сообщений
                // Без Reversed (по умолчанию false) — сначала новые сообщения
                var history = await api.Messages.GetHistoryAsync(new MessagesGetHistoryParams
                {
                    PeerId = peerId,
                    Count = MessagePageSize,
                    Extended = true,
                    Fields = new[] { "photo_50", "first_name", "last_name" }
                });

                if (history?.Messages == null) return;

                // Кэшируем URL аватаров из профилей ДО создания сообщений,
                // чтобы GetSenderPhotoUrl мог найти их в кэше
                if (history.Users != null)
                {
                    foreach (var profile in history.Users)
                    {
                        if (!_userPhotoCache.ContainsKey(profile.Id))
                        {
                            _userPhotoCache[profile.Id] = profile.Photo50?.ToString() ?? "";
                        }
                    }
                }

                // Ищем закреплённое сообщение в истории
                string pinnedText = null;
                foreach (var msg in history.Messages)
                {
                    if (msg.PinnedAt.HasValue)
                    {
                        pinnedText = !string.IsNullOrEmpty(msg.Text)
                            ? msg.Text
                            : GetAttachmentDescription(msg);
                        break;
                    }
                }

                // Создаём сообщения в порядке от старых к новым
                // VK API возвращает: сначала новые, потом старые
                // Нам нужно: сначала старые, потом новые
                var messagesList = history.Messages.ToList();
                messagesList.Reverse(); // теперь от старых к новым

                var messages = new List<MessageItem>();

                foreach (var msg in messagesList)
                {
                    var messageItem = CreateMessageItem(msg);
                    messages.Add(messageItem);
                }

                _currentOffset = messages.Count;
                if (messages.Count < MessagePageSize)
                {
                    _hasMoreMessages = false;
                }

                var pinned = pinnedText;
                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    _messages.Clear();
                    foreach (var m in messages)
                    {
                        _messages.Add(m);
                    }

                    // Устанавливаем закреплённое сообщение
                    if (!string.IsNullOrEmpty(pinned))
                    {
                        PinnedMessageText = pinned;
                        PinnedMessageVisibility = Visibility.Visible;
                    }

                    ScrollToBottom();
                });
            }
            catch (Exception ex)
            {
                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Ошибка загрузки сообщений",
                        Content = $"Не удалось загрузить сообщения: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                });
            }
            finally
            {
                _isLoading = false;
                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    LoadingIndicator.IsActive = false;
                });
            }
        }

        /// <summary>
        /// Создаёт MessageItem из объекта Message VK API.
        /// </summary>
        private MessageItem CreateMessageItem(Message msg)
        {
            var isOutgoing = msg.FromId == VK_UI3.DB.AccountsDB.activeAccount.id;
            var senderPhotoUrl = GetSenderPhotoUrl(msg.FromId ?? 0);

            var messageItem = new MessageItem
            {
                Text = msg.Text ?? "",
                Time = msg.Date?.ToString("HH:mm") ?? "",
                IsOutgoing = isOutgoing,
                SenderPhotoUrl = senderPhotoUrl
            };

            // Скрываем текст, если его нет
            messageItem.TextVisibility = string.IsNullOrEmpty(msg.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            // Обрабатываем вложения
            if (msg.Attachments != null && msg.Attachments.Count > 0)
            {
                var photoUrls = new ObservableCollection<string>();
                var videoUrls = new ObservableCollection<string>();

                foreach (var attachment in msg.Attachments)
                {
                    if (attachment.Instance is VkNet.Model.Attachments.Photo photo)
                    {
                        var url = photo.Photo604?.ToString()
                            ?? photo.Photo130?.ToString()
                            ?? photo.Photo75?.ToString()
                            ?? "";
                        if (!string.IsNullOrEmpty(url))
                            photoUrls.Add(url);
                    }
                    else if (attachment.Instance is VkNet.Model.Attachments.Video video)
                    {
                        var url = video.Image?.FirstOrDefault()?.Url?.ToString()
                            ?? video.Photo130?.ToString()
                            ?? video.Photo320?.ToString()
                            ?? "";
                        if (!string.IsNullOrEmpty(url))
                            videoUrls.Add(url);
                    }
                }

                messageItem.PhotoAttachments = photoUrls;
                messageItem.PhotoAttachmentsVisibility = photoUrls.Count > 0
                    ? Visibility.Visible : Visibility.Collapsed;

                messageItem.VideoAttachments = videoUrls;
                messageItem.VideoAttachmentsVisibility = videoUrls.Count > 0
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                messageItem.PhotoAttachmentsVisibility = Visibility.Collapsed;
                messageItem.VideoAttachmentsVisibility = Visibility.Collapsed;
            }

            // Обрабатываем пересланные сообщения
            if (msg.ForwardedMessages != null && msg.ForwardedMessages.Count > 0)
            {
                var forwardedItems = new ObservableCollection<ForwardedMessageItem>();

                foreach (var fwd in msg.ForwardedMessages)
                {
                    var fromName = fwd.FromId.HasValue
                        ? GetSenderName(fwd.FromId.Value)
                        : "Пользователь";

                    forwardedItems.Add(new ForwardedMessageItem
                    {
                        ForwardedFrom = fromName,
                        ForwardedText = !string.IsNullOrEmpty(fwd.Text)
                            ? fwd.Text
                            : GetAttachmentDescription(fwd)
                    });
                }

                messageItem.ForwardedMessages = forwardedItems;
                messageItem.ForwardedMessagesVisibility = Visibility.Visible;
            }
            else
            {
                messageItem.ForwardedMessagesVisibility = Visibility.Collapsed;
            }

            return messageItem;
        }

        /// <summary>
        /// Получает описание вложений для сообщения без текста.
        /// </summary>
        private string GetAttachmentDescription(Message msg)
        {
            if (msg.Attachments == null || msg.Attachments.Count == 0)
                return "📎 Вложение";

            var types = msg.Attachments
                .Select(a => a.Type?.ToString() ?? "")
                .Distinct()
                .ToList();

            if (types.Contains("photo")) return "📷 Фото";
            if (types.Contains("video")) return "🎬 Видео";
            if (types.Contains("audio")) return "🎵 Аудио";
            if (types.Contains("doc")) return "📄 Документ";
            if (types.Contains("sticker")) return "🎨 Стикер";
            if (types.Contains("wall")) return "📝 Запись";
            if (types.Contains("link")) return "🔗 Ссылка";

            return "📎 Вложение";
        }

        /// <summary>
        /// Получает имя отправителя по ID.
        /// </summary>
        private string GetSenderName(long userId)
        {
            if (_userPhotoCache.ContainsKey(userId))
                return "Пользователь";

            // Пытаемся загрузить
            _ = LoadUserPhotoAsync(userId);
            return "Пользователь";
        }

        /// <summary>
        /// Загружает более старые сообщения (пагинация).
        /// </summary>
        private async Task LoadMoreMessagesAsync()
        {
            if (_isLoadingMore || !_hasMoreMessages) return;
            _isLoadingMore = true;

            try
            {
                var api = VK.api;

                // Без Reversed — сначала новые, offset пропускает последние N сообщений
                var history = await api.Messages.GetHistoryAsync(new MessagesGetHistoryParams
                {
                    PeerId = _currentPeerId,
                    Count = MessagePageSize,
                    Offset = _currentOffset,
                    Extended = true,
                    Fields = new[] { "photo_50", "first_name", "last_name" }
                });

                if (history?.Messages == null || !history.Messages.Any())
                {
                    _hasMoreMessages = false;
                    return;
                }

                var messagesCount = history.Messages.Count();

                if (history.Users != null)
                {
                    foreach (var profile in history.Users)
                    {
                        if (!_userPhotoCache.ContainsKey(profile.Id))
                        {
                            _userPhotoCache[profile.Id] = profile.Photo50?.ToString() ?? "";
                        }
                    }
                }

                // VK API возвращает: сначала новые, потом старые
                // Нам нужно вставить перед текущими сообщениями: сначала старые, потом новые
                var messagesList = history.Messages.ToList();
                messagesList.Reverse(); // теперь от старых к новым

                var oldMessages = new List<MessageItem>();

                foreach (var msg in messagesList)
                {
                    var messageItem = CreateMessageItem(msg);
                    oldMessages.Add(messageItem);
                }

                _currentOffset += oldMessages.Count;
                if (messagesCount < MessagePageSize)
                {
                    _hasMoreMessages = false;
                }

                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    for (int i = oldMessages.Count - 1; i >= 0; i--)
                    {
                        _messages.Insert(0, oldMessages[i]);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMoreMessages error: {ex.Message}");
            }
            finally
            {
                _isLoadingMore = false;
            }
        }

        /// <summary>
        /// Получает URL аватара отправителя из кэша или загружает через API.
        /// </summary>
        private string GetSenderPhotoUrl(long userId)
        {
            if (userId == VK_UI3.DB.AccountsDB.activeAccount.id)
                return "";

            if (_userPhotoCache.TryGetValue(userId, out var cachedUrl))
                return cachedUrl;

            _ = LoadUserPhotoAsync(userId);
            return "";
        }

        /// <summary>
        /// Асинхронно загружает фото пользователя и обновляет кэш.
        /// </summary>
        private async Task LoadUserPhotoAsync(long userId)
        {
            try
            {
                var api = VK.api;
                var users = await api.Users.GetAsync(new[] { userId.ToString() },
                    VkNet.Enums.Filters.ProfileFields.Photo50);

                if (users != null && users.Count > 0)
                {
                    var photoUrl = users[0].Photo50?.ToString() ?? "";
                    _userPhotoCache[userId] = photoUrl;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUserPhoto error: {ex.Message}");
            }
        }

        /// <summary>
        /// Отправляет сообщение через VK API.
        /// </summary>
        private async Task SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || SelectedDialog == null) return;

            try
            {
                var api = VK.api;

                var result = await api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = SelectedDialog.PeerId,
                    Message = text,
                    RandomId = new Random().Next()
                });

                if (result > 0)
                {
                    var message = new MessageItem
                    {
                        Text = text,
                        Time = DateTime.Now.ToString("HH:mm"),
                        IsOutgoing = true,
                        SenderPhotoUrl = "",
                        TextVisibility = Visibility.Visible,
                        PhotoAttachmentsVisibility = Visibility.Collapsed,
                        VideoAttachmentsVisibility = Visibility.Collapsed,
                        ForwardedMessagesVisibility = Visibility.Collapsed
                    };

                    await _dispatcherQueue.TryEnqueueAsync(() =>
                    {
                        _messages.Add(message);
                        ScrollToBottom();
                    });
                }
            }
            catch (Exception ex)
            {
                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Ошибка отправки",
                        Content = $"Не удалось отправить сообщение: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                });
            }
        }

        /// <summary>
        /// Прокручивает список сообщений вниз.
        /// </summary>
        private void ScrollToBottom()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    MessagesScrollViewer.ChangeView(null, MessagesScrollViewer.ScrollableHeight, null);
                });
            });
        }

        /// <summary>
        /// Получает название диалога.
        /// </summary>
        private string GetDialogTitle(ConversationAndLastMessage conv,
            IReadOnlyCollection<User> profiles,
            IReadOnlyCollection<Group> groups)
        {
            var peer = conv.Conversation.Peer;

            if (peer.Type == ConversationPeerType.Chat)
            {
                return conv.Conversation.ChatSettings?.Title ?? "Беседа";
            }

            if (peer.Type == ConversationPeerType.User || peer.Type == ConversationPeerType.Email)
            {
                var profile = profiles?.FirstOrDefault(p => p.Id == peer.LocalId);
                if (profile != null)
                {
                    return $"{profile.FirstName} {profile.LastName}";
                }
            }

            if (peer.Type == ConversationPeerType.Group)
            {
                var group = groups?.FirstOrDefault(g => g.Id == Math.Abs(peer.LocalId));
                return group?.Name ?? "Сообщество";
            }

            return $"Диалог {peer.LocalId}";
        }

        /// <summary>
        /// Получает URL фотографии диалога.
        /// </summary>
        private string GetDialogPhotoUrl(ConversationAndLastMessage conv,
            IReadOnlyCollection<User> profiles,
            IReadOnlyCollection<Group> groups)
        {
            var peer = conv.Conversation.Peer;

            if (peer.Type == ConversationPeerType.Chat)
            {
                var photo = conv.Conversation.ChatSettings?.Photo;
                return photo?.Photo50?.ToString() ?? "";
            }

            if (peer.Type == ConversationPeerType.User || peer.Type == ConversationPeerType.Email)
            {
                var profile = profiles?.FirstOrDefault(p => p.Id == peer.LocalId);
                return profile?.Photo50?.ToString() ?? "";
            }

            if (peer.Type == ConversationPeerType.Group)
            {
                var group = groups?.FirstOrDefault(g => g.Id == Math.Abs(peer.LocalId));
                return group?.Photo50?.ToString() ?? "";
            }

            return "";
        }

        /// <summary>
        /// Обработчик выбора диалога из списка.
        /// </summary>
        private async void DialogsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DialogsList.SelectedItem is DialogItem dialog)
            {
                SelectedDialog = dialog;
                await LoadMessagesAsync(dialog.PeerId);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки отправки.
        /// </summary>
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var text = MessageInput.Text;
            MessageInput.Text = "";
            await SendMessageAsync(text);
        }

        /// <summary>
        /// Обработчик нажатия Enter в поле ввода.
        /// </summary>
        private async void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                var text = MessageInput.Text;
                MessageInput.Text = "";
                await SendMessageAsync(text);
            }
        }

        /// <summary>
        /// Обработчик изменения текста поиска.
        /// </summary>
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                await LoadDialogsAsync();
                return;
            }

            try
            {
                var api = VK.api;
                var result = await api.Messages.SearchConversationsAsync(query,
                    new[] { "photo_50", "first_name", "last_name" }, 20);

                if (result?.Items == null) return;

                await _dispatcherQueue.TryEnqueueAsync(() =>
                {
                    _dialogs.Clear();
                    foreach (var conv in result.Items)
                    {
                        var title = GetSearchDialogTitle(conv, result.Profiles);
                        var photoUrl = GetSearchDialogPhotoUrl(conv, result.Profiles);

                        var dialog = new DialogItem
                        {
                            PeerId = conv.Peer.Id,
                            Title = title,
                            LastMessage = "",
                            PhotoUrl = photoUrl
                        };
                        _dialogs.Add(dialog);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
        }

        private string GetSearchDialogTitle(Conversation conv, IReadOnlyCollection<User> profiles)
        {
            if (conv.Peer.Type == ConversationPeerType.Chat)
            {
                return conv.ChatSettings?.Title ?? "Беседа";
            }

            if (conv.Peer.Type == ConversationPeerType.User || conv.Peer.Type == ConversationPeerType.Email)
            {
                var profile = profiles?.FirstOrDefault(p => p.Id == conv.Peer.LocalId);
                if (profile != null)
                {
                    return $"{profile.FirstName} {profile.LastName}";
                }
            }

            if (conv.Peer.Type == ConversationPeerType.Group)
            {
                return "Сообщество";
            }

            return $"Диалог {conv.Peer.LocalId}";
        }

        private string GetSearchDialogPhotoUrl(Conversation conv, IReadOnlyCollection<User> profiles)
        {
            if (conv.Peer.Type == ConversationPeerType.Chat)
            {
                return conv.ChatSettings?.Photo?.Photo50?.ToString() ?? "";
            }

            if (conv.Peer.Type == ConversationPeerType.User || conv.Peer.Type == ConversationPeerType.Email)
            {
                var profile = profiles?.FirstOrDefault(p => p.Id == conv.Peer.LocalId);
                return profile?.Photo50?.ToString() ?? "";
            }

            return "";
        }

        /// <summary>
        /// Обработчик кнопки "Новый диалог".
        /// </summary>
        private async void NewMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var inputTextBox = new TextBox
            {
                PlaceholderText = "ID или короткий адрес"
            };

            var inputDialog = new ContentDialog
            {
                Title = "Новый диалог",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Введите ID пользователя или короткий адрес:" },
                        inputTextBox
                    }
                },
                PrimaryButtonText = "Начать",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            inputDialog.Loaded += (s, args) =>
            {
                inputTextBox.Focus(FocusState.Programmatic);
            };

            var result = await inputDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var input = inputTextBox.Text;

                if (string.IsNullOrWhiteSpace(input)) return;

                try
                {
                    var api = VK.api;

                    var users = await api.Users.GetAsync(new[] { input },
                        VkNet.Enums.Filters.ProfileFields.Photo50);

                    if (users == null || users.Count == 0)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "Пользователь не найден",
                            Content = $"Пользователь с ID '{input}' не найден.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                        return;
                    }

                    var user = users[0];

                    var newDialog = new DialogItem
                    {
                        PeerId = user.Id,
                        Title = $"{user.FirstName} {user.LastName}",
                        PhotoUrl = user.Photo50?.ToString() ?? ""
                    };

                    _dialogs.Insert(0, newDialog);
                    DialogsList.SelectedItem = newDialog;
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Ошибка",
                        Content = $"Не удалось найти пользователя: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Extension methods for DispatcherQueue.
    /// </summary>
    internal static class DispatcherQueueExtensions
    {
        public static Task TryEnqueueAsync(this DispatcherQueue dispatcher, Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }))
            {
                tcs.TrySetException(new InvalidOperationException("Failed to enqueue on dispatcher"));
            }
            return tcs.Task;
        }
    }
}