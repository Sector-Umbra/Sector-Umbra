using System.Text.RegularExpressions;
using System.Text;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using Content.Server._Latestation.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server._Latestation.Speech.EntitySystems;

public sealed class ValleyGirlAccentSystem : EntitySystem
{
    //Words ending in -ing = in'. Bein', Darlin', etc.
    //Taken from mobster accent.
    private static readonly Regex RegexIng = new(@"(?<=\w\w)(in)g(?!\w)", RegexOptions.IgnoreCase);
    private static readonly Regex RegexLastWord = new(@"(\S+)$");
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ValleyGirlAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<ValleyGirlAccentComponent> ent, ref AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "valleygirl");

        message = RegexIng.Replace(message, "$1'");

        //Capitalizes the first letter.
        message = message[0].ToString().ToUpper() + message.Remove(0, 1);

        var suffix = "";
        // Suffixes
        if (_random.Prob(0.8f))
        {
            var pick = _random.Next(1, 6);
            suffix = Loc.GetString($"accent-valleygirl-suffix-{pick}");
        }
        message += suffix;

        args.Message = message;
    }
}