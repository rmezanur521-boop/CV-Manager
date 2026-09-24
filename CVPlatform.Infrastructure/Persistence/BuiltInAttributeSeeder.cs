using CVPlatform.Domain.Constants;
using CVPlatform.Domain.Entities;
using CVPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Persistence;

public static class BuiltInAttributeSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var category = await db.AttributeCategories
            .FirstAsync(c => c.Name == "Personal Information");

        var definitions = new (string Name, AttributeType Type, string Description)[]
        {
            (BuiltInAttributes.FirstName, AttributeType.SingleLineText, "Candidate first name"),
            (BuiltInAttributes.LastName, AttributeType.SingleLineText, "Candidate last name"),
            (BuiltInAttributes.Location, AttributeType.SingleLineText, "City / country"),
            (BuiltInAttributes.PersonalPhoto, AttributeType.Image, "Personal photo")
        };

        var names = definitions.Select(d => d.Name).ToList();
        var existing = await db.Attributes.Where(a => names.Contains(a.Name)).ToListAsync();

        foreach (var (name, type, description) in definitions)
        {
            var attribute = existing.FirstOrDefault(a => a.Name == name);

            if (attribute is null)
            {
                db.Attributes.Add(new AttributeDefinition
                {
                    Name = name,
                    Type = type,
                    Description = description,
                    CategoryId = category.Id,
                    IsBuiltIn = true,
                    Version = 1
                });
            }
            else if (!attribute.IsBuiltIn)
            {
                attribute.IsBuiltIn = true; 
            }
        }

        await db.SaveChangesAsync();
    }
}