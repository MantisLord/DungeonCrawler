using Godot;
using static Game;

public partial class Player : Node3D
{
    private const string INTERACT_ACTION_NAME = "interact";
    private const float SWORD_ANIM_SPEED = 0.3f;
    private const float MOVE_SPEED = 0.3f;

    private Game game;

    private Camera3D cam;
    private RayCast3D forwardRay;
    private RayCast3D backRay;
    private RayCast3D leftRay;
    private RayCast3D rightRay;
    private Tween tween;

    private Item equippedItem;
    private bool canUse = false;
    private bool canEquip = true;

    private bool foundItem1 = false;
    private bool foundItem2 = false;
    private bool foundItem3 = false;

    // ui
    private PanelContainer menu;
    private TextureRect item1Texture;
    private TextureRect item2Texture;
    private TextureRect item3Texture;
    private PanelContainer item1Panel;
    private PanelContainer item2Panel;
    private PanelContainer item3Panel;
    private Label interactLabel;
    private PanelContainer debugInfoPanel;
    private Label debugInfoLabel;
    private Label logLabel;
    private ScrollContainer logScroll;
    private ProgressBar healthBar;
    private ColorRect hitRect;
    private Label gameOverLabel;

    private Texture2D swordTexture = GD.Load<Texture2D>("res://Assets/Textures/item_sword.png");

    private string keycodeString;
    private InteractableArea interactAreaType;
    private Node3D interactAreaParent;
    private AnimationPlayer anim;

    private int health = 100;

    private AudioStreamPlayer3D interactAudio;
    private AudioStreamPlayer3D stepAudio;
    private AudioStreamPlayer3D swordUnsheathAudio;
    private AudioStreamPlayer3D swordSheathAudio;
    private AudioStreamPlayer3D swordHitAudio;
    private AudioStreamPlayer3D swordSwingAudio;
    private AudioStreamPlayer3D torchLightAudio;
    private AudioStreamPlayer3D torchExtinguishAudio;
    private AudioStreamPlayer3D torchLoopAudio;
    private AudioStreamPlayer3D receiveHitAudio; // missing sound
    private AudioStreamPlayer3D deathAudio; // missing sound

    public bool tookActionThisTick = false;
    public bool swinging = false;

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        game.gameOver = false;
        game.DisplayLog += HandleDisplayLogSignal;
        game.Tick += () => tookActionThisTick = false;

        cam = GetNode<Camera3D>("Camera3D");
        forwardRay = GetNode<RayCast3D>("ForwardRayCast3D");
        backRay = GetNode<RayCast3D>("BackRayCast3D");
        leftRay = GetNode<RayCast3D>("LeftRayCast3D");
        rightRay = GetNode<RayCast3D>("RightRayCast3D");
        anim = GetNode<AnimationPlayer>("AnimationPlayer");

        menu = GetNode<PanelContainer>("UserInterface/MainMenu/MainMenuPanelContainer");
        interactLabel = GetNode<Label>("UserInterface/InteractLabel");
        logScroll = GetNode<ScrollContainer>("UserInterface/LogPanelContainer/ScrollContainer");
        logLabel = logScroll.GetNode<Label>("LogLabel");
        debugInfoPanel = GetNode<PanelContainer>("UserInterface/DebugInfoPanelContainer");
        debugInfoLabel = debugInfoPanel.GetNode<Label>("DebugInfoLabel");
        item1Panel = GetNode<PanelContainer>("UserInterface/StatsPanelContainer/HBoxContainer/Item1Panel");
        item2Panel = GetNode<PanelContainer>("UserInterface/StatsPanelContainer/HBoxContainer/Item2Panel");
        item3Panel = GetNode<PanelContainer>("UserInterface/StatsPanelContainer/HBoxContainer/Item3Panel");
        item1Texture = GetNode<TextureRect>("UserInterface/StatsPanelContainer/HBoxContainer/Item1Panel/Item1TextureRect");
        item2Texture = GetNode<TextureRect>("UserInterface/StatsPanelContainer/HBoxContainer/Item2Panel/Item2TextureRect");
        item3Texture = GetNode<TextureRect>("UserInterface/StatsPanelContainer/HBoxContainer/Item3Panel/Item3TextureRect");
        healthBar = GetNode<ProgressBar>("UserInterface/StatsPanelContainer/HBoxContainer/HPProgressBar");
        hitRect = GetNode<ColorRect>("UserInterface/HitRect");
        gameOverLabel = GetNode<Label>("UserInterface/GameOverLabel");

