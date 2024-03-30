using Godot;
using Godot.Collections;

public partial class Game : Node
{
    private const float TILE_SIZE = 2.0f;
    private const double TICK_TIME = 1.0f;
    private Timer npcActionTick;

    [Signal] public delegate void TickEventHandler();
    [Signal] public delegate void DisplayLogEventHandler();
    public string allLogText = "DungeonCrawler - Top Secret Messages\r\n--------------------------------";

    public bool gameOver = false;

    // menu settings
    public bool debugMode = true;
    public float fov = 75;
    public bool teleportMode = false;

    public static float NormalizeDegrees(float input)
    {
        float degrees = (input * 180.0f) / Mathf.Pi;
        degrees = (degrees + 360) % 360; // Ensure positive angle within 0-360 range
        degrees = degrees < 0 ? 360 + degrees : degrees; // Adjust if negative
        return degrees;
    }

    public static string GetCardinalDirectionFromNormalizedDegrees(float degrees)
    {
        string cardinalDirection;
        if ((degrees >= 0 && degrees < 45) || (degrees >= 315 && degrees <= 360))
        {
            cardinalDirection = "North";
        }
        else if (degrees >= 45 && degrees < 135)
        {
            cardinalDirection = "West";
        }
        else if (degrees >= 135 && degrees < 225)
        {
            cardinalDirection = "South";
        }
        else
        {
            cardinalDirection = "East";
        }
        return cardinalDirection;
    }

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
        if (teleportMode)
            mover.Translate(direction * TILE_SIZE);
        else
            tween.Play();
        return tween;
    }

    public Tween HandleRotateTween(Node3D rotater, int shift, float speed)
    {
        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        var finalRot = rotater.Rotation.Y + shift * (Mathf.Pi / 2.0);
        tween.TweenProperty(rotater, "rotation:y", finalRot, speed);
        if (teleportMode)
        {
            Vector3 newRot = new(0, (float)finalRot, 0);
            rotater.Rotation = newRot;
        }
        else
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

    public void Log(string msg, bool debugOnly = false)
    {
        if (!debugMode && debugOnly)
        {
            GD.Print("Would have displayed something useful, but it was hidden from the player because debug mode is turned off.");
            GD.Print(msg);
            return;
        }
        Dictionary time = Time.GetTimeDictFromSystem();
        var formattedLogText = $"{System.Environment.NewLine}{time["hour"].ToString().PadLeft(2, '0')}:{time["minute"].ToString().PadLeft(2, '0')}:{time["second"].ToString().PadLeft(2, '0')}| {msg}";
        allLogText += formattedLogText;
        EmitSignal(SignalName.DisplayLog);
        GD.Print(formattedLogText);
    }
}