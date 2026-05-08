using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;

namespace Gremlins.GremlinsCode.Vfx;

[GlobalClass]
public partial class NGremlinsMerchantCharacter : NMerchantCharacter
{
    private NSpineCreatureVisuals _angry = null!;
    private NSpineCreatureVisuals _fat = null!;
    private NSpineCreatureVisuals _shield = null!;
    private NSpineCreatureVisuals _sneak = null!;
    private NSpineCreatureVisuals _wizard = null!;
    private Node2D _body = null!;

    public override void _Ready()
    {
        _body = GetNode<Node2D>((NodePath) "%Visuals");
        _angry = _body.GetNode<NSpineCreatureVisuals>("AngryCombat");
        _fat = _body.GetNode<NSpineCreatureVisuals>("FatCombat");
        _shield = _body.GetNode<NSpineCreatureVisuals>("ShieldCombat");
        _sneak = _body.GetNode<NSpineCreatureVisuals>("SneakCombat");
        _wizard = _body.GetNode<NSpineCreatureVisuals>("WizardCombat");
        PlayAnimation(_angry, "idle", true);
        PlayAnimation(_fat, "animation", true);
        PlayAnimation(_shield, "idle", true);
        PlayAnimation(_sneak, "animation", true);
        PlayAnimation(_wizard, "animation", true);
    }

    private void PlayAnimation(NCreatureVisuals visuals, string anim, bool loop = false)
    {
        var megaTrackEntry = new MegaSprite(visuals._body).GetAnimationState().SetAnimation(anim, loop);
        if (!loop || megaTrackEntry == null)
            return;
        megaTrackEntry.SetTrackTime(megaTrackEntry.GetAnimationEnd() * Rng.Chaotic.NextFloat());
    }
}