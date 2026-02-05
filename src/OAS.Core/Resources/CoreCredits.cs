using OpenAsphalte.Abstractions;

namespace OpenAsphalte.Core.Resources;

/// <summary>
/// Registre statique des crédits du Core pour l'affichage dans l'interface.
/// </summary>
public static class CoreCredits
{
    public static IReadOnlyList<Contributor> Team { get; } = new List<Contributor>
    {
        // Architectes & Lead
        new Contributor("Charles TILLY", "Lead Architect", "https://linkedin.com/in/charlestilly"),
        
        // Développement
        new Contributor("GitHub Copilot", "AI Assistant", "https://github.com/features/copilot"),
        
        // Contributeurs
        // Ajoutez les nouveaux contributeurs ici
    };
}
