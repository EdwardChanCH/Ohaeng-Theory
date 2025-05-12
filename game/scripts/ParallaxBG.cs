using Godot;
using System;

public class ParallaxBG : ParallaxBackground
{
    [Export]
    public int ScrollScale { get; set; } = 100;

    public override void _Process(float delta)
    {
        base._Process(delta);

        ScrollOffset = new Vector2(ScrollOffset.x - ScrollScale * delta, 0);
    }

}
