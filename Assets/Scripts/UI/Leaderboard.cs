using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI title, topScores, lowerScores;
        [SerializeField] Button moreButton;
        void Awake()
        {
            moreButton.onClick.AddListener(GetMoreTopScores);
            topPanelLayout.enabled = true;
            topPanelFitter.enabled = true;
        }
        int? currentIdx = null;
        string topScoresText = "";
        int topScoresShowing = 0;
        public void SwitchLevel(int levelIdx, string levelTitle)
        {
            currentIdx = levelIdx;
            title.text = levelTitle;

            topScoresShowing = 0;
            topScoresText = "";
            topScores.text = "loading...";
            topPanelLayout.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            ForceUpdateLayout();
            
            moreButton.interactable = true;
            GameManager.Instance.GetRankedScoresRemote(levelIdx, 0, 10, UpdateTopScores);

            var lowerScoresText = new StringBuilder();
            long? playerScore = GameManager.Instance.GetHighScoreLocal(levelIdx);
            if (playerScore != null) {
                lowerScoresText.Append($"You: {(playerScore??0).ToString("N0")}\n");
            }
            long? median = GameManager.Instance.GetCachedMedian(levelIdx);
            if (median != null) {
                lowerScoresText.Append($"Average: {(median??0).ToString("N0")}");
            }
            lowerScores.text = lowerScoresText.ToString();
        }
        [SerializeField] VerticalLayoutGroup topPanelLayout;
        [SerializeField] ContentSizeFitter topPanelFitter;
        void UpdateTopScores(bool successful, string newScoresText)
        {
            if (!successful) {
                topScores.text = topScoresText + "Please try again later.";
            }
            if (newScoresText == "")
            {
                moreButton.interactable = false;
                topScores.text = topScoresText + "No more scores!";
                return;
            }
            topScoresText += newScoresText;
            topScores.text = topScoresText;

            // assume each row is a new score
            foreach (char c in newScoresText)  {
                if (c == '\n') {
                    topScoresShowing += 1;
                }
            }
            ForceUpdateLayout();
        }
        void ForceUpdateLayout()
        {
            Canvas.ForceUpdateCanvases();
            topPanelLayout.CalculateLayoutInputVertical();
            topPanelLayout.SetLayoutVertical();
            topPanelFitter.SetLayoutVertical();
        }
        public void GetMoreTopScores()
        {
            Assert.IsFalse(currentIdx==null, "no level selected");

            topScores.text += "loading...";
            GameManager.Instance.GetRankedScoresRemote((int)currentIdx, topScoresShowing, 10, UpdateTopScores);
        }
    }
}