        interactAudio = GetNode<AudioStreamPlayer3D>("InteractAudioStreamPlayer3D");
        stepAudio = GetNode<AudioStreamPlayer3D>("StepAudioStreamPlayer3D");
        swordUnsheathAudio = GetNode<AudioStreamPlayer3D>("SwordUnsheathAudioStreamPlayer3D");
        swordSheathAudio = GetNode<AudioStreamPlayer3D>("SwordSheathAudioStreamPlayer3D");
        swordHitAudio = GetNode<AudioStreamPlayer3D>("SwordHitAudioStreamPlayer3D"); // use me somewhere
        swordSwingAudio = GetNode<AudioStreamPlayer3D>("SwordSwingAudioStreamPlayer3D");
        torchLightAudio = GetNode<AudioStreamPlayer3D>("TorchLightAudioStreamPlayer3D");
        torchExtinguishAudio = GetNode<AudioStreamPlayer3D>("TorchExtinguishAudioStreamPlayer3D");
        torchLoopAudio = GetNode<AudioStreamPlayer3D>("TorchLoopAudioStreamPlayer3D");
        receiveHitAudio = GetNode<AudioStreamPlayer3D>("ReceiveHitAudioStreamPlayer3D"); // missing sound
        deathAudio = GetNode<AudioStreamPlayer3D>("DeathAudioStreamPlayer3D"); // missing sound

