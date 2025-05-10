using Godot;
using System;

public class ElementCircle : Node2D
{
    public void SetElement(Globals.Element element)
    {
        int index = (int)element - 1;
        float angle = (float)index * -72.0f;
        RotationDegrees = angle;

        //Modulate
    }

    public void SetA(float a)
    {
        var color = new Color(Modulate);
        color.a = a;
        Modulate = color;
    }
}
