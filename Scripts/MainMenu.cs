using Godot;
using System;
using static Game;

public partial class MainMenu : Node
{
    private Game game;
    private AudioManager audioMgr;
    private PanelContainer mainPanel;
    private PanelContainer optionsPanel;
    private Label fovSettingDisplayLabel;
    private HSlider fovSlider;
    private CheckButton debugModeCheckButton;
    private CheckButton moveStyleCheckButton;
    private Button playButton;
    private Button cancelButton;

    // start screen parent - main_menu_start_screen.tscn
    private Label titleLabel;
    private Label creditsLabel;
    private Label introLabel;
    private ColorRect blackRect;
    private Button proceedButton;

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        mainPanel = GetNode<PanelContainer>("MainMenuPanelContainer");
        optionsPanel = GetNode<PanelContainer>("OptionsPanelContainer");
        fovSettingDisplayLabel = optionsPanel.GetNode<Label>("VBoxContainer/HBoxContainer/FOVSettingDisplayLabel");
        fovSlider = optionsPanel.GetNode<HSlider>("VBoxContainer/HBoxContainer/FOVHSlider");
        moveStyleCheckButton = optionsPanel.GetNode<CheckButton>("VBoxContainer/MoveStyleCheckButton");
        debugModeCheckButton = optionsPanel.GetNode<CheckButton>("VBoxContainer/DebugModeCheckButton");
        playButton = GetNode<Button>("MainMenuPanelContainer/VBoxContainer/PlayButton");
        cancelButton = GetNode<Button>("MainMenuPanelContainer/VBoxContainer/CancelButton");

        // so any changes that were made on main menu screen are persisted ingame
        fovSlider.Value = game.fov;
        moveStyleCheckButton.ButtonPressed = game.teleportMode;
        debugModeCheckButton.ButtonPressed = game.debugMode;

        if (game.currentScene == Scene.main_menu)
        {
            titleLabel = GetParent().GetNode<Label>("TitleLabel");
            creditsLabel = GetParent().GetNode<Label>("CreditsLabel");
            introLabel = GetParent().GetNode<Label>("IntroLabel");
            blackRect = GetParent().GetNode<ColorRect>("BlackRect");
            proceedButton = GetParent().GetNode<Button>("ProceedButton");
        }
        else
        {
            playButton.Text = "Restart";
            cancelButton.Visible = true;
        }

        base._Ready();
    }

    void Play()
    {
        if (introLabel != null && blackRect !=  null)
        {
            introLabel.Visible = false;
            blackRect.Visible = false;
        }
        game.Restart();
        game.ChangeScene(Scene.outside);
    }

    void ProceedButtonPressed()
    {
        Play();
    }

    void CancelButtonPressed()
    {
        mainPanel.Visible = false;
    }

    void PlayButtonPressed()
    {
        // intro sequence
        if (game.currentScene == Scene.main_menu)
        {
            mainPanel.Visible = false;
            titleLabel.Visible = false;
            creditsLabel.Visible = false;
            introLabel.Visible = true;
            blackRect.Visible = true;
            proceedButton.Visible = true;
        }
        else
        {
            Play();
        }
    }

    void OptionsButtonPressed()
    {
        mainPanel.Visible = false;
        optionsPanel.Visible = true;
    }

    void BackButtonPressed()
    {
        mainPanel.Visible = true;
        optionsPanel.Visible = false;
    }

    void QuitButtonPressed()
    {
        System.Environment.Exit(1);
    }

    void DebugModeOptionToggled(bool toggleState)
    {
        game.debugMode = toggleState;
    }

    void TeleportModeToggled(bool toggleState)
    {
        game.teleportMode = toggleState;
    }

    void FOVOptionChanged(float newValue)
    {
        game.fov = newValue;
        fovSettingDisplayLabel.Text = Convert.ToInt32(game.fov).ToString("000");
    }
}
