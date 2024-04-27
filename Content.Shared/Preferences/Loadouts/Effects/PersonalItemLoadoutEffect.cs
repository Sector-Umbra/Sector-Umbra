using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
///     Implements a loadout effect that restricts items to a specific character,
///     based on the currently selected character's name.
/// </summary>
/// <remarks>
///     Sector Umbra
/// </remarks>
public sealed partial class PersonalItemLoadoutEffect : LoadoutEffect
{
    [DataField("character", required: true)]
    public string CharacterName = default!;

    public override bool Validate(
        RoleLoadout loadout,
        ICommonSession session,
        ICharacterProfile? profile,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (profile is HumanoidCharacterProfile humanoid && humanoid.Name != CharacterName)
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString(
                "loadout-personal-item-belongs-to",
                ("character", CharacterName)));
            return false;
        }

        reason = null;
        return true;
    }
}
