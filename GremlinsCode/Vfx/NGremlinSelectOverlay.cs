using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace Gremlins.GremlinsCode.Vfx;

public partial class NGremlinSelectOverlay : Control, IOverlayScreen
{
    private readonly List<(NCreature node, Node originalParent, int originalIndex)> _gremlinNodes = [];
    private readonly TaskCompletionSource<int> _tcs = new();
    private int _controllerIndex;
    private List<Creature> _gremlins = [];

    public Control? DefaultFocusedControl => null;
    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => false;

    public void AfterOverlayClosed()
    {
        foreach (var (node, originalParent, originalIndex) in _gremlinNodes)
        {
            if (!IsInstanceValid(node) || !IsInstanceValid(originalParent)) continue;
            var globalPos = node.GlobalPosition;
            RemoveChild(node);
            originalParent.AddChild(node);
            originalParent.MoveChild(node, originalIndex);
            node.GlobalPosition = globalPos;
            node.Scale = Vector2.One; // reset scale
        }

        QueueFree();
    }

    public void AfterOverlayOpened()
    {
        Modulate = Colors.Transparent;
        CreateTween().TweenProperty(this, "modulate:a", 1f, 0.2);
    }

    public void AfterOverlayShown()
    {
        Visible = true;
    }

    public void AfterOverlayHidden()
    {
        Visible = false;
    }

    public static NGremlinSelectOverlay Create(List<Creature> gremlins)
    {
        var overlay = new NGremlinSelectOverlay();
        overlay._gremlins = gremlins;
        return overlay;
    }

    public override void _Ready()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        Size = viewportSize;
        Position = Vector2.Zero;
        MouseFilter = MouseFilterEnum.Stop;

        var dim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.6f),
            Size = viewportSize,
            Position = Vector2.Zero,
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(dim);

        foreach (var gremlin in _gremlins)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(gremlin);
            if (node == null) continue;

            var originalParent = node.GetParent();
            var originalIndex = node.GetIndex();
            var globalPos = node.GlobalPosition;

            originalParent.RemoveChild(node);
            AddChild(node);
            node.GlobalPosition = globalPos;

            _gremlinNodes.Add((node, originalParent, originalIndex));

            var captured = _gremlinNodes.Count - 1;
            var btn = new Button
            {
                Flat = true,
                Size = node.Hitbox.Size,
                GlobalPosition = node.Hitbox.GlobalPosition,
                Modulate = Colors.Transparent,
                MouseFilter = MouseFilterEnum.Stop
            };
            AddChild(btn);
            btn.Pressed += () => _tcs.TrySetResult(captured);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_tcs.Task.IsCompleted) return;

        if (@event.IsActionPressed(MegaInput.left))
        {
            _controllerIndex = (_controllerIndex - 1 + _gremlinNodes.Count) % _gremlinNodes.Count;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(MegaInput.right))
        {
            _controllerIndex = (_controllerIndex + 1) % _gremlinNodes.Count;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(MegaInput.select))
        {
            GetViewport().SetInputAsHandled();
            _tcs.TrySetResult(_controllerIndex);
        }
    }

    public override void _Process(double delta)
    {
        for (var i = 0; i < _gremlinNodes.Count; i++)
        {
            var (node, _, _) = _gremlinNodes[i];
            if (!IsInstanceValid(node)) continue;
            node.Scale = i == _controllerIndex ? new Vector2(1.1f, 1.1f) : Vector2.One;
        }
    }

    public Task<int> AwaitSelection()
    {
        return _tcs.Task;
    }
}