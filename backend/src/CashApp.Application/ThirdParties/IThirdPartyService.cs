using CashApp.Application.ThirdParties.Dtos;
using CashApp.Domain.Entities;

namespace CashApp.Application.ThirdParties;

public interface IThirdPartyService
{
    Task<IReadOnlyList<ThirdPartyDto>> ListAsync(string? search, CancellationToken ct = default);

    // Retourne le tiers existant (recherche insensible à la casse) ou en crée un nouveau.
    // Utilisé automatiquement lors de la saisie d'une opération pour alimenter la liste.
    Task<ThirdParty> FindOrCreateAsync(string name, CancellationToken ct = default);
}
