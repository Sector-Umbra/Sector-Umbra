using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server._Umbra.Speech.EntitySystems;

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

        // mantis noises
        message = Regex.Replace(message, "s+", "x");
        // LOUD MANTIS NOISES!!
        message = Regex.Replace(message, "S+", "X");

        args.Message = message;
    }
}
