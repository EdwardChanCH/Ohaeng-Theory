using Godot;
using System;

public class ElementalCount : Node
{
    [Export]
    public Godot.Collections.Array<NodePath> ElementalCountLabelPaths = new Godot.Collections.Array<NodePath>();
    private Godot.Collections.Array<Label> _countLabel = new Godot.Collections.Array<Label> ();


    public override void _Ready()
    {

        foreach (var labelPath in ElementalCountLabelPaths)
        {
            var label = GetNode<Label>(labelPath);

            if (label == null)
            {
                GD.PrintErr("Error: Label Count Contains Invalid Path");
                return;
            }

            _countLabel.Add(label);
        }

        if(_countLabel.Count < 5)
        {
            GD.PrintErr("Error: There Is Not Enought Label");
            return;
        }
    }

    public void _UpdateElement(Globals.Element element, int newCount)
    {
        /* switch (element)
        {
            case Globals.Element.Water:
                UpdateLabel(_countLabel[0], newCount);
                break;

            case Globals.Element.Wood:
                UpdateLabel(_countLabel[1], newCount);
                break;

            case Globals.Element.Fire:
                UpdateLabel(_countLabel[2], newCount);
                break;

            case Globals.Element.Earth:
                UpdateLabel(_countLabel[3], newCount);
                break;

            case Globals.Element.Metal:
                UpdateLabel(_countLabel[4], newCount);
                break;
        } */

        _countLabel[(int)element - 1].Text = $"{newCount} {element}";
    }


    /* private void UpdateLabel(Label label, int count)
    {
        label.Text = count.ToString();
        label.Visible = count > 0;
    } */


}
