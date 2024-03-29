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
    [Export] public WanderType wanderType = WanderType.Trailblazer;
    [Export] public bool randomIdle = true;
    [Export] public int idleTimeMin = 8; // only impacts initial idle after spawn if randomIdle = false
    [Export] public int idleTimeMax = 16;
    [Export] public int wanderTimeMin = 30; // how long before potentially going idle again if both wander = true and randomIdle = true
    [Export] public int wanderTimeMax = 60;

    private Game game;
    private AudioStreamPlayer3D stepAudio;
    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback stateMachine;
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
        game.Log($"{name} playing anim {newAnimation}.");
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

    public enum WanderType
    {
        Patrol,         // straight lines, only turning when necessary
        Trailblazer,    // 75% forward, 25% turn
        Chaotic         // all random
    }

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        anim = GetNode<AnimationPlayer>("AnimationPlayer");
        game.Tick += () => tookActionThisTick = false;
        stepAudio = GetNode<AudioStreamPlayer3D>("StepAudioStreamPlayer3D");

        forwardRay = GetNode<RayCast3D>("ForwardRayCast3D");
        backRay = GetNode<RayCast3D>("BackRayCast3D");
        leftRay = GetNode<RayCast3D>("LeftRayCast3D");
        rightRay = GetNode<RayCast3D>("RightRayCast3D");

        rand.Randomize();
        stateEndTimer = new()
        {
            OneShot = true
        };
        stateEndTimer.Timeout += HandleStateEndTimerTimeout;
        AddChild(stateEndTimer);
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

    void MoveRandomStep()
    {
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

        MoveAction randomMove = MoveAction.Forward;
        switch (wanderType)
        {
            case WanderType.Chaotic:
                randomMove = availMoveActions[rand.RandiRange(0, availMoveActions.Count)];
                break;
            case WanderType.Patrol:
                availMoveActions.Remove(MoveAction.RotateLeft);
                availMoveActions.Remove(MoveAction.RotateRight);
                randomMove = availMoveActions[rand.RandiRange(0, availMoveActions.Count)];
                break;
            case WanderType.Trailblazer:
                int chance = rand.RandiRange(0, 100);
                if (chance <= 75)
                {
                    randomMove = MoveAction.Forward;
                }
                else
                {
                    randomMove = rand.RandiRange(0, 1) == 1 ? MoveAction.RotateLeft : MoveAction.RotateRight;
                }
                break;
        }
        if (!CanPerformMoveAction(randomMove))
        {
            randomMove = GetFirstUnblockedMove();
        }
        HandleMoveAction(randomMove);
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
                game.Log($"{name} idling for {stateEndTimer.TimeLeft} secs.");
                break;
            case State.Wander:
                stateEndTimer.WaitTime = rand.RandiRange(wanderTimeMin, wanderTimeMax);
                stateEndTimer.Start();
                game.Log($"{name} wandering for {stateEndTimer.TimeLeft} secs.");
                break;
            case State.Dead:
                game.Log($"{name} died.");
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
        currentHealth -= damageDealt;
        game.Log($"{attacker.Name} hit {name}'s {hitArea.GetParent().Name} and dealt {damageDealt} damage! {name} has {currentHealth} HP remaining.");
        if (currentHealth <= 0)
        {
            SetState(State.Dead);
        }
    }

    public override void _Process(double delta)
    {
        if (!tookActionThisTick)
        {
            //string currentAnim = stateMachine.GetCurrentNode();
            switch (currentState)
            {
                case State.None:
                    SetState(State.Idle);
                    break;
                case State.Wander:
                    MoveRandomStep();
                    break;
            }
            tookActionThisTick = true;
        }
    }
}
