using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;

namespace EcoBuilder.UI
{
    public class Glossary : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TMPro.TextMeshProUGUI description;

        readonly Dictionary<string,string> definitions = new Dictionary<string,string>(){
            {"competitive", "competitive exclusion is cool"},
            {"exclusion", "competitive exclusion is reaaaaaaaaaaaaaaaally really really really really really reaaaaaaaaaaaaaally COOOL LCJLAKSJDLDKFH asdjsdkfjcool"},
            {"apparent", "so is apparent competition lol"},
            {"competition", "so is apparent competition ye"},
            {"chain", "swing low"},
            // {"length", "sweet chariot"},
        };
        [SerializeField] string highlightedColour = "#0000FF";
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
                        sentence.Append($"<color={highlightedColour}><u>").Append(word).Append("</u></color>");
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