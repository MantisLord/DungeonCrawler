using Godot;
using System;
using System.Collections.Generic;
using static Game;

public partial class NPC : Node3D
{
    [Export] public int maxHealth = 50;
    [Export] public int currentHealth = 50;
    [Export] public string name = "Cyclops";
    [Export] public Character character = Character.Cyclops;
    [Export] public float moveSpeed = 0.5f;
    [Export] public bool wander = true;
    [Export] public MovementType movementType = MovementType.Trailblazer;
    [Export] public bool randomIdle = true;
    [Export] public int idleTimeMin = 8; // only impacts initial idle after spawn if randomIdle = false
    [Export] public int idleTimeMax = 16;
    [Export] public int wanderTimeMin = 30; // how long before potentially going idle again if both wander = true and randomIdle = true
    [Export] public int wanderTimeMax = 60;
    [Export] public double visionCheckTime = 0.25;
    [Export] public float attackRange = 3.5f;
    [Export] public bool hostile = true;
    [Export] public int damageMin = 5;
    [Export] public int damageMax = 10;

    private Game game;
    private AnimationPlayer anim;
    private RandomNumberGenerator rand = new();
    private Animation currentAnim = Animation.TPose; // needed?
    private State currentState = State.None;
    private bool tookActionThisTick = false;
    private Tween tween;
    private RayCast3D forwardRay;
    private RayCast3D backRay;
    private RayCast3D leftRay;
    private RayCast3D rightRay;
    private Timer stateEndTimer;
    private GpuParticles3D bloodInstance = new();
    private PackedScene blood = GD.Load<PackedScene>("res://Scenes/blood_particles.tscn");

    // LoS system
    private Area3D visionArea;
    private RayCast3D visionRayCast;
    private Timer visionCheckTimer;

    private NavigationAgent3D navAgent;
    private Node3D spottedTarget;
    private Vector3 nextFollowMovePoint;
    private Node3D followTarget;
    private Label3D debugLabel;

    private AudioStreamPlayer3D stepAudio;
    private AudioStreamPlayer3D attackAudio;
    private AudioStreamPlayer3D receiveHitAudio;
    private AudioStreamPlayer3D deathAudio;
    private AudioStreamPlayer3D aggroAudio;

    private MeshInstance3D visionDebugMesh;

    private MeshInstance3D cyclopsMesh1;
    private MeshInstance3D cyclopsMesh2;
    private MeshInstance3D cyclopsMesh3;
    private MeshInstance3D cyclopsMesh4;
    private MeshInstance3D cyclopsMesh5;
    private MeshInstance3D cyclopsMesh6;

    private CollisionShape3D collision;
    private StaticBody3D staticBody;

    public enum Animation
    {
        // Cyclops
        TPose,
        Idle,
        IdleLook,
        IdleSitDown,
        Walk,
        Run, // Walk Speed*2?
        AttackBite,
        AttackOverhead,
        AttackSingle,
        AttackTriple,
        HitReactionLeft,
        HitReactionRight,
        Death,
    }

    private void PlayAnimForCurrentState()
    {
        Animation newAnimation = Animation.TPose;
        float speed = 1.0f;
        double blend = 0.2f;
        switch (currentState)
        {
            case State.Idle:
                switch (rand.RandiRange(0, 2))
                {
                    case 0: newAnimation = Animation.Idle; break;
                    case 1: newAnimation = Animation.IdleLook; break;
                    case 2: newAnimation = Animation.IdleSitDown; break;
                }
                break;
            case State.Dead:
                newAnimation = Animation.Death;
                break;
            case State.Chase:
                switch (character)
                {
                    case Character.Cyclops:
                        newAnimation = Animation.Walk;
                        speed = 2.0f;
                        break;
                    default:
                        newAnimation = Animation.Run;
                        break;
                }
                break;
            case State.Wander:
                newAnimation = Animation.Walk;
                break;
            case State.Attack:
                switch (rand.RandiRange(0, 3))
                {
                    case 0: newAnimation = Animation.AttackSingle; break;
                    case 1: newAnimation = Animation.AttackBite; break;
                    case 2: newAnimation = Animation.AttackOverhead; break;
                    case 3: newAnimation = Animation.AttackTriple; break;
                }
                break;
        }
        anim.Play($"{newAnimation}", blend, speed);
        game.Log($"{name} playing anim {newAnimation}.", true);
        currentAnim = newAnimation;
    }

