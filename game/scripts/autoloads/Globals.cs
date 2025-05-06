using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// This singleton class contains global constants.
// Note: Godot autoload requires Node type.
public class Globals : Node
{
    [Signal]
    public delegate void GameDataChanged(string key, string value);

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

    // Collision Layers
    public const int GroundLayerBit = 0; // Layer 1
    public const int PlayerLayerBit = 1; // Layer 2
    public const int PlayerProjectileLayerBit = 2; // Layer 3
    public const int EnemyLayerBit = 3; // Layer 4
    public const int EnemyProjectileLayerBit = 4; // Layer 5

    public static Globals Singleton { get; private set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Singleton = this;
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

    /* 
    // Find the element with the highest importance from a list of elements
    public static Element MostImportantElement(params Element[] elementList)
    {
        int maxValue = (int)Element.Null;
        
        foreach (Element element in elementList)
        {
            if (element == Element.Null)
            {
                return Element.Null;
            }
            
            int value = (int)element;

            if (value > maxValue)
            {
                maxValue = value;
            }
        }

        return (Element)maxValue;
    }
 */

}