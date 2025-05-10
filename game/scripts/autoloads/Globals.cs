using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// This singleton class contains global constants.
// Note: Godot autoload requires Node type.
public class Globals : Node
{
    [Signal]
    public delegate void GameDataChanged(string key, string value);

    public static Globals Singleton { get; private set; }

    // Game Data (last for the whole game)
    public static Dictionary<string, string> GameData { get; private set; } = new Dictionary<string, string>();

    // Temporary Data (for passing data between screens)
    public static Dictionary<string, string> TempData { get; set; } = new Dictionary<string, string>();

    // Element (ranked by importance)
    public enum Element
    {
        None = 0, // Must be the smallest
        Water = 1,
        Wood = 2,
        Fire = 3,
        Earth = 4,
        Metal = 5 // Most important element
    }

    public static readonly Element[] AllElements = 
    {
        Element.Water,
        Element.Wood,
        Element.Fire,
        Element.Earth,
        Element.Metal
    };

    // Collision Layers
    public const int GroundLayerBit = 0; // Layer 1
    public const int PlayerLayerBit = 1; // Layer 2
    public const int PlayerProjectileLayerBit = 2; // Layer 3
    public const int EnemyLayerBit = 3; // Layer 4
    public const int EnemyProjectileLayerBit = 4; // Layer 5

    public override void _EnterTree()
    {
        base._EnterTree();

        Singleton = this;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GameData.Add("UseMouseDirectedInput", "true");
        GameData.Add("ToggleAttack", "true");
        GameData.Add("ToggleSlow", "true");
    }

    public static void ChangeGameData(string key, string value)
    {
        if (GameData.ContainsKey(key))
        {
            GameData[key] = value;
            Singleton.EmitSignal("GameDataChanged", key, value); // Update the UI accordingly
        }
    }

    public static bool String2Bool(string str)
    {
        return str.ToLower() == "true";
    }
    public static string Bool2String(bool var)
    {
        if (var)
            return "true";
        return "false";
    }

    // Increment element enum
    public static Element NextElement(Element element)
    {
        if (element == Element.None)
        {
            return Element.None; // Undefined
        }

        int newValue = (int)element + 1;

        if (newValue > 5)
        {
            newValue = 1;
        }

        return (Element)newValue;
    }

    // Decrement element enum
    public static Element PreviousElement(Element element)
    {
        if (element == Element.None)
        {
            return Element.None; // Undefined
        }

        int newValue = (int)element - 1;

        if (newValue < 1)
        {
            newValue = 5;
        }

        return (Element)newValue;
    }

    // Find the element that the input element can boost
    public static Element BoostToElement(Element element)
    {
        return NextElement(element);
    }

    // Find the element that can boost the input element
    public static Element BoostByElement(Element element)
    {
        return PreviousElement(element);
    }

    // Find the element that the input element can counter
    public static Element CounterToElement(Element element)
    {
        return NextElement(NextElement(element));
    }

    // Find the element that can counter the input element
    public static Element CounterByElement(Element element)
    {
        return PreviousElement(PreviousElement(element));
    }

    // Find the dominant element based on the count of each type of elements
    // Returns the element with the highest count (or the highest importance if tied)
    public static Element DominantElement(Dictionary<Element, int> elementCounts)
    {
        if (elementCounts.ContainsKey(Element.None))
        {
            return Element.None; // Undefined
        }

        int highestCount = elementCounts.Values.Max();

        if (highestCount == 0)
        {
            return Element.None; // Undefined
        }

        Element dominant = Element.None;

        foreach (Element key in elementCounts.Keys)
        {
            if (elementCounts[key] == highestCount)
            {
                if (key > dominant)
                {
                    dominant = key; // The more important element
                }
            }
        }

        return dominant;
    }

    // Return the sum of all elements
    public static int SumElement(Dictionary<Element, int> elementCounts)
    {
        int count = 0;

        foreach (Element element in AllElements)
        {
            if (elementCounts.ContainsKey(element))
            {
                count += elementCounts[element];
            }
        }

        return count;
    }

    public static string EncodeAllElement(Dictionary<Element, int> elementCounts)
    {
        string encoding = "";

        foreach (Element element in AllElements)
        {
            if (elementCounts.ContainsKey(element))
            {
                encoding += $",{elementCounts[element]}";
            }
        }

        if (encoding.StartsWith(","))
        {
            encoding = encoding.Remove(0, 1);
        }

        return encoding;
    }

    public static Dictionary<Element, int> DecodeAllElement(string encoding)
    {
        Dictionary<Element, int> elementCounts = new Dictionary<Element, int>();

        foreach (Element element in AllElements)
        {
            elementCounts[element] = 0;
        }

        // - - - Start parsing data - - -

        String[] parts = encoding.Split(",");

        if (parts.Length != AllElements.Length)
        {
            GD.Print($"Warning: Failed to parse element data '{encoding}'.");
            return elementCounts;
        }

        foreach (Element element in AllElements)
        {
            int count = 0;

            if (Int32.TryParse(parts[(int)element - 1], out count))
            {
                elementCounts[element] += count;
            }
        }

        return elementCounts;
    }

}