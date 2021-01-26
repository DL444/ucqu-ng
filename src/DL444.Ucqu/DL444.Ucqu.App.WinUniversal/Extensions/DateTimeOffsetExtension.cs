using System;

namespace DL444.Ucqu.App.WinUniversal.Extensions
{
    internal static class DateTimeOffsetExtension
    {
        public static DateTimeOffset GetLocalDate(this DateTimeOffset dateTimeOffset) => dateTimeOffset.LocalDateTime.Date;
    }
}
