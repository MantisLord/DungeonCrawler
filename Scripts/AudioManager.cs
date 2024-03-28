using Godot;
public partial class AudioManager : Node
{
    AudioStreamPlayer sfxStreamPlayer1 = new();
    AudioStreamPlayer sfxStreamPlayer2 = new();
    AudioStreamPlayer sfxStreamPlayer3 = new();
    AudioStreamPlayer ambienceStreamPlayer = new();
    AudioStreamPlayer musicStreamPlayer = new();
    AudioStream asStep, asSwingSword, asDrawSword, asSheathSword, asInteractSuccess;

    // todo: 1 channel per audio played?...
    public enum AudioChannel
    {
        SFX1, // menu buttons, item pickups, interacting
        SFX2, // weapons
        SFX3, // footsteps, menu open/close
        Ambient,
        Music
    }

    public enum Audio
    {
        Step,
        SwingSword,
        DrawSword,
        SheathSword,
        InteractSuccess,
    }

    public override void _Ready()
    {
        base._Ready();
        asStep = GD.Load<AudioStream>("res://Assets/Audio/step.wav");
        asSwingSword = GD.Load<AudioStream>("res://Assets/Audio/sword_swing.wav");
        asDrawSword = GD.Load<AudioStream>("res://Assets/Audio/sword_draw.mp3");
        asSheathSword = GD.Load<AudioStream>("res://Assets/Audio/sword_sheath.wav");
        asInteractSuccess = GD.Load<AudioStream>("res://Assets/Audio/interactSuccess.wav");
        AddChild(sfxStreamPlayer1);
        AddChild(sfxStreamPlayer2);
        AddChild(sfxStreamPlayer3);
        AddChild(ambienceStreamPlayer);
        AddChild(musicStreamPlayer);
    }

    public void Play(Audio sound, AudioChannel channel = AudioChannel.SFX1, bool await = false)
    {
        AudioStream stream = null;
        AudioStreamPlayer currentPlayer = null;

        switch (channel)
        {
            case AudioChannel.SFX1:
                currentPlayer = sfxStreamPlayer1;
                break;
            case AudioChannel.SFX2:
                currentPlayer = sfxStreamPlayer2;
                break;
            case AudioChannel.SFX3:
                currentPlayer = sfxStreamPlayer3;
                break;
            case AudioChannel.Ambient:
                currentPlayer = ambienceStreamPlayer;
                break;
            case AudioChannel.Music:
                currentPlayer = musicStreamPlayer;
                break;
        }
        float fromPos = 0;
        switch (sound)
        {
            case Audio.Step:
                stream = asStep;
                break;
            case Audio.DrawSword:
                stream = asDrawSword;
                break;
            case Audio.SwingSword:
                stream = asSwingSword;
                break;
            case Audio.SheathSword:
                stream = asSheathSword;
                break;
            case Audio.InteractSuccess:
                stream = asInteractSuccess;
                break;
        }
        currentPlayer.Stream = stream;
        currentPlayer.Play(fromPos);
        if (await)
            System.Threading.Thread.Sleep((int)(stream.GetLength() * 1000));
    }

    public void Stop()
    {
        sfxStreamPlayer1.Stop();
        sfxStreamPlayer2.Stop();
        sfxStreamPlayer3.Stop();
        ambienceStreamPlayer.Stop();
        musicStreamPlayer.Stop();
    }

    public bool IsMusicPlaying()
    {
        return musicStreamPlayer.Playing;
    }
}
