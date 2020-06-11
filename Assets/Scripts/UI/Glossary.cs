using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Glossary : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TMPro.TextMeshProUGUI description;

        readonly Dictionary<string,string> definitions = new Dictionary<string,string>(){
            {"competitive", "competitive exclusion is cool"},
            {"exclusion", "competitive exclusion is really cool"},
            {"apparent", "so is apparent competition lol"},
            {"competition", "so is apparent competition ye"},
        };
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