        tookActionThisTick = false;
        interactLabel.Text = "";
        interactAreaType = InteractableArea.None;
        game.Log($"Entered {GetParent().Name}.");
        SetInteractKeycodeString();
        menu.Visible = false;
        base._Ready();
    }

    private void AddItem(Item item)
    {
        switch (item)
        {
            case Item.Sword:
                item1Texture.Texture = swordTexture;
                interactAudio.Play();
                game.Log($"Picked up {Item.Sword}.");
                foundItem1 = true;
                break;
        }
    }

    private async void UnequipItem()
    {
        canUse = false;
        canEquip = false;
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
        };

        anim.PlayBackwards($"Raise{equippedItem}");
        switch (equippedItem)
        {
            case Item.Sword:
                swordSheathAudio.Play();
                item1Panel.AddThemeStyleboxOverride("panel", style);
                break;
        }
        game.Log($"Unequipped {equippedItem}.");
        await ToSignal(GetTree().CreateTimer(SWORD_ANIM_SPEED), SceneTreeTimer.SignalName.Timeout);
        equippedItem = Item.None;
        canEquip = true;
    }
    private async void EquipItem(Item newItem)
    {
        canUse = false;
        canEquip = false;
        if (newItem != Item.None)
        {
            switch (newItem)
            {
                case Item.Sword:
                    swordUnsheathAudio.Play();
                    var style = new StyleBoxFlat();
                    style.BgColor = new Color("YELLOW");
                    style.SetCornerRadiusAll(20);
                    item1Panel.AddThemeStyleboxOverride("panel", style);
                    break;
            }
            anim.Play($"Raise{newItem}");
            await ToSignal(GetTree().CreateTimer(SWORD_ANIM_SPEED), SceneTreeTimer.SignalName.Timeout);
            canUse = true;
            canEquip = true;
            game.Log($"Equipped {newItem}.");
        }
        equippedItem = newItem;
    }

    private async void UseItem()
    {
        game.Log($"Used {equippedItem}.");
        switch (equippedItem)
        {
            case Item.Sword:
                canUse = false;
                canEquip = false;
                swinging = true;
                swordSwingAudio.Play();
                anim.Play("SwingSword");
                await ToSignal(GetTree().CreateTimer(SWORD_ANIM_SPEED * 2), SceneTreeTimer.SignalName.Timeout);
                swinging = false;
                anim.Play($"RaiseSword");
                await ToSignal(GetTree().CreateTimer(SWORD_ANIM_SPEED), SceneTreeTimer.SignalName.Timeout);
                canUse = true;
                canEquip = true;
                break;
        }
    }

    private void SetInteractKeycodeString()
    {
        foreach (var action in InputMap.ActionGetEvents(INTERACT_ACTION_NAME))
            if (action.GetType() == typeof(InputEventKey))
                keycodeString = OS.GetKeycodeString(((InputEventKey)action).PhysicalKeycode);
    }

    private void OnArea3DEntered(Area3D area)
    {
        if (area.Name == InteractableArea.SwordPickupArea3D.ToString())
        {
            interactLabel.Text = $"Press [{keycodeString}] to pick up sword!";
            interactAreaType = InteractableArea.SwordPickupArea3D;
            interactAreaParent = area.GetParent<Node3D>();
        }
    }
    private void OnArea3DExited(Area3D area)
    {
        if (area.Name == InteractableArea.SwordPickupArea3D.ToString())
        {
            interactLabel.Text = "";
            interactAreaType = InteractableArea.None;
            interactAreaParent = null;
        }
    }

    private void Interact()
    {
        switch (interactAreaType)
        {
            case InteractableArea.None:
                // play oopmf sound, nothing here to use
                break;
            case InteractableArea.SwordPickupArea3D:
                AddItem(Item.Sword);
                interactAreaParent.QueueFree();
                break;
        }
    }

    // todo: more specific hitboxes for players?
    public async void ReceiveHit(Node3D attacker, int damageDealt)
    {
        game.Log($"{attacker.Name} hit you for {damageDealt} damage.");

        // need sounds for this
        //receiveHitAudioPlayer.Play();

        health -= damageDealt;
        healthBar.Value = health;

        hitRect.Visible = true;
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        hitRect.Visible = false;

        if (health <= 0)
        {
            game.gameOver = true;
            menu.Visible = true;
            gameOverLabel.Visible = true;
            gameOverLabel.Text = $"\n\n\nGAME OVER - You were killed by a {attacker.Name}!";
        }
    }

    public override void _Process(double delta)
    {
        float degrees = NormalizeDegrees(Rotation.Y);
        string cardinal = GetCardinalDirectionFromNormalizedDegrees(degrees);
        debugInfoLabel.Text = $"Map: {GetParent().Name}" +
            $"\nPosition (X: {System.Math.Round(Position.X, 2)}, Y: {System.Math.Round(Position.Y, 2)}, Z: {System.Math.Round(Position.Z, 2)})" +
            $"\nDegrees: {degrees}" +
            $"\nFacing: {cardinal}" +
            $"\nEquipped: {equippedItem}" +
            $"\nCanUse: {canUse}" +
            $"\nCanEquip: {canEquip}" +
            $"\nFPS: {Engine.GetFramesPerSecond()}";
        debugInfoPanel.Visible = game.debugMode;

        if (cam.Fov != game.fov)
            cam.Fov = game.fov;

        if (game.gameOver)
            return;

        if (Input.IsActionJustPressed("interact"))
        {
            Interact();
        }
        if (Input.IsActionJustPressed("equip_item1") && foundItem1 && canEquip)
        {
            switch (equippedItem)
            {
                case Item.Sword:
                    UnequipItem();
                    break;
                case Item.None:
                    EquipItem(Item.Sword);
                    break;
            }
        }
        if (Input.IsActionJustPressed("equip_item2"))
        {
        }
        if (Input.IsActionJustPressed("equip_item3"))
        {
        }
        if (Input.IsActionJustPressed("use_item") && canUse)
        {
            UseItem();
        }
        if (Input.IsActionJustPressed("menu"))
        {
            menu.Visible = !menu.Visible;
        }

        if (tween != null && tween.IsRunning())
            return;
        if (Input.IsActionPressed("forward") && !forwardRay.IsColliding())
        {
            tween = game.HandleMoveTween(this, Vector3.Forward, MOVE_SPEED);
            stepAudio.Play();
        }
        if (Input.IsActionPressed("back") && !backRay.IsColliding())
        {
            tween = game.HandleMoveTween(this, Vector3.Back, MOVE_SPEED);
            stepAudio.Play();
        }
        if (Input.IsActionPressed("strafe_right") && !rightRay.IsColliding())
        {
            tween = game.HandleMoveTween(this, Vector3.Right, MOVE_SPEED);
            stepAudio.Play();
        }
        if (Input.IsActionPressed("strafe_left") && !leftRay.IsColliding())
        {
            tween = game.HandleMoveTween(this, Vector3.Left, MOVE_SPEED);
            stepAudio.Play();
        }
        if (Input.IsActionPressed("turn_left"))
        {
            tween = game.HandleRotateTween(this, 1, MOVE_SPEED);
        }
        if (Input.IsActionPressed("turn_right"))
        {
            tween = game.HandleRotateTween(this, -1, MOVE_SPEED);
        }
        base._Process(delta);
    }

    private void HandleDisplayLogSignal()
    {
        SyncLogText();
    }
    public async void SyncLogText()
    {
        logLabel.Text = game.allLogText;
        // wait a tiny bit here, otherwise the vertical scroll max value won't update in time for us to scroll to the bottom...
        await ToSignal(GetTree().CreateTimer(0.01f), SceneTreeTimer.SignalName.Timeout);
        logScroll.ScrollVertical = (int)logScroll.GetVScrollBar().MaxValue;
    }

    // without this, disposed object will still receive signals
    public override void _ExitTree()
    {
        game.DisplayLog -= HandleDisplayLogSignal;
        base._ExitTree();
    }
}
