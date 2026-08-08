namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string subFolder,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    string BuildPublicUrl(string storagePath);
}

public sealed record StoredFile(
    string StoragePath,
    string PublicUrl,
    long SizeBytes,
    string ContentType,
    string Sha256Hash);
