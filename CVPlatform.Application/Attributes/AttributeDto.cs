using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Attributes;

public record AttributeCategoryDto(int Id, string Name);

public record AttributeOptionDto(int Id, string Value, int DisplayOrder);

public record AttributeDto(
    int Id,
    string Name,
    string? Description,
    AttributeType Type,
    int CategoryId,
    string CategoryName,
    bool IsBuiltIn,
    int Version,
    IReadOnlyList<AttributeOptionDto> Options);