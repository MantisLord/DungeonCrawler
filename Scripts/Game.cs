using Godot;

public partial class Game : Node
{
    public const double TICK_TIME = 0.5;
    private Timer moveLimitTick;
    [Signal] public delegate void TickEventHandler();
    public override void _Ready()
    {
        moveLimitTick = new Timer();
        moveLimitTick.Autostart = true;
        moveLimitTick.WaitTime = TICK_TIME;
        moveLimitTick.Timeout += () => EmitSignal(SignalName.Tick);
        AddChild(moveLimitTick);
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
}