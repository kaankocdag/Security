namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly TodayUtc { get; }
    DateTimeOffset UtcNowOffset { get; }
}
