namespace CashApp.Application.Attachments.Dtos;

public record AttachmentDto(
    int Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string EntityType,
    int EntityId,
    int UploadedBy,
    string UploadedByName,
    DateTime UploadedAt,
    string? Description);

public record UploadAttachmentResponseDto(AttachmentDto Attachment);
