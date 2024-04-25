using Content.Client.Eui;
using Content.Shared._Umbra.SiliconLaws;
using Content.Shared.Eui;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Umbra.SiliconLawUi;

public sealed class SiliconLawEui : BaseEui
{
    public readonly EntityManager _entityManager = default!;

    private SiliconLawUi _siliconLawUi;
    private EntityUid _target;

    public SiliconLawEui()
    {
        _entityManager = IoCManager.Resolve<EntityManager>();

        _siliconLawUi = new SiliconLawUi();
        _siliconLawUi.OnClose += () => SendMessage(new CloseEuiMessage());
        _siliconLawUi.Save.OnPressed += _ => SendMessage(new SiliconLawsSaveMessage(_siliconLawUi.GetLaws(), _entityManager.GetNetEntity(_target)));
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not SiliconLawsEuiState s)
        {
            return;
        }

        _target = _entityManager.GetEntity(s.Target);
        _siliconLawUi.SetLaws(s.Laws);
    }

    public override void Opened()
    {
        _siliconLawUi.OpenCentered();
    }
}
