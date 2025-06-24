using System;

namespace LocalMessenger
{
    /// <summary>
    /// Статусы пользователя
    /// </summary>
    public enum UserStatus
    {
        Online,
        Busy,
        Away,
        DoNotDisturb
    }

    /// <summary>
    /// Типы сообщений
    /// </summary>
    public enum MessageType
    {
        Text,
        File,
        Image,
        Emoji,
        GroupMessage,
        StatusUpdate,
        KeyExchange
    }

    /// <summary>
    /// Режимы тем оформления
    /// </summary>
    public enum ThemeMode
    {
        Light,
        Dark
    }
}
