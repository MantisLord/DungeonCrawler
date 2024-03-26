using Godot;
using System.Diagnostics.CodeAnalysis;
using static Godot.TextServer;

public partial class Player : Node3D
{
    public const int TILE_SIZE = 2;

    private Game game;

    private Label info;

    private RayCast3D forwardRay;
    private RayCast3D backRay;
    private RayCast3D leftRay;
    private RayCast3D rightRay;


    private const float moveLerpModifier = 0.3f;
    private float t = 0;

    public bool tookActionThisTick = false;
    Action queuedAction = Action.None;
    Vector3 lerpTarget = Vector3.Zero;

    private float targetYRotation = 0;

    private Tween tween;

    public enum Action
    {
        None,
        Forward,
        Back,
        StrafeLeft,
        StrafeRight,
        TurnLeft,
        TurnRight,
    }

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        game.Tick += () => tookActionThisTick = false;
        forwardRay = GetNode<RayCast3D>("ForwardRayCast3D");
        backRay = GetNode<RayCast3D>("BackRayCast3D");
        leftRay = GetNode<RayCast3D>("LeftRayCast3D");
        rightRay = GetNode<RayCast3D>("RightRayCast3D");
        info = GetNode<Label>("InfoLabel");
        tookActionThisTick = false;
        base._Ready();
    }

    private void HandleMoveTween(Vector3 direction)
    {
        tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "position", Position + direction.Rotated(Vector3.Up, Rotation.Y) * 2.0f, moveLerpModifier);
        tween.Play();
    }

    private void HandleRotateTween(bool left)
    {
        var shift = left ? (Mathf.Pi / 2.0) : -(Mathf.Pi / 2.0);
        tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "rotation:y", Rotation.Y + shift, moveLerpModifier);
        tween.Play();
    }

    public override void _Process(double delta)
    {
        
        info.Text = $"Position (X: {System.Math.Round(Position.X,2)}, Y: {System.Math.Round(Position.Y, 2)}, Z: {System.Math.Round(Position.Z,2)})\nRotationDegrees {Rotation.Y}";

        //if (lerpTarget != Vector3.Zero)
        //    return;
        if (tween != null && tween.IsRunning())
            return;
        if (Input.IsActionJustPressed("forward") && !forwardRay.IsColliding())
        {
            HandleMoveTween(Vector3.Forward);
        }
        if (Input.IsActionJustPressed("back") && !backRay.IsColliding())
        {
            HandleMoveTween(Vector3.Back);
        }
        if (Input.IsActionJustPressed("strafe_right") && !rightRay.IsColliding())
        {
            HandleMoveTween(Vector3.Right);
        }
        if (Input.IsActionJustPressed("strafe_left") && !leftRay.IsColliding())
        {
            HandleMoveTween(Vector3.Left);
        }
        if (Input.IsActionJustPressed("turn_left"))
        {
            HandleRotateTween(true);
        }
        if (Input.IsActionJustPressed("turn_right"))
        {
            HandleRotateTween(false);
        }

        //if (!tookActionThisTick)
        //{
        //    if (queuedAction != Action.None)
        //    {
        //        HandleAction(queuedAction);
        //        queuedAction = nextAction;
        //    }
        //    else
        //    {
        //        HandleAction(nextAction);
        //    }
        //}
        //else if (nextAction != Action.None)
        //    queuedAction = nextAction;

        base._Process(delta);
    }
}
