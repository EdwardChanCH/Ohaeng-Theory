using Godot;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

// WIPS
public class AudioManager : Node
{
    public int AudioChannelCount { get; private set; } = 16;


    private Dictionary<string, AudioStreamPlayer> _sfxChannels = new Dictionary<string, AudioStreamPlayer>();

    private AudioStreamPlayer _bgmChannels;


    private static AudioManager _instance;
    public override void _EnterTree()
    {
        if(_instance != null)
        {
            QueueFree();
            return;
        }
        _instance = this;

        var audioPlayer = new AudioStreamPlayer();
        audioPlayer.Bus = "BGM";
        _bgmChannels = audioPlayer;
        AddChild(audioPlayer);
    }

    private void CreateSFXChannel(string soundPath)
    {
        var audioPlayer = new AudioStreamPlayer();
        audioPlayer.Stream = GD.Load<AudioStream>(soundPath);

        audioPlayer.Bus = "SFX";
        _sfxChannels.Add(soundPath, audioPlayer);
        AddChild(audioPlayer);
    }

    public static void PlaySFX(string soundPath)
    {
        if(_instance != null)
        {
            _instance.PlaySFXInternal(soundPath);
        }
    }

    private void PlaySFXInternal(string soundPath)
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
        if (_instance != null)
        {
            _instance.PlayBGMInternal(soundPath, volume);
        }
    }

    public void PlayBGMInternal(string soundPath, float volume = 0.5f)
    {
        _bgmChannels.Stream = GD.Load<AudioStream>(soundPath);
        _bgmChannels.VolumeDb = GD.Linear2Db(volume);
        _bgmChannels.Play();
    }


    public static void SetSFXChannelVolume(string soundPath, float volume)
    {
        if (_instance != null)
        {
            _instance.SetSFXChannelVolumeInternal(soundPath, volume);
        }
    }

    private void SetSFXChannelVolumeInternal(string soundPath, float volume)
    {
        if (!_sfxChannels.ContainsKey(soundPath))
        {
            CreateSFXChannel(soundPath);
        }
        _sfxChannels[soundPath].VolumeDb = GD.Linear2Db(volume);
    }




    public static void SetMasterVolume(float volume)
    {
        if (_instance != null)
        {
            _instance.SetVolumeInternal(0, volume);
        }
    }
    public static void SetSFXVolume(float volume)
    {
        if (_instance != null)
        {
            _instance.SetVolumeInternal(1, volume);
        }
    }
    public static void SetBGMVolume(float volume)
    {
        if (_instance != null)
        {
            _instance.SetVolumeInternal(2, volume);
        }
    }
    private void SetVolumeInternal(int index, float volume)
    {
        AudioServer.SetBusVolumeDb(index, GD.Linear2Db(volume));
    }
}
