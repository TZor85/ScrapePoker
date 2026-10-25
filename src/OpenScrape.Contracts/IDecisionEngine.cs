namespace OpenScrape.Contracts;

/// <summary>
/// Puerto único de decisión. Lo consumen el cliente (por HTTP) y el motor
/// (implementación local); vive en Contracts para que ninguno de los dos
/// dependa del ensamblado del otro.
/// </summary>
public interface IDecisionEngine
{
    Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}
