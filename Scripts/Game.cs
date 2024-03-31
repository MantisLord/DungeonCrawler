using Godot;
using Godot.Collections;
using static AudioManager;

public partial class Game : Node
{
    private AudioManager audioMgr;

    private const float TILE_SIZE = 2.0f;
    private const double TICK_TIME = 1.0f;
    private const int TIME_TO_ESCAPE = 120;
    private const string LOG_DEFAULT_TEXT = "Cyclopes - Top Secret Messages\r\n--------------------------------";
    private Timer npcActionTick;

    public Timer timeToEscape;
    public bool timerStarted;

    [Signal] public delegate void TickEventHandler();
    [Signal] public delegate void DisplayLogEventHandler();
    [Signal] public delegate void EscapeTimeoutEventHandler();
    public string allLogText = LOG_DEFAULT_TEXT;

    public Scene currentScene = Scene.main_menu;
    public bool gameOver = false;
    public bool returningFromDungeon = false;

    public int health = 100;
    public bool foundItem1 = false;
    public bool foundItem2 = false;
    public bool foundItem3 = false;

    // menu settings
    public bool debugMode = false;
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
        SceneTransitionArea3D,
        TorchPickupArea3D,
        AmphoraPickupArea3D,
        SpearPickupArea3D,
        EscapeArea3D,
    }

    public enum Item
    {
        None,
        Sword,
        Amphora,
        Torch,
    }
    
    public void Restart()
    {
        timeToEscape.Stop();
        timeToEscape.WaitTime = TIME_TO_ESCAPE;
        timerStarted = false;
        allLogText = LOG_DEFAULT_TEXT;
        foundItem1 = false;
        foundItem2 = false;
        foundItem3 = false;
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
        audioMgr = GetNode<AudioManager>("/root/AudioManager");
        npcActionTick = new Timer
        {
            Autostart = true,
            WaitTime = TICK_TIME
        };
        timeToEscape = new Timer
        {
            Autostart = false,
            WaitTime = TIME_TO_ESCAPE,
        };
        npcActionTick.Timeout += () => EmitSignal(SignalName.Tick);
        timeToEscape.Timeout += () => EmitSignal(SignalName.EscapeTimeout);
        AddChild(npcActionTick);
        AddChild(timeToEscape);
        base._Ready();
    }

    public enum Scene
    {
        main_menu,
        outside,
        dungeon
    }

    public void ChangeScene(Scene scene)
    {
        GetTree().ChangeSceneToFile($"res://Scenes/{scene}.tscn");
        currentScene = scene;
        
        switch (scene)
        {
            case Scene.outside:
                if (!timerStarted)
                {
                    audioMgr.StopMusic();
                }
                audioMgr.Play(Audio.AmbienceOutside, AudioChannel.Ambient);
                break;
            case Scene.dungeon:
                audioMgr.StopAmbience();
                if (!timerStarted)
                {
                    audioMgr.Play(Audio.MusicDungeon, AudioChannel.Music);
                }
                break;
        }
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