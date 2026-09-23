namespace resolve_AI.Application.DTOs;

/// <summary>
/// Temporary Microsoft 365 user information model.
///
/// MOCK/TBD:
/// Final fields will be aligned with the real Microsoft Graph
/// response and project requirements when confirmed.
/// </summary>
public class Microsoft365UserDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool Found { get; set; }

    public bool IsMock { get; set; } = true;
}