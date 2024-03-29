using Godot;
using Godot.Collections;

public partial class Game : Node
{
    private const float TILE_SIZE = 2.0f;
    public const double TICK_TIME = 1.0f;

    private Timer npcActionTick;
    [Signal] public delegate void TickEventHandler();
    [Signal] public delegate void DebugLogEventHandler();
    public string allLogText = "DungeonCrawler - Top Secret Messages\r\n--------------------------------";

    public enum Character
    {
        Cyclops
    }

    public enum InteractableArea
    {
        None,
        SwordPickupArea3D,
        HitboxArea3D,
    }

    public enum Item
    {
        None,
        Sword,
        Potion
    }

    public Tween HandleMoveTween(Node3D mover, Vector3 direction, float speed)
    {
        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(mover, "position", mover.Position + direction.Rotated(Vector3.Up, mover.Rotation.Y) * TILE_SIZE, speed);
        tween.Play();
        return tween;
    }

    public Tween HandleRotateTween(Node3D rotater, int shift, float speed)
    {
        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(rotater, "rotation:y", rotater.Rotation.Y + shift * (Mathf.Pi / 2.0), speed);
        tween.Play();
        return tween;
    }

    public override void _Ready()
    {
        npcActionTick = new Timer
        {
            Autostart = true,
            WaitTime = TICK_TIME
        };
        npcActionTick.Timeout += () => EmitSignal(SignalName.Tick);
        AddChild(npcActionTick);
        base._Ready();
    }

    public enum Scene
    {
        main_menu,
        outside
    }

    public void ChangeScene(Scene scene)
    {
        GetTree().ChangeSceneToFile($"res://Scenes/{scene}.tscn");
    }

    public void Log(string msg)
    {
        Dictionary time = Time.GetTimeDictFromSystem();
        var formattedLogText = $"{System.Environment.NewLine}{time["hour"].ToString().PadLeft(2, '0')}:{time["minute"].ToString().PadLeft(2, '0')}:{time["second"].ToString().PadLeft(2, '0')}| {msg}";
        allLogText += formattedLogText;
        EmitSignal(SignalName.DebugLog);
        GD.Print(formattedLogText);
    }
}