using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class MantidaeAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MantidaeAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, MantidaeAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // Replace s with x for messages.
        message = Regex.Replace(message, "s+", "x");
        // Same as above, but capital letters.
        message = Regex.Replace(message, "S+", "X");

        args.Message = message;
    }
}
