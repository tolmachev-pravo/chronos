namespace Chronos.Infrastructure.Extensions.YandexCalendar
{
    // JSON stored in UserExtension.Settings — password is encrypted
    internal record YandexCalendarSettings(string Login, string AppPasswordEncrypted);
}
