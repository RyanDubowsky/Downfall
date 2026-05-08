using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Gremlins.GremlinsCode.Vfx;

[GlobalClass]
public partial class NGremlinsCreatureVisuals : NCreatureVisuals
{
    private List<Creature> _gremlins = [];
    private int ActiveGremlinIndex { get; set; }

    public void ArrangeGremlins(List<Creature> gremlins)
    {
        _gremlins = gremlins;
        ApplySlotPositions(false);

        foreach (var node in _gremlins.Select(g => NCombatRoom.Instance?.GetCreatureNode(g)))
            node?.ToggleIsInteractable(true);

        HideGremlinBar(ActiveGremlinIndex);

        // Defer so pet NCreature nodes finish their own _Ready/UpdateBounds first
        SyncBoundsToActive();
    }
    
    
    public void ReviveGremlin(int index)
    {
        if (index < 0 || index >= _gremlins.Count) return;
        var node = NCombatRoom.Instance?.GetCreatureNode(_gremlins[index]);
        if (node == null) return;

        node.Visible = true;
        var tween = node.CreateTween().SetParallel();
        tween.TweenProperty(node, "modulate", Colors.White, 0.4)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "scale", Vector2.One * GetSlotScale(GetSlot(index)), 0.4)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
    }
    
    private void HideGremlinBar(int index)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(_gremlins[index]);
        node?.GetNode<Control>("%HealthBar")
            ?.GetNode<NHealthBar>("%HealthBar")
            ?.HpBarContainer.Hide();
    }
    
    private void ShowGremlinBar(int index)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(_gremlins[index]);
        node?.GetNode<Control>("%HealthBar")
            ?.GetNode<NHealthBar>("%HealthBar")
            ?.HpBarContainer.Show();
    }

    public void SwitchToGremlin(int index)
    {
        if (index < 0 || index >= _gremlins.Count) return;
        ShowGremlinBar(ActiveGremlinIndex);
        ActiveGremlinIndex = index;
        ApplySlotPositions(true);
        HideGremlinBar(ActiveGremlinIndex);
        SyncBoundsToActive();
    }
    
    private void SyncBoundsToActive()
    {
        var playerNode = GetParent<NCreature>();
        if (playerNode == null || _gremlins.Count == 0) return;

        var activeNode = NCombatRoom.Instance?.GetCreatureNode(_gremlins[ActiveGremlinIndex]);
        if (activeNode == null) return;

        //playerNode.Position = activeNode.Position;

        //var size      = activeNode.Hitbox.Size with { X = activeNode.Hitbox.Size.X * 2f };
        //var globalPos = activeNode.Hitbox.GlobalPosition;

        //playerNode.Hitbox.Size           = size;
        //playerNode.Hitbox.GlobalPosition = globalPos;

        /*var reticle = playerNode.GetNodeOrNull<Control>("%SelectionReticle");
        if (reticle != null)
        {
            reticle.Size           = size;
            reticle.GlobalPosition = globalPos;
            reticle.PivotOffset    = size * 0.5f;
        }*/

        //playerNode.IntentContainer.Position = activeNode.IntentContainer.Position;

        var stateDisplay = playerNode.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar");
        stateDisplay?.SetCreatureBounds(playerNode.Hitbox);
    }
    
    public void KillGremlin(int index)
    {
        if (index < 0 || index >= _gremlins.Count) return;
        var node = NCombatRoom.Instance?.GetCreatureNode(_gremlins[index]);
        if (node == null) return;

        var tween = node.CreateTween().SetParallel();
        tween.TweenProperty(node, "modulate", new Color(0, 0, 0, 0), 0.4)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "scale", Vector2.Zero, 0.4)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        tween.Chain().TweenCallback(Callable.From(() => node.Visible = false));
    }

    private int GetSlot(int gremlinIndex)
    {
        var count = _gremlins.Count;
        return (gremlinIndex - ActiveGremlinIndex + count) % count;
    }

    private static Vector2 GetSlotOffset(int slot)
    {
        return slot == 0 ? Vector2.Zero : new Vector2(-120f - (slot - 1) * 60f, 0f);
    }

    private static float GetSlotScale(int slot)
    {
        return slot == 0 ? 1f : 0.6f;
    }

    private void ApplySlotPositions(bool animated)
    {
        var anchor = GetParent<NCreature>()?.Position ?? GlobalPosition;

        // Build ordered list of living gremlins starting from active
        var living = _gremlins
            .Select((g, i) => (gremlin: g, index: i))
            .Where(x => x.gremlin.IsAlive)
            .OrderBy(x => (x.index - ActiveGremlinIndex + _gremlins.Count) % _gremlins.Count)
            .ToList();

        for (var slot = 0; slot < living.Count; slot++)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(living[slot].gremlin);
            if (node == null) continue;

            var targetPos = anchor + GetSlotOffset(slot);
            var targetScale = Vector2.One * GetSlotScale(slot);
            node.ZIndex = living.Count - slot;

            if (animated)
            {
                var tween = node.CreateTween().SetParallel();
                tween.TweenProperty(node, "position", targetPos, 0.3)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
                tween.TweenProperty(node, "scale", targetScale, 0.3)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

                // Only re-sync after the active gremlin (slot 0) finishes moving
                if (slot == 0)
                    tween.Chain().TweenCallback(Callable.From(SyncBoundsToActive));
            }
            else
            {
                node.Position = targetPos;
                node.Scale = targetScale;
            }
        }
    }
    
}