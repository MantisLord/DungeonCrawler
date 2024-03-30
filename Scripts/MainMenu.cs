using Godot;
using System;
using static Game;

public partial class MainMenu : Node
{
    private Game game;
    private PanelContainer mainPanel;
    private PanelContainer optionsPanel;
    private Label fovSettingDisplayLabel;
    private HSlider fovSlider;
    private CheckButton debugModeCheckButton;
    private CheckButton moveStyleCheckButton;

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        mainPanel = GetNode<PanelContainer>("MainMenuPanelContainer");
        optionsPanel = GetNode<PanelContainer>("OptionsPanelContainer");
        fovSettingDisplayLabel = optionsPanel.GetNode<Label>("VBoxContainer/HBoxContainer/FOVSettingDisplayLabel");
        fovSlider = optionsPanel.GetNode<HSlider>("VBoxContainer/HBoxContainer/FOVHSlider");
        moveStyleCheckButton = optionsPanel.GetNode<CheckButton>("VBoxContainer/MoveStyleCheckButton");
        debugModeCheckButton = optionsPanel.GetNode<CheckButton>("VBoxContainer/DebugModeCheckButton");

        // so any changes that were made on main menu screen are persisted ingame
        fovSlider.Value = game.fov;
        moveStyleCheckButton.ButtonPressed = game.teleportMode;
        debugModeCheckButton.ButtonPressed = game.debugMode;

        base._Ready();
    }

    void PlayButtonPressed()
    {
        game.ChangeScene(Scene.outside);
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
