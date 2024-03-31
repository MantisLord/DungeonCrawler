using Godot;
using System;
using static Game;
using static AudioManager;

public partial class Player : Node3D
{
    private const string INTERACT_ACTION_NAME = "interact";
    private const float SWORD_ANIM_SPEED = 0.3f;
    private const float MOVE_SPEED = 0.3f;

    private Game game;
    private AudioManager audioMgr;

    private Camera3D cam;
    private RayCast3D forwardRay;
    private RayCast3D backRay;
    private RayCast3D leftRay;
    private RayCast3D rightRay;
    private Tween tween;

    private Item equippedItem;
    private bool canUse = false;
    private bool canEquip = true;

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
    private ColorRect restoreRect;
    private Label timerLabel;

    private Texture2D swordTexture = GD.Load<Texture2D>("res://Assets/Textures/item_sword.png");
    private Texture2D torchTexture = GD.Load<Texture2D>("res://Assets/Textures/item_torch.png");
    private Texture2D amphoraTexture = GD.Load<Texture2D>("res://Assets/Textures/item_amphora.png");

    private string keycodeString;
    private InteractableArea interactAreaType;
    private Node3D interactAreaParent;
    private AnimationPlayer swordAnim;
    private AnimationPlayer torchAnim;
    private AnimationPlayer amphoraAnim;

    private int health = 100;
    private int amphoraRestoreMin = 10;
    private int amphoraRestoreMax = 15;

    private Node3D torchLight;

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
    private AudioStreamPlayer3D amphoraEquipAudio;
    private AudioStreamPlayer3D amphoraDrinkAudio;

    private RandomNumberGenerator rand = new();

    public bool tookActionThisTick = false;
    public bool swinging = false;

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        audioMgr = GetNode<AudioManager>("/root/AudioManager");
        game.gameOver = false;
        game.DisplayLog += HandleDisplayLogSignal;
        game.Tick += () => tookActionThisTick = false;
        game.EscapeTimeout += HandleEscapeTimeoutSignal;

        cam = GetNode<Camera3D>("Camera3D");
        forwardRay = GetNode<RayCast3D>("ForwardRayCast3D");
        backRay = GetNode<RayCast3D>("BackRayCast3D");
        leftRay = GetNode<RayCast3D>("LeftRayCast3D");
        rightRay = GetNode<RayCast3D>("RightRayCast3D");
        swordAnim = GetNode<AnimationPlayer>("SwordAnimationPlayer");
        torchAnim = GetNode<AnimationPlayer>("TorchAnimationPlayer");
        amphoraAnim = GetNode<AnimationPlayer>("AmphoraAnimationPlayer");

        torchLight = GetNode<Node3D>("Torch/Light");

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
        restoreRect = GetNode<ColorRect>("UserInterface/RestoreRect");
        timerLabel = GetNode<Label>("UserInterface/TimerLabel");

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
        amphoraEquipAudio = GetNode<AudioStreamPlayer3D>("AmphoraEquipAudioStreamPlayer3D");
        amphoraDrinkAudio = GetNode<AudioStreamPlayer3D>("AmphoraDrinkAudioStreamPlayer3D");

        rand.Randomize();

