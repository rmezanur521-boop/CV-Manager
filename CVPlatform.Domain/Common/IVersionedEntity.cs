namespace CVPlatform.Domain.Common;

public interface IVersionedEntity
{
    int Version { get; set; }
}