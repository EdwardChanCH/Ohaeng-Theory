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

    [Export]
    public NodePath MainMenuButtonPath = new NodePath();
    private Button _MainMenuButton;

    public override void _Ready()
    {
        PlayerCharacter.DisableInput();
        _MouseDirectedInputCheckBox = GetNode<CheckBox>(MouseDirectedInputCheckBoxPath);
        _ToggleAttackCheckBox = GetNode<CheckBox>(ToggleAttackCheckBoxPath);
        _ToggleSlowCheckBox = GetNode<CheckBox>(ToggleSlowCheckBoxPath);
        _MasterSlider = GetNode<Slider>(MasterSliderPath);
        _SFXSlider = GetNode<Slider>(SFXSliderPath);
        _MusicSlider = GetNode<Slider>(MusicSliderPath);

        _MouseDirectedInputCheckBox.Pressed = Globals.String2Bool(Globals.GameData["UseMouseDirectedInput"]);
        _ToggleAttackCheckBox.Pressed = Globals.String2Bool(Globals.GameData["ToggleAttack"]);
        _ToggleSlowCheckBox.Pressed = Globals.String2Bool(Globals.GameData["ToggleSlow"]);
        _MasterSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(0));
        _SFXSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(1));
        _MusicSlider.Value = GD.Db2Linear(AudioServer.GetBusVolumeDb(2));


        var masterArg = new Godot.Collections.Array(_MasterSlider, 0);
        _MasterSlider.Connect("drag_ended", this, "_UpdateVolume", masterArg);

        var sfxArg = new Godot.Collections.Array(_SFXSlider, 1);
        _SFXSlider.Connect("drag_ended", this, "_UpdateVolume", sfxArg);

        var bgmArg = new Godot.Collections.Array(_MusicSlider, 2);
        _MusicSlider.Connect("drag_ended", this, "_UpdateVolume", bgmArg);

        if(ScreenManager.CurrentScreen.Filename == ScreenManager.MainMenuScreenPath)
        {
            _MainMenuButton = GetNode<Button>(MainMenuButtonPath);
            _MainMenuButton.Visible = false;
        }
    }
    public override void _ExitTree()
    {
        PlayerCharacter.EnableInput();
    }

    public void UpdateSettings()
    {
        // TODO
    }

    public void _OnBackButtonPressed()
    {
        QueueFree();
    }

    public void _OnMainMenuButtonPressed()
    {
        ScreenManager.SwitchToNextScreen(ScreenManager.MainMenuScreenPath, GetTree().Root);
        QueueFree();
    }

    private void _UpdateVolume(bool changed, Slider sliderRef, int type)
    {
        GD.Print("Ah"); // TODO

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

    private void _UpdateToggle(bool pressed, string data)
    {
        Globals.ChangeGameData(data, Globals.Bool2String(pressed));
    }
}
