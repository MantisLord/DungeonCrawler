using Godot;
using System;

public partial class Dungeon : Node3D
{
    private Game game;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        game = GetNode<Game>("/root/Game");
        game.returningFromDungeon = true;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
