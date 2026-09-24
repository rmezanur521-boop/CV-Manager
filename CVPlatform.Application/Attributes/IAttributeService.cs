namespace CVPlatform.Application.Attributes;

public interface IAttributeService
{
    Task<IReadOnlyList<AttributeDto>> GetAllAsync(int? categoryId = null, string? prefix = null);
    Task<AttributeDto> GetByIdAsync(int id);
    Task<AttributeDto> CreateAsync(CreateAttributeRequest request);
    Task<AttributeDto> UpdateAsync(UpdateAttributeRequest request);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<AttributeCategoryDto>> GetCategoriesAsync();
    Task<IReadOnlyList<AttributeDto>> GetRecentlyUsedAsync(string userId, int count = 5);
    Task RecordUsageAsync(string userId, int attributeId);
}