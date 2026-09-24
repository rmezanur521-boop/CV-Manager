using CVPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Persistence;

public static class AttributeCategorySeeder
{
    private static readonly string[] DefaultCategories =
    {
        "Certification",
        "Domain Knowledge",
        "Personal Information",
        "Soft Skills"
    };

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var existingNames = await db.AttributeCategories
            .Select(c => c.Name)
            .ToListAsync();

        foreach (var name in DefaultCategories)
        {
            if (!existingNames.Contains(name))
            {
                db.AttributeCategories.Add(new AttributeCategory { Name = name });
            }
        }

        await db.SaveChangesAsync();
    }
}