using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class ShuttleRoundRestartTimerCommand : ToolshedCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [CommandImplementation("enable")]
    public void Enable([CommandInvocationContext] IInvocationContext ctx)
    {
        _cfg.SetCVar(CCVars.EnableEndOfRoundTimer, true);
        ctx.WriteLine("The round end timer will now start upon shuttle landing.");
    }

    [CommandImplementation("disable")]
    public void Disable([CommandInvocationContext] IInvocationContext ctx)
    {
        _cfg.SetCVar(CCVars.EnableEndOfRoundTimer, false);
        ctx.WriteLine("The round end timer will no longer start upon shuttle landing.");
    }
}
