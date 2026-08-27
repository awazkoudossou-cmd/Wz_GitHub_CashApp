using CashApp.Domain.Enums;

namespace CashApp.Application.Categories.Dtos;

public record CategoryListItemDto(
    int Id,
    string Code,
    string Label,
    OperationDirection Direction,
    bool IsActive,
    DateTime CreatedAt,
    int? GroupId,
    string? GroupName);

public record CategoryDetailDto(
    int Id,
    string Code,
    string Label,
    OperationDirection Direction,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int? GroupId,
    string? GroupName);

public record CreateCategoryDto(
    string Code,
    string Label,
    OperationDirection Direction,
    string GroupName);

public record UpdateCategoryDto(
    string Label,
    OperationDirection Direction,
    string GroupName);

public record UpdateCategoryStatusDto(bool IsActive);