    public enum State
    {
        None,
        Idle,
        Wander,
        Chase,
        Attack,
        Dead
    }

    public enum MoveAction
    {
        Forward,
        RotateLeft,
        RotateRight,
        Back,
        StrafeLeft,
        StrafeRight,
    }

    public enum MovementType
    {
        None,
        Patrol,         // straight lines, only turning when necessary
        Trailblazer,    // 75% forward, 25% turn
        Chaotic,        // all random
        Follow
    }

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        anim = GetNode<AnimationPlayer>("AnimationPlayer");
        game.Tick += () => tookActionThisTick = false;
        stepAudio = GetNode<AudioStreamPlayer3D>("StepAudioStreamPlayer3D");
        receiveHitAudio = GetNode<AudioStreamPlayer3D>("ReceiveHitAudioStreamPlayer3D");
        attackAudio = GetNode<AudioStreamPlayer3D>("AttackAudioStreamPlayer3D");
        deathAudio = GetNode<AudioStreamPlayer3D>("DeathAudioStreamPlayer3D");
        aggroAudio = GetNode<AudioStreamPlayer3D>("AggroAudioStreamPlayer3D");

        forwardRay = GetNode<RayCast3D>("ForwardRayCast3D");
        backRay = GetNode<RayCast3D>("BackRayCast3D");
        leftRay = GetNode<RayCast3D>("LeftRayCast3D");
        rightRay = GetNode<RayCast3D>("RightRayCast3D");

        debugLabel = GetNode<Label3D>("DebugLabel3D");

        navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        visionArea = GetNode<Area3D>("Joints/Skeleton3D/Eye/VisionArea3D");
        visionRayCast = GetNode<RayCast3D>("StaticBody3D/VisionRayCast3D");
        visionDebugMesh = visionArea.GetNode<MeshInstance3D>("VisionMeshInstance3D");

        cyclopsMesh1 = GetNode<MeshInstance3D>("Joints/Skeleton3D/n1_low");
        cyclopsMesh2 = GetNode<MeshInstance3D>("Joints/Skeleton3D/n2_low");
        cyclopsMesh3 = GetNode<MeshInstance3D>("Joints/Skeleton3D/n3_low");
        cyclopsMesh4 = GetNode<MeshInstance3D>("Joints/Skeleton3D/n4_low");
        cyclopsMesh5 = GetNode<MeshInstance3D>("Joints/Skeleton3D/n5_low");
        cyclopsMesh6 = GetNode<MeshInstance3D>("Joints/Skeleton3D/n6_low");

        collision = GetNode<CollisionShape3D>("StaticBody3D/CollisionShape3D");
        staticBody = GetNode<StaticBody3D>("StaticBody3D");

        rand.Randomize();

        RandomizeSkin();

