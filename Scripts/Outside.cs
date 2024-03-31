using Godot;

public partial class Outside : Node3D
{
    private Game game;
    private Player player;
    private Area3D sceneTransitionArea3D;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        game = GetNode<Game>("/root/Game");
        player = GetNode<Player>("Player");
        sceneTransitionArea3D = GetNode<Area3D>("SceneTransition/SceneTransitionArea3D");

        if (game.returningFromDungeon)
        {
            player.GlobalPosition = new Vector3(sceneTransitionArea3D.GlobalPosition.X, 1, sceneTransitionArea3D.GlobalPosition.Z);
            game.returningFromDungeon = false;
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
