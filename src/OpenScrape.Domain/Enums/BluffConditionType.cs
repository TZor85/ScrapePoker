namespace OpenScrape.Domain.Enums;

/// <summary>
/// Condición bajo la cual se permite un bluff.
/// Reemplaza los strings "None", "Always", "OOPOnly", "IPCoordinatedSmallOnly".
/// </summary>
public enum BluffConditionType
{
    None,
    Always,
    OOPOnly,
    IPCoordinatedSmallOnly
}