        stateEndTimer = new()
        {
            OneShot = true
        };
        stateEndTimer.Timeout += HandleStateEndTimerTimeout;
        visionCheckTimer = new()
        {
            Autostart = true,
            OneShot = false,
            WaitTime = visionCheckTime
        };
        visionCheckTimer.Timeout += HandleVisionCheckTimerTimeout;
        AddChild(stateEndTimer);
        AddChild(visionCheckTimer);
    }

    private void RandomizeSkin()
    {
        if (rand.RandiRange(0, 1) == 1)
        {
            string path = "res://Assets/Models/Cyclops/cyclops2.tres";
            var newSkinMaterial = ResourceLoader.Load(path, "StandardMaterial3D") as StandardMaterial3D;
            cyclopsMesh1.SetSurfaceOverrideMaterial(0, newSkinMaterial);
            cyclopsMesh2.SetSurfaceOverrideMaterial(0, newSkinMaterial);
            cyclopsMesh3.SetSurfaceOverrideMaterial(0, newSkinMaterial);
            cyclopsMesh4.SetSurfaceOverrideMaterial(0, newSkinMaterial);
            cyclopsMesh5.SetSurfaceOverrideMaterial(0, newSkinMaterial);
            cyclopsMesh6.SetSurfaceOverrideMaterial(0, newSkinMaterial);
        }
    }

    private bool CanPerformMoveAction(MoveAction action)
    {
        bool ret = false;
        switch (action)
        {
            case MoveAction.Forward:
                if (!forwardRay.IsColliding())
                    ret = true;
                break;
            case MoveAction.Back:
                if (!backRay.IsColliding())
                    ret = true;
                break;
            case MoveAction.StrafeRight:
                if (!rightRay.IsColliding())
                    ret = true;
                break;
            case MoveAction.StrafeLeft:
                if (!leftRay.IsColliding())
                    ret = true;
                break;
            case MoveAction.RotateLeft:
            case MoveAction.RotateRight:
                ret = true;
                break;
        }
        return ret;
    }

    private MoveAction GetFirstUnblockedMove()
    {
        MoveAction action = rand.RandiRange(0,1) == 1 ? MoveAction.RotateLeft : MoveAction.RotateRight;
        if (!forwardRay.IsColliding())
        {
            action = MoveAction.Forward;
        }
        if (character == Character.Cyclops) // maybe only other humanoid characters can backpedal/strafe?
            return action;

        if (!backRay.IsColliding())
        {
            action = MoveAction.Back;
        }
        if (!rightRay.IsColliding())
        {
            action = MoveAction.StrafeRight;
        }
        if (!leftRay.IsColliding())
        {
            action = MoveAction.StrafeLeft;
        }
        return action;
    }

    void TryMoveStep()
    {
        if (tween != null && tween.IsRunning())
            return;
        if (movementType == MovementType.None)
        {
            return;
        }

        Array values = Enum.GetValues(typeof(MoveAction));
        List<MoveAction> availMoveActions = new List<MoveAction>();
        availMoveActions.AddRange((IEnumerable<MoveAction>)values);
        switch (character)
        {
            case Character.Cyclops:
                availMoveActions.Remove(MoveAction.Back);
                availMoveActions.Remove(MoveAction.StrafeRight);
                availMoveActions.Remove(MoveAction.StrafeLeft);
                break;
        }

        MoveAction nextMove = MoveAction.Forward;
        switch (movementType)
        {
            case MovementType.Follow:
                // find direction of the nextFollowMovePoint
                // find closest 90' rotation to direction
                // rotate if not matching closest 90' rotation
                // forward if matching closest 90' rotation

                float degrees = NormalizeDegrees(Rotation.Y);
                string cardinal = GetCardinalDirectionFromNormalizedDegrees(degrees);

                float angleTo = Position.AngleTo(nextFollowMovePoint);
                visionRayCast.LookAt(nextFollowMovePoint);
                visionRayCast.ForceRaycastUpdate();

                float calcDegrees = NormalizeDegrees(visionRayCast.Rotation.Y);
                string calcCardinal = GetCardinalDirectionFromNormalizedDegrees(calcDegrees);

                float bufferAmount = 44.9f;

                //     360
                //90    o    270
                //     180
                if (calcDegrees >= 180 && calcDegrees <= 270 + bufferAmount)
                    nextMove = MoveAction.RotateRight;
                else if (calcDegrees >= 90 - bufferAmount && calcDegrees < 180)
                    nextMove = MoveAction.RotateLeft;
                else
                    nextMove = MoveAction.Forward;

                debugLabel.Text = $"{name}" +
                    $"\nDirectionTo: {Position.DirectionTo(nextFollowMovePoint)}" +
                    $"\nDegrees: {degrees}" +
                    $"\nFacing: {cardinal}" +
                    $"\nAngleTo: {angleTo}" +
                    $"\nCalcFace: {calcCardinal}" +
                    $"\nCalcDegrees: {calcDegrees}" +
                    $"\nNextMove: {nextMove}" + 
                    $"\nDistanceTo: {Position.DistanceTo(nextFollowMovePoint)}";

                break;
            case MovementType.Chaotic:
                nextMove = availMoveActions[rand.RandiRange(0, availMoveActions.Count)];
                break;
            case MovementType.Patrol:
                nextMove = MoveAction.Forward;
                break;
            case MovementType.Trailblazer:
                int chance = rand.RandiRange(0, 100);
                if (chance <= 75)
                {
                    nextMove = MoveAction.Forward;
                }
                else
                {
                    nextMove = rand.RandiRange(0, 1) == 1 ? MoveAction.RotateLeft : MoveAction.RotateRight;
                }
                break;
        }

        if (!CanPerformMoveAction(nextMove))
        {
            nextMove = GetFirstUnblockedMove();
        }
        HandleMoveAction(nextMove);
    }

    void HandleMoveAction(MoveAction action)
    {
        switch (action)
        {
            case MoveAction.Forward:
                tween = game.HandleMoveTween(this, Vector3.Forward, moveSpeed);
                stepAudio.Play();
                break;
            case MoveAction.Back:
                tween = game.HandleMoveTween(this, Vector3.Back, moveSpeed);
                stepAudio.Play();
                break;
            case MoveAction.StrafeLeft:
                tween = game.HandleMoveTween(this, Vector3.Left, moveSpeed);
                stepAudio.Play();
                break;
            case MoveAction.StrafeRight:
                tween = game.HandleMoveTween(this, Vector3.Right, moveSpeed);
                stepAudio.Play();
                break;
            case MoveAction.RotateLeft:
                tween = game.HandleRotateTween(this, 1, moveSpeed);
                break;
            case MoveAction.RotateRight:
                tween = game.HandleRotateTween(this, -1, moveSpeed);
                break;
        }
    }

    private void HandleStateEndTimerTimeout()
    {
        switch (currentState)
        {
            case State.Idle:
            case State.Wander:
                if (wander && randomIdle)
                {
                    SetState(randomIdle ? rand.RandiRange(0, 1) == 1 ? State.Wander : State.Idle : State.Wander);
                }
                else
                {
                    SetState(wander ? State.Wander : State.Idle);
                }
                break;
        }
    }

    private void SetState(State newState)
    {
        switch (newState)
        {
            case State.Idle:
                stateEndTimer.WaitTime = rand.RandiRange(idleTimeMin, idleTimeMax);
                stateEndTimer.Start();
                game.Log($"{name} idling for {stateEndTimer.TimeLeft} secs.", true);
                break;
            case State.Wander:
                stateEndTimer.WaitTime = rand.RandiRange(wanderTimeMin, wanderTimeMax);
                stateEndTimer.Start();
                game.Log($"{name} wandering for {stateEndTimer.TimeLeft} secs.", true);
                break;
            case State.Dead:
                game.Log($"{name} died.");
                break;
            case State.Chase:
                game.Log($"{name} started chasing {spottedTarget.Name}.", true);
                followTarget = spottedTarget; // store here because spottedTarget can be changed/lost during chase or follow
                movementType = MovementType.Follow;
                break;
            case State.Attack:
                game.Log($"{name} started attack on {followTarget.Name}.", true);
                break;
        }
        currentState = newState;
        PlayAnimForCurrentState();
    }

    public void TriggerHitEmission(Vector3 point)
    {
        bloodInstance = blood.Instantiate<GpuParticles3D>();
        bloodInstance.GlobalPosition = point;
        bloodInstance.Emitting = true;
        GetTree().Root.AddChild(bloodInstance);
    }

    public void ReceiveHit(Node3D attacker, Area3D hitArea, int damageDealt)
    {
        if (currentState == State.Dead)
            return;

        if (currentState != State.Chase && currentState != State.Attack)
        {
            game.Log($"You performed a critical sneak attack on {name}'s {hitArea.GetParent().Name}!");
            currentHealth = 0;
        }
        else
        {
            currentHealth -= damageDealt;
            game.Log($"{attacker.Name} hit {name}'s {hitArea.GetParent().Name} and dealt {damageDealt} damage! {name} has {currentHealth} HP remaining.");
        }
        if (currentHealth <= 0)
        {
            game.cyclopsKilled++;
            SetState(State.Dead);
            deathAudio.Play();
            collision.Disabled = true;
            staticBody.CollisionLayer = 69;
            staticBody.Visible = false;
        }
        else
        {
            receiveHitAudio.Play();
        }
    }

    public bool FoundTarget()
    {
        bool foundTarget = false;
        var overlaps = visionArea.GetOverlappingBodies();
        if (overlaps.Count > 0)
        {
            foreach (var overlap in overlaps)
            {
                var overlapParent = overlap.GetParent();

                if (overlap.Name == "Player" || overlapParent.Name == "Player" || overlapParent.GetParent().Name == "Player")
                    game.Log($"I SEE YOU - PT1", true);

                if (overlapParent.GetType() == typeof(Player))
                {
                    var playerPosition = overlap.GlobalTransform.Origin;
                    visionRayCast.LookAt(playerPosition, Vector3.Up);
                    visionRayCast.ForceRaycastUpdate();

                    if (visionRayCast.IsColliding())
                    {
                        var collider = visionRayCast.GetCollider();
                        var colliderParent = ((Node3D)collider).GetParent();

                        // consider configuring collision layers here instead of explicit check

                        if (overlap.Name == "Player" || overlapParent.Name == "Player" || overlapParent.GetParent().Name == "Player")
                            game.Log($"I SEE YOU! - PT2", true);

                        if (colliderParent.GetType() == typeof(Player))
                        {
                            spottedTarget = (Node3D)colliderParent;
                            foundTarget = true;
                        }
                    }
                }
            }
        }
        return foundTarget;
    }

    public void HandleVisionCheckTimerTimeout()
    {
        switch (currentState)
        {
            case State.Idle:
            case State.Wander:
                if (FoundTarget())
                {
                    aggroAudio.Play();
                    SetState(State.Chase); // do something different for non-hostile NPCs like greet player
                }
                break;
        }
    }

    private bool ReachedTarget()
    {
        return GlobalTransform.Origin.DistanceTo(followTarget.GlobalTransform.Origin) <= attackRange;
    }

    // invoke from animations? or am I too lazy?
    private void SwingComplete()
    {
        var player = ((Player)followTarget);
        player.ReceiveHit(this, rand.RandiRange(damageMin, damageMax));
    }

    public override void _Process(double delta)
    {
        navAgent.DebugEnabled = game.debugMode;
        debugLabel.Visible = game.debugMode;
        visionDebugMesh.Visible = game.debugMode;
        if (followTarget != null)
        {
            navAgent.TargetPosition = followTarget.GlobalTransform.Origin;
            nextFollowMovePoint = navAgent.GetNextPathPosition();
        }

        if (game.gameOver)
            return;

        switch (currentState)
        {
            case State.None:
                SetState(State.Idle);
                break;
            case State.Wander:
                TryMoveStep();
                break;
            case State.Chase:
                if (ReachedTarget() && FoundTarget())
                {
                    movementType = MovementType.None;
                    if (hostile && !tookActionThisTick)
                    {
                        SetState(State.Attack);
                        tookActionThisTick = true;
                    }
                }
                else
                {
                    movementType = MovementType.Follow;
                    TryMoveStep();
                }
                break;
            case State.Attack:
                if (!tookActionThisTick)
                {
                    if (hostile && ReachedTarget() && FoundTarget())
                    {
                        SetState(State.Attack);
                        attackAudio.Play();
                        SwingComplete(); // todo: delay handling instead
                    }
                    else
                    {
                        movementType = MovementType.Follow;
                        SetState(State.Chase);
                    }
                    tookActionThisTick = true;
                }
                break;
        }
    }
}
