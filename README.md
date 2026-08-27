[![TotalDownloads](https://img.shields.io/github/downloads/Werhes/Music-M/total?label=Загрузок)](https://github.com/Werhes/Music-M/releases "Download")
[![OS - Windows](https://img.shields.io/badge/OS-Windows-blue?logo=windows&logoColor=white)](https://github.com/Werhes/Music-M/releases "Download")
[![GitHub Release](https://img.shields.io/github/v/release/Werhes/Music-M?include_prereleases&label=Latest%20Release)](https://github.com/Werhes/Music-M/releases)

## Описание
Music M — это абсолютно бесплатное приложение для прослушивания музыки в социальной сети VK.
Music M - По сути своей адаптация VK X и Music X, перенесённый на интерфейс WinUI3. По этому, в нём много схожестей.

Что есть?

- Скачивание треков
- Генерация плейлистов
- Полноценный поиск
- Удаление и архивирование аудиозаписей из профиля.
- Мультиаккаунт
- Просмотр аудио из вложений
- Возможность поделиться треками.
- Возможность удалить все треки из профиля в ВК
- Возможность заархивировать все треки в ВК
- Просмотр музыкальных видео
- Discord RPC
- Полноэкранный плеер
- Режим мессенджера (Music M messenger-mode) — отдельный VK-мессенджер, запускаемый из настроек и авторизуемый токеном текущего аккаунта

Чего нет? 

- Реклама отсутствует.
- Возможности взломать ВК нет.
- Передача личных данных третьим лицам не производится (весь код открытый для проверки).
- Создания аккаунта вк без номера телефона.

## Star History

<a href="https://www.star-history.com/?repos=werhes%2FMusic-M&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=werhes/Music-M&type=date&theme=dark&legend=top-left&sealed_token=zadibJMp2brhuL9GwwTGXg6iLq9y5wJ11XhCdPhaeB8v9DB18QuRAvLSAMJRof5HtWrGugCwXhg7axPsqn6l6r89ZHB1vqGtVbGKkmxpLDWVscLoUk4Flw" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=werhes/Music-M&type=date&legend=top-left&sealed_token=zadibJMp2brhuL9GwwTGXg6iLq9y5wJ11XhCdPhaeB8v9DB18QuRAvLSAMJRof5HtWrGugCwXhg7axPsqn6l6r89ZHB1vqGtVbGKkmxpLDWVscLoUk4Flw" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=werhes/Music-M&type=date&legend=top-left&sealed_token=zadibJMp2brhuL9GwwTGXg6iLq9y5wJ11XhCdPhaeB8v9DB18QuRAvLSAMJRof5HtWrGugCwXhg7axPsqn6l6r89ZHB1vqGtVbGKkmxpLDWVscLoUk4Flw" />
 </picture>
</a>

## Режим мессенджера (Music M messenger-mode)
В настройках → **Интеграции** → **Режим мессенджера (Music M)** есть кнопка «Переключиться в режим мессенджера».
Она запускает отдельный мессенджер на базе [Laney-Avalonia](https://github.com/Elorucov/Laney-Avalonia) и сразу передаёт ему токен текущего аккаунта Music M, поэтому он открывается уже авторизованным.

- Исходники мессенджера лежат в папке `Messenger/` (переименованы и ребрендированы как «Music M (messenger-mode)», используется иконка Music M).
- При запуске через Music M данные аккаунта кладутся в отдельную локальную папку `%LOCALAPPDATA%\MusicM\Messenger` (аргумент `-ldp=`), поэтому не пересекаются с обычной установкой Laney.
- В CI ([`.github/workflows/new.yml`](.github/workflows/new.yml)) мессенджер собирается отдельной задачей `build_messenger` и кладётся в папку `Messenger/` внутри zip-сборки VK UI3.

### Как собрать мессенджер локально
```pwsh
# требует .NET SDK 10
dotnet publish "Messenger\L2\L2.csproj" -c Release -r win-x64 --self-contained true -o messenger_out
# переименовать messenger_out\laney.exe -> MusicMMessenger.exe и положить рядом с приложением
```

## Благодарочка
Отдельная благодарность [Fooxboy/MusicX-WPF](https://github.com/Fooxboy/MusicX-WPF)https://github.com/Fooxboy/MusicX-WPF
Много чего скопипасчено именно отсюда.
