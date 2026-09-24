using CVPlatform.Domain.Entities;

namespace CVPlatform.Application.Positions;

public interface IPositionAccessEvaluator
{
    Task<bool> IsCandidateEligibleAsync(string candidateId, int positionId);
    Task<HashSet<int>> GetEligiblePositionIdsAsync(string candidateId, IReadOnlyCollection<int> positionIds);
    Task<HashSet<(string CandidateId, int PositionId)>> GetEligibleCandidatePositionPairsAsync(IReadOnlyCollection<(string CandidateId, int PositionId)> pairs);
}