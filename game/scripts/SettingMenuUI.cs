using Godot;
using System;

public class SettingMenuUI : Node
{

    [Export]
    public NodePath MouseDirectedInputCheckBoxPath = new NodePath();
    private CheckBox _MouseDirectedInputCheckBox;

    [Export]
    public NodePath ToggleAttackCheckBoxPath = new NodePath();
    private CheckBox _ToggleAttackCheckBox;

    [Export]
    public NodePath ToggleSlowCheckBoxPath = new NodePath();
    private CheckBox _ToggleSlowCheckBox;

    [Export]
    public NodePath MasterSliderPath = new NodePath();
    private Slider _MasterSlider;

    [Export]
    public NodePath SFXSliderPath = new NodePath();
    private Slider _SFXSlider;

    [Export]
    public NodePath MusicSliderPath = new NodePath();
    private Slider _MusicSlider;

    public override void _Ready()
    {
        _MouseDirectedInputCheckBox = GetNode<CheckBox>(MouseDirectedInputCheckBoxPath);
        _ToggleAttackCheckBox = GetNode<CheckBox>(ToggleAttackCheckBoxPath);
        _ToggleSlowCheckBox = GetNode<CheckBox>(ToggleSlowCheckBoxPath);
        _MasterSlider = GetNode<Slider>(MasterSliderPath);
        _SFXSlider = GetNode<Slider>(SFXSliderPath);
        _MusicSlider = GetNode<Slider>(MusicSliderPath);



        _MasterSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(0));
        _SFXSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(1));
        _MusicSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(2));


        var masterArg = new Godot.Collections.Array(_MasterSlider, 0);
        _MasterSlider.Connect("drag_ended", this, "UpdateVolume", masterArg);

        var sfxArg = new Godot.Collections.Array(_SFXSlider, 1);
        _SFXSlider.Connect("drag_ended", this, "UpdateVolume", sfxArg);

        var bgmArg = new Godot.Collections.Array(_MusicSlider, 2);
        _MusicSlider.Connect("drag_ended", this, "UpdateVolume", bgmArg);
    }

    public void UpdateSettings()
    {

    }

    public void _OnBackButtonPressed()
    {
        QueueFree();
    }

    private void UpdateVolume(bool changed, Slider sliderRef, int type)
    {
        GD.Print("Ah");

        switch (type)
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
