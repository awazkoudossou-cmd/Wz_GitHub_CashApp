namespace CashApp.Application.Common.Interfaces;

// Vérification d'accès utilisateur aux caisses + filtrage de requêtes.
public interface ICashRegisterAccessService
{
    Task<bool> CanAccessAsync(int userId, int cashRegisterId, CancellationToken ct = default);
    Task EnsureCanAccessAsync(int cashRegisterId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetAccessibleRegisterIdsAsync(CancellationToken ct = default);
}
