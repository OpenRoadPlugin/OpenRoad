namespace OpenRoad.Abstractions;

/// <summary>
/// Represents a person or entity contributing to the project.
/// </summary>
/// <param name="Name">Display name of the contributor.</param>
/// <param name="Role">Role description (e.g., "Lead Developer", "Tester").</param>
/// <param name="Url">Optional URL (LinkedIn, GitHub, Website).</param>
public record Contributor(string Name, string Role, string? Url = null);
