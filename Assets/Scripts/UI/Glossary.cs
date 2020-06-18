using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;

namespace EcoBuilder.UI
{
    public class Glossary : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TMPro.TextMeshProUGUI description;

        Dictionary<string,string> definitions;
        [SerializeField] Color highlightedCol, definitionCol;
        void Awake()
        {
            definitions = new Dictionary<string,string>(){
                {"competitive", $"<color=#{ColorUtility.ToHtmlStringRGB(definitionCol)}>Competitive exclusion</color> is when a species goes extinct because its food source is being eaten too much by another species."},
                {"exclusion", $"<color=#{ColorUtility.ToHtmlStringRGB(definitionCol)}>Competitive exclusion</color> is when a species goes extinct because its food source is being eaten too much by another species."},
                {"apparent", $"<color=#{ColorUtility.ToHtmlStringRGB(definitionCol)}>Apparent competition</color> is when a species goes extinct because its predator is being fed too much by another species."},
                {"competition", $"<color=#{ColorUtility.ToHtmlStringRGB(definitionCol)}>Apparent competition</color> is when a species goes extinct because its predator is being fed too much by another species."},
                {"chain", $"The <color=#{ColorUtility.ToHtmlStringRGB(definitionCol)}>chain length</color> of a species is its shortest path to any plant."},
                {"biocontrol", $"<color=#{ColorUtility.ToHtmlStringRGB(definitionCol)}>Biocontrol</color> is the act of introducing new species in order to indirectly suppress existing ones. This was done in 1995 in Yellowstone Park, where ecologists introduced wolves to successfully heal its ecosystem."},
            };
        }
        public string HighlightDefinitions(string toReplace)
        {
            var sentence = new StringBuilder();
            var word = new StringBuilder();
            // bool prevhighlighted = false;
            foreach (char c in toReplace)
            {
                if (char.IsLetter(c))
                {
                    word.Append(c);
                }
                else
                {
                    if (definitions.ContainsKey(word.ToString()))
                    {
                        sentence.Append($"<color=#{ColorUtility.ToHtmlStringRGB(highlightedCol)}><u>").Append(word).Append("</u></color>");
                        // prevhighlighted = true;
                    }
                    else
                    {
                        sentence.Append(word);
                        // prevhighlighted = false;
                    }
                    word.Length = 0;
                    sentence.Append(c);
                }
            }
            if (word.Length > 0)
            {
                sentence.Append(word);
            }
            return sentence.ToString();
        }
        public void OnPointerClick(PointerEventData ped)
        {
            int defIdx = TMPro.TMP_TextUtilities.FindIntersectingWord(description, ped.position, null);
            if (defIdx != -1)
            {
                string clickedWord = description.textInfo.wordInfo[defIdx].GetWord().ToLower();
                if (definitions.TryGetValue(clickedWord, out var val))
                {
                    GameManager.Instance.ShowAlert(val);
                }
            }
        }
    }
}