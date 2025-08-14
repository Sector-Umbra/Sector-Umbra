using Content.Shared._Umbra.Discord;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client._Umbra.Discord;

public sealed class DiscordOAuthManager
{
    [Dependency] private readonly IClientNetManager _clientNetManager = default!;
    [Dependency] private readonly IUriOpener _uriOpener = default!;

    public void Initialize()
    {
        _clientNetManager.RegisterNetMessage<DiscordOAuthStatusMessage>(ReceivedStatus);
        _clientNetManager.RegisterNetMessage<DiscordOAuthUnlinkMessage>();
        _clientNetManager.RegisterNetMessage<DiscordOAuthStartLinkMessage>();
        _clientNetManager.RegisterNetMessage<DiscordOAuthOpenUrlMessage>(OpenUrl);
    }

    private void ReceivedStatus(DiscordOAuthStatusMessage message)
    {
        OnStatusReceived?.Invoke(message);
    }

    private void OpenUrl(DiscordOAuthOpenUrlMessage message)
    {
        _uriOpener.OpenUri(message.Url);
    }

    public event Action<DiscordOAuthStatusMessage>? OnStatusReceived;

    public void Unlink()
    {
        _clientNetManager.ClientSendMessage(new DiscordOAuthUnlinkMessage());
    }

    public void Link()
    {
        _clientNetManager.ClientSendMessage(new DiscordOAuthStartLinkMessage());
    }
}
