using CashApp.Application.Attachments.Dtos;

namespace CashApp.Application.Attachments;

public record UploadFileInput(string OriginalFileName, string ContentType, long Length, Stream Content);

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentDto>> ListForEntityAsync(string entityType, int entityId, CancellationToken ct = default);
    Task<AttachmentDto> GetAsync(int id, CancellationToken ct = default);
    Task<UploadAttachmentResponseDto> UploadAsync(string entityType, int entityId, string? description, UploadFileInput file, CancellationToken ct = default);
    Task<(Stream Content, string ContentType, string OriginalFileName)> DownloadAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
