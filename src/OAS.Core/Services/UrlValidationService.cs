// Open Asphalte
// Copyright (C) 2026 Open Asphalte Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace OpenAsphalte.Services;

/// <summary>
/// Service de validation des URLs pour la sécurité.
/// Centralise les contrôles de sécurité pour éviter les duplications.
/// </summary>
public static class UrlValidationService
{
    /// <summary>
    /// Liste blanche des domaines autorisés pour les mises à jour
    /// </summary>
    private static readonly string[] AllowedUpdateHosts =
    {
        "github.com",
        "gitlab.com",
        "bitbucket.org"
    };

    /// <summary>
    /// Valide qu'une URL de mise à jour est sécurisée.
    /// </summary>
    /// <param name="url">URL à valider</param>
    /// <returns>True si l'URL est valide et sécurisée, false sinon</returns>
    /// <remarks>
    /// Critères de validation :
    /// - URL non nulle et non vide
    /// - URL absolue valide
    /// - Protocole HTTPS uniquement
    /// - Domaine dans la liste blanche
    /// </remarks>
    public static bool IsValidUpdateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // N'autoriser que HTTPS pour la sécurité
        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;

        // Vérifier que le domaine est dans la liste blanche
        return AllowedUpdateHosts.Any(host =>
            uri.Host.EndsWith(host, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Valide qu'une URL est sécurisée (HTTPS uniquement).
    /// </summary>
    /// <param name="url">URL à valider</param>
    /// <returns>True si l'URL utilise HTTPS, false sinon</returns>
    public static bool IsSecureUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// Vérifie si un domaine est dans la liste blanche des mises à jour.
    /// </summary>
    /// <param name="host">Nom de domaine à vérifier</param>
    /// <returns>True si le domaine est autorisé</returns>
    public static bool IsAllowedHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        return AllowedUpdateHosts.Any(allowed =>
            host.EndsWith(allowed, StringComparison.OrdinalIgnoreCase));
    }
}
