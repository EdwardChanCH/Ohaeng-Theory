using Godot;
using System;

public class TransitionScreen : CanvasLayer
{
    [Export]
    public NodePath AnimationPlayerPath;
    private AnimationPlayer _animationPlayer;

    public override void _EnterTree()
    {
        _animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
    }

    public void FadeToBlack()
    {
        _animationPlayer.Play("fade_to_black");
    }

    public void FadeToNormal()
    {
        _animationPlayer.Play("fade_to_normal");
    }

    public void _OnAnimationFished(string animationName)
    {
        if (animationName == "fade_to_black")
        {

        }
        else if (animationName == "fade_to_normal")
        {

        }
    }
}
