using Godot;

public partial class AudioManager : Node
{
    AudioStreamPlayer sfxStreamPlayer = new();
    AudioStreamPlayer ambienceStreamPlayer = new();
    AudioStreamPlayer musicStreamPlayer = new();
    AudioStream asAmbienceOutside, asMusicDungeon, asMusicEscape;
    public enum AudioChannel
    {
        SFX,
        Ambient,
        Music
    }

    public enum Audio
    {
        AmbienceOutside,
        MusicDungeon,
        MusicEscape,
    }

    public override void _Ready()
    {
        base._Ready();
        asAmbienceOutside = GD.Load<AudioStream>("res://Assets/Audio/Music/Beach.wav");
        asMusicDungeon = GD.Load<AudioStream>("res://Assets/Audio/Music/battle_music_2.wav");
        asMusicEscape = GD.Load<AudioStream>("res://Assets/Audio/Music/battle_music_final.wav");
        AddChild(sfxStreamPlayer);
        AddChild(ambienceStreamPlayer);
        AddChild(musicStreamPlayer);
    }

    public void Play(Audio sound, AudioChannel channel = AudioChannel.SFX, bool await = false)
    {
        AudioStream stream = null;
        AudioStreamPlayer currentPlayer = null;

        switch (channel)
        {
            case AudioChannel.SFX:
                currentPlayer = sfxStreamPlayer;
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
            case Audio.MusicEscape:
                stream = asMusicEscape;
                break;
            case Audio.MusicDungeon:
                stream = asMusicDungeon;
                break;
            case Audio.AmbienceOutside:
                stream = asAmbienceOutside;
                break;
        }
        currentPlayer.Stream = stream;
        currentPlayer.Play(fromPos);
    }

    public void StopMusic()
    {
        musicStreamPlayer.Stop();
    }
    public void StopAmbience()
    {
        ambienceStreamPlayer.Stop();
    }
}
