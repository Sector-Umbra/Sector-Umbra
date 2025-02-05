using System.Text;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class MantidaeAccentSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<MantidaeAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            // Insert E before every S
            message = InsertS(message);
            return message;
        }

        private string InsertS(string message)
        {
            // Replace every new Word that starts with s/S
            var msg = message.Replace(" s", " x").Replace(" S", " X");

            // Still need to check if the beginning of the message starts
            if (msg.StartsWith("s", StringComparison.Ordinal))
            {
                return msg.Remove(0, 1).Insert(0, "x");
            }
            else if (msg.StartsWith("S", StringComparison.Ordinal))
            {
                return msg.Remove(0, 1).Insert(0, "X");
            }

            return msg;
        }

        private void OnAccent(EntityUid uid, MantidaeAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
