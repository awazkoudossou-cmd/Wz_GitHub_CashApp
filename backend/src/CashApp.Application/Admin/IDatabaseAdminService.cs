namespace CashApp.Application.Admin;

public interface IDatabaseAdminService
{
    // Réinitialise complètement la base : drop schéma + recrée + reseed admin/settings/features/catégories.
    // Vide aussi les répertoires de fichiers attachés et imports.
    Task ResetAsync(CancellationToken ct = default);
}
