using Godot;
using static Game;
public partial class MainMenu : Control
{
    private Game game;
    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        base._Ready();
    }
    void PlayButtonPressed()
    {
        game.ChangeScene(Scene.outside);
    }
    void QuitButtonPressed()
    {
        System.Environment.Exit(1);
    }
}
