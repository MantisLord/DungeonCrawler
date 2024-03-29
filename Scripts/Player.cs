using Godot;
using static Game;
using static AudioManager;

public partial class Player : Node3D
{
    private const string INTERACT_ACTION_NAME = "interact";
    private const float SWORD_ANIM_SPEED = 0.3f;
    private const float MOVE_SPEED = 0.3f;

    private Game game;
    private AudioManager audioMgr;

    private Label interactLabel;
    private Label debugInfoLabel;
    private Label logLabel;
    private ScrollContainer logScroll;

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

    private Texture2D swordTexture = GD.Load<Texture2D>("res://Assets/Textures/item_sword.png");

    private string keycodeString;
    private InteractableArea interactAreaType;
    private Node3D interactAreaParent;
    private AnimationPlayer anim;

    public bool tookActionThisTick = false;
    public bool swinging = false;

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        game.DebugLog += HandleDebugLogSignal;
        game.Tick += () => tookActionThisTick = false;
        audioMgr = GetNode<AudioManager>("/root/AudioManager");
        forwardRay = GetNode<RayCast3D>("ForwardRayCast3D");
        backRay = GetNode<RayCast3D>("BackRayCast3D");
        leftRay = GetNode<RayCast3D>("LeftRayCast3D");
        rightRay = GetNode<RayCast3D>("RightRayCast3D");

        menu = GetNode<PanelContainer>("UserInterface/MainMenu/MainMenuPanelContainer");
        interactLabel = GetNode<Label>("UserInterface/InteractLabel");
        logScroll = GetNode<ScrollContainer>("UserInterface/LogPanelContainer/ScrollContainer");
        logLabel = logScroll.GetNode<Label>("LogLabel");
        debugInfoLabel = GetNode<Label>("UserInterface/DebugInfoPanelContainer/DebugInfoLabel");
        anim = GetNode<AnimationPlayer>("AnimationPlayer");

        item1Panel = GetNode<PanelContainer>("UserInterface/StatsPanelContainer/HBoxContainer/Item1Panel");
        item2Panel = GetNode<PanelContainer>("UserInterface/StatsPanelContainer/HBoxContainer/Item2Panel");
        item3Panel = GetNode<PanelContainer>("UserInterface/StatsPanelContainer/HBoxContainer/Item3Panel");

        item1Texture = GetNode<TextureRect>("UserInterface/StatsPanelContainer/HBoxContainer/Item1Panel/Item1TextureRect");
        item2Texture = GetNode<TextureRect>("UserInterface/StatsPanelContainer/HBoxContainer/Item2Panel/Item2TextureRect");
        item3Texture = GetNode<TextureRect>("UserInterface/StatsPanelContainer/HBoxContainer/Item3Panel/Item3TextureRect");

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
                audioMgr.Play(Audio.InteractSuccess, AudioChannel.SFX1);
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
                audioMgr.Play(Audio.SheathSword, AudioChannel.SFX2);
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
                    audioMgr.Play(Audio.DrawSword, AudioChannel.SFX2);
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
                audioMgr.Play(Audio.SwingSword, AudioChannel.SFX2);
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

    public override void _Process(double delta)
    {
        debugInfoLabel.Text = $"Map: {GetParent().Name}" +
            $"\nPosition (X: {System.Math.Round(Position.X, 2)}, Y: {System.Math.Round(Position.Y, 2)}, Z: {System.Math.Round(Position.Z, 2)})" +
            $"\nRotationDegrees {Rotation.Y}" +
            $"\nEquipped: {equippedItem}" +
            $"\nCanUse: {canUse}" +
            $"\nCanEquip: {canEquip}" +
            $"\nFPS: {Engine.GetFramesPerSecond()}";

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
            audioMgr.Play(Audio.Step, AudioChannel.SFX3);
        }
        if (Input.IsActionPressed("back") && !backRay.IsColliding())
        {
            tween = game.HandleMoveTween(this, Vector3.Back, MOVE_SPEED);
            audioMgr.Play(Audio.Step, AudioChannel.SFX3);
        }
        if (Input.IsActionPressed("strafe_right") && !rightRay.IsColliding())
        {
            tween = game.HandleMoveTween(this, Vector3.Right, MOVE_SPEED);
            audioMgr.Play(Audio.Step, AudioChannel.SFX3);
        }
        if (Input.IsActionPressed("strafe_left") && !leftRay.IsColliding())
        {
            tween = game.HandleMoveTween(this, Vector3.Left, MOVE_SPEED);
            audioMgr.Play(Audio.Step, AudioChannel.SFX3);
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

    private void HandleDebugLogSignal()
    {
        SyncDebugLogText();
    }
    public async void SyncDebugLogText()
    {
        logLabel.Text = game.allLogText;
        // wait a tiny bit here, otherwise the vertical scroll max value won't update in time for us to scroll to the bottom...
        await ToSignal(GetTree().CreateTimer(0.01f), SceneTreeTimer.SignalName.Timeout);
        logScroll.ScrollVertical = (int)logScroll.GetVScrollBar().MaxValue;
    }

    // without this, disposed object will still receive signals
    public override void _ExitTree()
    {
        game.DebugLog -= HandleDebugLogSignal;
        base._ExitTree();
    }
}
