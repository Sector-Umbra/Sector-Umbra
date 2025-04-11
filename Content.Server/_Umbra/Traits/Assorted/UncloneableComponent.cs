namespace Content.Server._Umbra.Traits.Assorted;

/// <summary>
/// This is used for the uncloneable trait.
/// </summary>
[RegisterComponent]
public sealed partial class UncloneableComponent : Component
{
    /// <summary>
    /// Can this player be cloned using a cloning pod?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Cloneable = false;
}
