// Copyright 2026 Open Asphalte Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
