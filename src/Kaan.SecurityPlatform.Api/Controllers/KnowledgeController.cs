using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.Knowledge;
using Kaan.SecurityPlatform.Application.Features.Knowledge.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
public sealed class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _knowledge;

    public KnowledgeController(IKnowledgeService knowledge)
    {
        _knowledge = knowledge;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
        => Ok(await _knowledge.ListCategoriesAsync(cancellationToken));

    [HttpGet("articles")]
    [AllowAnonymous]
    public async Task<IActionResult> Articles(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? tag,
        CancellationToken cancellationToken)
        => Ok(await _knowledge.ListArticlesAsync(categoryId, tag, includeUnpublished: false, cancellationToken));

    [HttpGet("articles/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> Article(string slug, CancellationToken cancellationToken)
    {
        var result = await _knowledge.GetArticleAsync(slug, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}

[ApiController]
[Route("api/admin/knowledge")]
[Authorize(Policy = PolicyNames.CanEditKnowledge)]
public sealed class AdminKnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _knowledge;

    public AdminKnowledgeController(IKnowledgeService knowledge)
    {
        _knowledge = knowledge;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
        => Ok(await _knowledge.ListCategoriesAsync(cancellationToken));

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] UpsertKnowledgeCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledge.UpsertCategoryAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpsertKnowledgeCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledge.UpsertCategoryAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _knowledge.DeleteCategoryAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    [HttpGet("articles")]
    public async Task<IActionResult> Articles([FromQuery] Guid? categoryId, [FromQuery] string? tag, CancellationToken cancellationToken)
        => Ok(await _knowledge.ListArticlesAsync(categoryId, tag, includeUnpublished: true, cancellationToken));

    [HttpPost("articles")]
    public async Task<IActionResult> CreateArticle([FromBody] UpsertKnowledgeArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledge.UpsertArticleAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPut("articles/{id:guid}")]
    public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] UpsertKnowledgeArticleRequest request, CancellationToken cancellationToken)
    {
        var result = await _knowledge.UpsertArticleAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpDelete("articles/{id:guid}")]
    public async Task<IActionResult> DeleteArticle(Guid id, CancellationToken cancellationToken)
    {
        var result = await _knowledge.DeleteArticleAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    [HttpPost("articles/{articleId:guid}/media")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(Guid articleId, IFormFile file, [FromForm] string? caption, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { errorCode = "no_file", detail = "Bir dosya yüklemelisiniz." });
        }

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { errorCode = "unsupported_media_type", detail = "Sadece görsel dosyalar (JPEG, PNG, WEBP, GIF) yüklenebilir." });
        }

        using var stream = file.OpenReadStream();
        var result = await _knowledge.UploadMediaAsync(articleId, stream, file.FileName, file.ContentType, caption, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpDelete("media/{mediaId:guid}")]
    public async Task<IActionResult> DeleteMedia(Guid mediaId, CancellationToken cancellationToken)
    {
        var result = await _knowledge.DeleteMediaAsync(mediaId, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound();
    }
}
