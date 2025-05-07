using Godot;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

// WIPS
public class AudioManager : Node
{
    public static AudioManager Singleton { get; private set; }

    public static int AudioChannelCount { get; private set; } = 16;

    private static Dictionary<string, AudioStreamPlayer> _sfxChannels = new Dictionary<string, AudioStreamPlayer>();

    private static AudioStreamPlayer _bgmChannels;

    public override void _EnterTree()
    {
        base._EnterTree();

        Singleton = this;

        AudioStreamPlayer audioPlayer = new AudioStreamPlayer();
        audioPlayer.Bus = "BGM";
        _bgmChannels = audioPlayer;
        
        AddChild(audioPlayer);
    }

    private static void CreateSFXChannel(string soundPath)
    {
        var audioPlayer = new AudioStreamPlayer();
        audioPlayer.Stream = GD.Load<AudioStream>(soundPath);

        audioPlayer.Bus = "SFX";
        _sfxChannels.Add(soundPath, audioPlayer);
        
        Singleton.AddChild(audioPlayer);
    }

    public static void PlaySFX(string soundPath)
    {
        PlaySFXInternal(soundPath);
    }

    private static void PlaySFXInternal(string soundPath)
    {
        if (!_sfxChannels.ContainsKey(soundPath))
        {
            CreateSFXChannel(soundPath);
        }

        var channelRef = _sfxChannels[soundPath];
        channelRef.Play();
        return;
    }

    public static void PlayBMG(string soundPath, float volume = 0.5f)
    {
        PlayBGMInternal(soundPath, volume);
    }

    public static void PlayBGMInternal(string soundPath, float volume = 0.5f)
    {
        _bgmChannels.Stream = GD.Load<AudioStream>(soundPath);
        _bgmChannels.VolumeDb = GD.Linear2Db(volume);
        _bgmChannels.Play();
    }


    public static void SetSFXChannelVolume(string soundPath, float volume)
    {
        SetSFXChannelVolumeInternal(soundPath, volume);
    }

    private static void SetSFXChannelVolumeInternal(string soundPath, float volume)
    {
        if (!_sfxChannels.ContainsKey(soundPath))
        {
            CreateSFXChannel(soundPath);
        }
        _sfxChannels[soundPath].VolumeDb = GD.Linear2Db(volume);
    }

    public static void SetMasterVolume(float volume)
    {
        SetVolumeInternal(0, volume);
    }
    public static void SetSFXVolume(float volume)
    {
        SetVolumeInternal(1, volume);
    }
    public static void SetBGMVolume(float volume)
    {
        SetVolumeInternal(2, volume);
    }
    private static void SetVolumeInternal(int index, float volume)
    {
        AudioServer.SetBusVolumeDb(index, GD.Linear2Db(volume));
    }
}