        tookActionThisTick = false;
        interactLabel.Text = "";
        interactAreaType = InteractableArea.None;
        game.Log($"Entered {GetParent().Name}.");
        SetInteractKeycodeString();
        menu.Visible = false;
        InitUI();
        base._Ready();
    }

    private void InitUI()
    {
        if (game.foundItem1)
        {
            item1Texture.Texture = swordTexture;
        }
        if (game.foundItem2)
        {
            item2Texture.Texture = torchTexture;
        }
        if (game.foundItem3)
        {
            item3Texture.Texture = amphoraTexture;
        }
    }

    private void AddItem(Item item)
    {
        switch (item)
        {
            case Item.Sword:
                item1Texture.Texture = swordTexture;
                interactAudio.Play();
                game.Log($"Picked up {Item.Sword}. Use this weapon to slay your foes!");
                game.foundItem1 = true;
                break;
            case Item.Torch:
                item2Texture.Texture = torchTexture;
                interactAudio.Play();
                game.Log($"Picked up {Item.Torch}. May this tool guide your path.");
                game.foundItem2 = true;
                break;
            case Item.Amphora:
                item3Texture.Texture = amphoraTexture;
                interactAudio.Play();
                game.Log($"Picked up {Item.Amphora}. Drink from this vessel when you grow weary.");
                game.foundItem3 = true;
                break;
        }
    }

    private async void UnequipItem()
    {
        var animPlayer = new AnimationPlayer();
        canUse = false;
        canEquip = false;
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
        };

        switch (equippedItem)
        {
            case Item.Sword:
                animPlayer = swordAnim;
                item1Panel.AddThemeStyleboxOverride("panel", style);
                swordSheathAudio.Play();
                break;
            case Item.Torch:
                animPlayer = torchAnim;
                item2Panel.AddThemeStyleboxOverride("panel", style);
                if (torchLight.Visible)
                {
                    torchExtinguishAudio.Play();
                    torchLoopAudio.Stop();
                }
                torchLight.Visible = false;
                break;
            case Item.Amphora:
                animPlayer = amphoraAnim;
                amphoraEquipAudio.Play();
                item3Panel.AddThemeStyleboxOverride("panel", style);
                break;
        }
        animPlayer.PlayBackwards($"Raise{equippedItem}");
        game.Log($"Unequipped {equippedItem}.");
        await ToSignal(GetTree().CreateTimer(SWORD_ANIM_SPEED), SceneTreeTimer.SignalName.Timeout);
        equippedItem = Item.None;
        canEquip = true;
    }
    private async void EquipItem(Item newItem)
    {
        var animPlayer = new AnimationPlayer();
        canUse = false;
        canEquip = false;
        var style = new StyleBoxFlat
        {
            BgColor = new Color("YELLOW")
        };
        style.SetCornerRadiusAll(20);

        if (newItem != Item.None)
        {
            switch (newItem)
            {
                case Item.Sword:
                    animPlayer = swordAnim;
                    swordUnsheathAudio.Play();
                    item1Panel.AddThemeStyleboxOverride("panel", style);
                    break;
                case Item.Torch:
                    animPlayer = torchAnim;
                    // take out unlit torch sound here
                    item2Panel.AddThemeStyleboxOverride("panel", style);
                    break;
                case Item.Amphora:
                    animPlayer = amphoraAnim;
                    amphoraEquipAudio.Play();
                    item3Panel.AddThemeStyleboxOverride("panel", style);
                    break;
            }
            animPlayer.Play($"Raise{newItem}");
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
                swordAnim.Play("SwingSword");
                await ToSignal(GetTree().CreateTimer(SWORD_ANIM_SPEED * 2), SceneTreeTimer.SignalName.Timeout);
                swinging = false;
                swordAnim.Play($"RaiseSword");
                await ToSignal(GetTree().CreateTimer(SWORD_ANIM_SPEED), SceneTreeTimer.SignalName.Timeout);
                canUse = true;
                canEquip = true;
                break;
            case Item.Torch:
                torchLight.Visible = !torchLight.Visible;
                if (torchLight.Visible)
                {
                    torchLightAudio.Play();
                    torchLoopAudio.Play();
                }
                else
                {
                    torchExtinguishAudio.Play();
                    torchLoopAudio.Stop();
                }
                break;
            case Item.Amphora:
                canUse = false;
                canEquip = false;
                amphoraAnim.Play("DrinkAmphora");
                await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
                canUse = true;
                canEquip = true;
                break;
        }
    }

    // triggered by animation
    private async void PotionReachedLips()
    {
        amphoraDrinkAudio.Play();
        var restoreAmount = rand.RandiRange(amphoraRestoreMin, amphoraRestoreMax);
        game.Log($"You drank from the {Item.Amphora}. It restored {restoreAmount} HP.");
        restoreRect.Visible = true;
        await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        restoreRect.Visible = false;
    }

    private void SetInteractKeycodeString()
    {
        foreach (var action in InputMap.ActionGetEvents(INTERACT_ACTION_NAME))
            if (action.GetType() == typeof(InputEventKey))
                keycodeString = OS.GetKeycodeString(((InputEventKey)action).PhysicalKeycode);
    }

    private void OnArea3DEntered(Area3D area)
    {
        Enum.TryParse(area.Name, out InteractableArea interactArea);
        switch (interactArea)
        {
            case InteractableArea.SwordPickupArea3D:
            case InteractableArea.TorchPickupArea3D:
            case InteractableArea.AmphoraPickupArea3D:
            case InteractableArea.SpearPickupArea3D:
                interactAreaType = interactArea;
                interactAreaParent = area.GetParent<Node3D>();

                if (interactArea == InteractableArea.SpearPickupArea3D)
                    interactAreaParent = interactAreaParent.GetNode<Node3D>("Wooden Spear");

                interactLabel.Text = $"Press [{keycodeString}] to pick up {area.Name.ToString().Replace("PickupArea3D", "")}!";
                break;
            case InteractableArea.SceneTransitionArea3D:
                interactAreaType = interactArea;
                interactAreaParent = area.GetParent<Node3D>();

                if (game.currentScene == Scene.outside)
                    interactLabel.Text = $"Press [{keycodeString}] to enter dungeon.";
                else
                    interactLabel.Text = $"Press [{keycodeString}] to exit dungeon.";
                break;
        }
    }
    private void OnArea3DExited(Area3D area)
    {
        Enum.TryParse(area.Name, out InteractableArea interactArea);
        switch (interactArea)
        {
            case InteractableArea.SwordPickupArea3D:
            case InteractableArea.SceneTransitionArea3D:
            case InteractableArea.TorchPickupArea3D:
            case InteractableArea.AmphoraPickupArea3D:
            case InteractableArea.SpearPickupArea3D:
                interactLabel.Text = "";
                interactAreaType = InteractableArea.None;
                interactAreaParent = null;
                break;
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
            case InteractableArea.TorchPickupArea3D:
                AddItem(Item.Torch);
                interactAreaParent.QueueFree();
                break;
            case InteractableArea.SceneTransitionArea3D:
                if (game.currentScene == Scene.outside)
                {
                    game.ChangeScene(Scene.dungeon);
                }
                else
                    game.ChangeScene(Scene.outside);
                break;
            case InteractableArea.AmphoraPickupArea3D:
                AddItem(Item.Amphora);
                interactAreaParent.QueueFree();
                break;
            case InteractableArea.SpearPickupArea3D:
                audioMgr.StopMusic();
                audioMgr.Play(Audio.MusicEscape, AudioChannel.Music);
                interactAreaParent.QueueFree();
                game.timeToEscape.Start();
                game.timerStarted = true;
                game.Log("The ground rumbles as you grab the spear.\nCyclopes bellow from all corners of the island, somehow of one mind.\nYou must escape before they catch you!");
                break;
        }
    }

    // todo: more specific hitboxes for players?
    public async void ReceiveHit(Node3D attacker, int damageDealt)
    {
        var npcAttacker = (NPC)attacker;
        game.Log($"{npcAttacker.name} hit you for {damageDealt} damage.");

        // need sounds for this
        //receiveHitAudioPlayer.Play();

        health -= damageDealt;
        healthBar.Value = health;

        hitRect.Visible = true;
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        hitRect.Visible = false;

        if (health <= 0)
        {
            GameOver($"\n\n\nYou have been slain by a {npcAttacker.name}! Your spirit will haunt the island for eternity!");
        }
    }

    private void GameOver(string reason)
    {
        game.returningFromDungeon = false;
        game.gameOver = true;
        menu.Visible = true;
        gameOverLabel.Visible = true;
        gameOverLabel.Text = reason;
    }

    private void ItemSwitchHelper(Item newItem)
    {
        if (equippedItem != Item.None && equippedItem != newItem)
        {
            UnequipItem();
            EquipItem(newItem);
        }
        else if (equippedItem == newItem) // must be trying to just put away instead of switch
            UnequipItem();
        else
            EquipItem(newItem);
    }

    public override void _Process(double delta)
    {
        float degrees = NormalizeDegrees(Rotation.Y);
        string cardinal = GetCardinalDirectionFromNormalizedDegrees(degrees);
        debugInfoLabel.Text = $"Map: {GetParent().Name}" +
            $"\nPosition (X: {Math.Round(Position.X, 2)}, Y: {Math.Round(Position.Y, 2)}, Z: {Math.Round(Position.Z, 2)})" +
            $"\nDegrees: {degrees}" +
            $"\nFacing: {cardinal}" +
            $"\nEquipped: {equippedItem}" +
            $"\nCanUse: {canUse}" +
            $"\nCanEquip: {canEquip}" +
            $"\nFPS: {Engine.GetFramesPerSecond()}";
        debugInfoPanel.Visible = game.debugMode;

        if (game.timerStarted)
        {
            timerLabel.Visible = true;
            timerLabel.Text = $"TIME TO ESCAPE: {game.timeToEscape.TimeLeft:00:00}";
        }

        if (cam.Fov != game.fov)
            cam.Fov = game.fov;

        if (game.gameOver)
            return;

        if (Input.IsActionJustPressed("interact"))
        {
            Interact();
        }
        if (Input.IsActionJustPressed("equip_item1") && game.foundItem1 && canEquip)
        {
            ItemSwitchHelper(Item.Sword);
        }
        if (Input.IsActionJustPressed("equip_item2") && game.foundItem2 && canEquip)
        {
            ItemSwitchHelper(Item.Torch);
        }
        if (Input.IsActionJustPressed("equip_item3") && game.foundItem3 && canEquip)
        {
            ItemSwitchHelper(Item.Amphora);
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

    private void HandleEscapeTimeoutSignal()
    {
        GameOver("The cyclopes surround you, too many to fight off. Your last window to the mortal world is a massive foot descending upon your head.");
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
        game.EscapeTimeout -= HandleEscapeTimeoutSignal;
        base._ExitTree();
    }
}
