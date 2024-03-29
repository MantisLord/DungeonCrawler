using Godot;
using static Game;

public partial class Sword : Node3D
{
    [Export] public int damageMin = 1;
    [Export] public int damageMax = 3;
    private RandomNumberGenerator rand = new();
    private Node3D hitEmissionPoint;
    private Game game;
    private Player player;
    
    public override void _Ready()
	{
        game = GetNode<Game>("/root/Game");
        player = GetParent<Player>();
        hitEmissionPoint = GetNode<Node3D>("HitEmissionPoint");
        rand.Randomize();
    }

    private void OnArea3DEntered(Area3D area)
    {
        if (player == null || !player.swinging)
            return;

        if (area.Name == InteractableArea.HitboxArea3D.ToString())
        {
            var recipient = area.GetOwner<NPC>();
            // should occur at time of entering or leaving??
            recipient.ReceiveHit(this, area, rand.RandiRange(damageMin, damageMax));
            recipient.TriggerHitEmission(hitEmissionPoint.GlobalPosition);
        }
    }
    private void OnArea3DExited(Area3D area)
    {
        if (player == null || !player.swinging)
            return;

        if (area.Name == InteractableArea.HitboxArea3D.ToString())
        {
            var recipient = area.GetOwner<NPC>();
            recipient.TriggerHitEmission(hitEmissionPoint.GlobalPosition);
        }
    }

    public override void _Process(double delta)
	{
	}
}
