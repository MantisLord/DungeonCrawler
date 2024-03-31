using Godot;

public partial class Dungeon : Node3D
{
    private Game game;
    private PackedScene cyclops = GD.Load<PackedScene>("res://Scenes/cyclops.tscn");
    private NPC cyclopsInstance = new();
    private bool spawnedBonusCyclops = false;

    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        game.returningFromDungeon = true;
    }

	public override void _Process(double delta)
	{
        if (game.timerStarted && !spawnedBonusCyclops)
        {
            for (int i = 1; i <= 5; i++)
            {
                cyclopsInstance = cyclops.Instantiate<NPC>();
                cyclopsInstance.GlobalPosition = GetNode<Node3D>($"Spawns/Spawn{i}").GlobalPosition;
                AddChild(cyclopsInstance);
            }
            spawnedBonusCyclops = true;
        }
	}
}
