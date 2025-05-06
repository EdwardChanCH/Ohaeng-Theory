using Godot;
using System;

public class AudioTestingUserInterface : Node
{
    [Export]
    Resource[] SoundPath { get; set; } = new Resource[0];

    [Export]
    Resource[] MusicPath { get; set; } = new Resource[0];

    [Export]
    NodePath ButtonContainer { get; set; }

    [Export]
    NodePath MasterSliderPath { get; set; }

    [Export]
    NodePath SFXSliderPath { get; set; }

    [Export]
    NodePath BGMSliderPath { get; set; }

    public override void _Ready()
    {
        var buttonContrainer = GetNode<VBoxContainer>(ButtonContainer);
        var MasterSlider = GetNode<Slider>(MasterSliderPath);
        var SFXSlider = GetNode<Slider>(SFXSliderPath);
        var BGMSlider = GetNode<Slider>(BGMSliderPath);

        foreach (Resource sound in SoundPath)
        {
            var button = new Button();

            var arg = new Godot.Collections.Array(sound);
            button.Connect("pressed", this, "PlaySound", arg);

            button.RectSize = new Vector2(300, 60);
            button.Text = sound.ResourcePath;
            button.Theme = GD.Load<Theme>("res://themes/button_theme.tres");
            buttonContrainer.AddChild(button);
        }


        foreach (Resource sound in MusicPath)
        {
            var button = new Button();
            var arg = new Godot.Collections.Array(sound);
            button.Connect("pressed", this, "PlayMusic", arg);
            button.RectSize = new Vector2(300, 60);
            button.Text = sound.ResourcePath;
            button.Theme = GD.Load<Theme>("res://themes/button_theme.tres");
            buttonContrainer.AddChild(button);
        }

        var masterArg = new Godot.Collections.Array(MasterSlider, 0);
        MasterSlider.Connect("drag_ended", this, "UpdateVolume", masterArg);

        var sfxArg = new Godot.Collections.Array(SFXSlider, 1);
        SFXSlider.Connect("drag_ended", this, "UpdateVolume", sfxArg);

        var bgmArg = new Godot.Collections.Array(BGMSlider, 2);
        BGMSlider.Connect("drag_ended", this, "UpdateVolume", bgmArg);
    }

    private void PlaySound(Resource sound)
    {
        AudioManager.PlaySFX(sound.ResourcePath);
    }

    private void PlayMusic(Resource sound)
    {
        AudioManager.PlayBMG(sound.ResourcePath);
    }

    private void UpdateVolume(bool changed, Slider sliderRef ,int type)
    {
        GD.Print("Ah");

        switch(type)
        {
            case 0:
                AudioManager.SetMasterVolume((float)sliderRef.Value);
                break;

            case 1:
                AudioManager.SetSFXVolume((float)sliderRef.Value);
                break;

            case 2:
                AudioManager.SetBGMVolume((float)sliderRef.Value);
                break;
        }
    }

}
