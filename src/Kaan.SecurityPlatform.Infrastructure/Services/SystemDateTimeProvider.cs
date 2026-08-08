using Kaan.SecurityPlatform.Application.Common.Interfaces;

namespace Kaan.SecurityPlatform.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
