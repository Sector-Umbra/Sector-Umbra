using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class ReplaysCommand : ToolshedCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [CommandImplementation("enable")]
    public void Enable([CommandInvocationContext] IInvocationContext ctx)
    {
        _cfg.SetCVar(CCVars.ReplayAutoRecord, true);
        ctx.WriteLine("Replays will now be recorded");
    }

    [CommandImplementation("disable")]
    public void Disable([CommandInvocationContext] IInvocationContext ctx)
    {
        _cfg.SetCVar(CCVars.ReplayAutoRecord, false);
        ctx.WriteLine("Replays will no longer be recorded");
    }
}
