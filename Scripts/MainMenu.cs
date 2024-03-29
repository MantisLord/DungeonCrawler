using Godot;
using static Game;

public partial class MainMenu : Node
{
    private Game game;
    private PanelContainer mainPanel;
    private PanelContainer optionsPanel;

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        optionsPanel = GetNode<PanelContainer>("OptionsPanelContainer");
        mainPanel = GetNode<PanelContainer>("MainMenuPanelContainer");
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
}
