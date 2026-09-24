using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Projects;
using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CVPlatform.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _db;

    public ProjectService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetMyProjectsAsync(string candidateId)
    {
        var projects = await _db.Projects
            .Include(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
            .Where(p => p.CandidateId == candidateId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        return projects.Select(MapToDto).ToList();
    }

    public async Task<ProjectDto> GetByIdAsync(string candidateId, int id)
    {
        var project = await FindOwnedAsync(candidateId, id);
        return MapToDto(project);
    }

    public async Task<ProjectDto> CreateAsync(string candidateId, SaveProjectRequest request)
    {
        var project = new Project
        {
            CandidateId = candidateId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DescriptionMarkdown = request.DescriptionMarkdown,
            Version = 1
        };

        await AttachTagsAsync(project, request.Tags);

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(candidateId, project.Id);
    }

    public async Task<ProjectDto> UpdateAsync(string candidateId, SaveProjectRequest request)
    {
        var project = await FindOwnedAsync(candidateId, request.Id);

        _db.Entry(project).Property(p => p.Version).OriginalValue = request.Version;

        project.Name = request.Name;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.DescriptionMarkdown = request.DescriptionMarkdown;
        project.Version++;

        project.ProjectTags.Clear();
        await AttachTagsAsync(project, request.Tags);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        return await GetByIdAsync(candidateId, project.Id);
    }

    public async Task DeleteAsync(string candidateId, int id)
    {
        var project = await FindOwnedAsync(candidateId, id);
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<string>> SearchTagsAsync(string prefix)
    {
        return await _db.TechnologyTags
            .Where(t => t.Name.StartsWith(prefix))
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .Take(10)
            .ToListAsync();
    }

    private async Task<Project> FindOwnedAsync(string candidateId, int id)
    {
        return await _db.Projects
            .Include(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id && p.CandidateId == candidateId)
            ?? throw new NotFoundException("Project not found.");
    }

    private async Task AttachTagsAsync(Project project, List<string> tagNames)
    {
        var names = tagNames
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0) return;

        var existing = await _db.TechnologyTags
            .Where(t => names.Contains(t.Name))
            .ToListAsync();

        foreach (var name in names)
        {
            var tag = existing.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (tag is null)
            {
                tag = new TechnologyTag { Name = name };
                _db.TechnologyTags.Add(tag);
            }

            project.ProjectTags.Add(new ProjectTag { Tag = tag });
        }
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto(
            project.Id,
            project.Name,
            project.StartDate,
            project.EndDate,
            project.DescriptionMarkdown,
            project.Version,
            project.ProjectTags.Select(pt => pt.Tag.Name).OrderBy(n => n).ToList());
    }
}