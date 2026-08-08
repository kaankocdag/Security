using System.Security.Cryptography;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly ILogger<LocalFileStorage> _logger;
    private readonly string _rootPath;
    private readonly string _publicBaseUrl;

    public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
    {
        _logger = logger;
        _rootPath = configuration["Storage:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "storage");
        _publicBaseUrl = configuration["Storage:PublicBaseUrl"] ?? "/storage";

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string subFolder,
        CancellationToken cancellationToken = default)
    {
        var safeSubFolder = SanitizeFolder(subFolder);
        var folderPath = Path.Combine(_rootPath, safeSubFolder);
        Directory.CreateDirectory(folderPath);

        var extension = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(folderPath, fileName);
        var relativePath = Path.Combine(safeSubFolder, fileName).Replace('\\', '/');

        long size;
        string hash;
        await using (var target = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            using var sha = SHA256.Create();
            await using var crypto = new CryptoStream(target, sha, CryptoStreamMode.Write);
            await content.CopyToAsync(crypto, cancellationToken);
            crypto.FlushFinalBlock();
            size = target.Position;
            hash = Convert.ToHexString(sha.Hash!);
        }

        _logger.LogInformation("Dosya kaydedildi: {Path} ({Size} bayt)", relativePath, size);

        return new StoredFile(
            relativePath,
            BuildPublicUrl(relativePath),
            size,
            contentType,
            hash);
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(_rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(_rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public string BuildPublicUrl(string storagePath)
    {
        var normalized = storagePath.Replace('\\', '/').TrimStart('/');
        return $"{_publicBaseUrl.TrimEnd('/')}/{normalized}";
    }

    private static string SanitizeFolder(string subFolder)
    {
        if (string.IsNullOrWhiteSpace(subFolder))
        {
            return "misc";
        }

        var invalidChars = Path.GetInvalidFileNameChars().Union(new[] { '/', '\\', '.', ':' }).ToArray();
        var parts = subFolder.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => new string(part.Where(c => !invalidChars.Contains(c)).ToArray()));
        return string.Join(Path.DirectorySeparatorChar, parts);
    }
}
