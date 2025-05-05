using Godot;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

// WIPS
public class AudioManager : Node
{
    public int AudioChannelCount { get; private set; } = 16;

    private string AudioBus = "master";


    private Dictionary<string, AudioStreamPlayer> _sfxChannels = new Dictionary<string, AudioStreamPlayer>();
    //private AudioStreamPlayer _bgmChannels;


    private static AudioManager _instance;
    public override void _EnterTree()
    {
        if(_instance != null)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    private void CreateAudioChannel(string soundPath)
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
            CreateAudioChannel(soundPath);
        }

        var channelRef = _sfxChannels[soundPath];
        GD.Print(channelRef.Bus);
        channelRef.Play();
        return;
    }

    public static void SetChannelVolume(string soundPath, float volume)
    {
        if (_instance != null)
        {
            _instance.SetChannelVolumeInternal(soundPath, volume);
        }
    }

    private void SetChannelVolumeInternal(string soundPath, float volume)
    {
        if (!_sfxChannels.ContainsKey(soundPath))
        {
            CreateAudioChannel(soundPath);
        }
        _sfxChannels[soundPath].VolumeDb = GD.Linear2Db(volume);
    }

    public static void SetMasterVolume(float volume)
    {
        if (_instance != null)
        {
            _instance.SetMasterVolumeInternal(volume);
        }
    }

    private void SetMasterVolumeInternal(float volume)
    {
        AudioServer.SetBusVolumeDb(0, GD.Linear2Db(volume));
    }

    public static void SetSFXVolume(float volume)
    {
        if (_instance != null)
        {
            _instance.SetSFXVolumeInternal(volume);
        }
    }

    private void SetSFXVolumeInternal(float volume)
    {
        AudioServer.SetBusVolumeDb(1, GD.Linear2Db(volume));
    }
}
