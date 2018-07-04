using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Parameterizer : MonoBehaviour {

    class Species
    {
        public Species(float bodyMass, string name)
        {
            BodyMass = bodyMass;
            Name = name;
        }

        public float BodyMass { get; private set; }
        public string Name { get; private set; }
    }

    [SerializeField] Text nameText;
    [SerializeField] Slider massSlider;
    [SerializeField] Button spawnButton;
    //[SerializeField] Slider attackSlider;
    //[SerializeField] Slider defenceSlider;

    //private readonly Func<int, double> r_i = i => i == 0 ? .1 : -.4;
    //private readonly Func<int, double> a_ii = i => .01;
    //private readonly Func<int, int, double> a_ij = (i, j) => .02;
    //private readonly Func<int, int, double> e_ij = (i, j) => 1


    Dictionary<int, Species> allSpecies = new Dictionary<int, Species>();
    public void AddSpecies(int idx)
    {
        allSpecies[idx] = new Species(massSlider.value, GenerateName());
    }
    public void RemoveSpecies(int idx)
    {
        allSpecies.Remove(idx);
    }

    public void InspectSpecies(int idx)
    {
        nameText.text = allSpecies[idx].Name;
        massSlider.value = allSpecies[idx].BodyMass;
        massSlider.interactable = false;
        spawnButton.interactable = false;
    }
    public void Uninspect()
    {
        nameText.text = "none selected";
        massSlider.interactable = true;
        spawnButton.interactable = true;
    }

    public float GetGrowth(int idx)
    {
        if (idx == 0)
            return 1;
        else
            return -2;
    }
    public float GetIntraspecific(int idx)
    {
        return -.1f;
    }
    public float GetEfficiency(int resource, int consumer)
    {
        return .5f;
    }
    public float GetInteraction(int resource, int consumer)
    {
        return 1;
    }






    public static string[] adjectives = new string[]{
        // "Doc",
        "Grumpy",
        "Happy",
        "Sleepy",
        "Dopey",
        "Bashful",
        "Sneezy",

        "Tiny",
        "Hungry",
        "Flirtatious",
        "Fire-Breathing",
        "Curious",
        "Brave",
        "Wise",
        "Flying",
        "Mega-Ultra",
        "Invisible",
        "Average",
        "Shy",
    };
    public static string[] nouns = new string[]{
        "Rat",
        "Ox",
        "Tiger",
        "Rabbit",
        "Dragon",
        "Snake",
        "Horse",
        "Sheep",
        "Monkey",
        "Rooster",
        "Dog",
        "Pig",

        "Elephant",
        "Polar Bear",
        "Aardvark",
        "Koala",
        "Platypus",
        "Raccoon",
        "Crocodile",
        "Caterpillar",
        "Sloth",
        "Panda",
        "Chameleon",
        "Alpaca",
        "Wolverine",
        "Bear",
        "Elephant",
        "Chihuahua",
        "Koala",
        "Tyrannosaurus Rex",
        "Velociraptor",
    };
    public static string GenerateName() {
        return adjectives[Random.Range(0,adjectives.Length)] + " " + nouns[Random.Range(0,nouns.Length)];
    }
}
