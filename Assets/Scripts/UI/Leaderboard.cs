using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI title, topScores, botScores;
        [SerializeField] Button moreButton;
        void Awake()
        {
            moreButton.onClick.AddListener(GetMoreScores);
            topPanelLayout.enabled = true;
            topPanelFitter.enabled = true;
        }
        int? currentIdx = null;
        string topScoresText = "";
        [SerializeField] int topScoresShowing = 0;
        public void SwitchLevel(int levelIdx, string levelTitle)
        {
            currentIdx = levelIdx;
            title.text = levelTitle;

            topScoresShowing = 0;
            topScoresText = "";
            topScores.text = "loading...";
            moreButton.interactable = true;
            GameManager.Instance.GetRankedScoresRemote(levelIdx, 0, 10, (b,s)=>{ if (b) UpdateScores(s); });
        }
        [SerializeField] VerticalLayoutGroup topPanelLayout;
        [SerializeField] ContentSizeFitter topPanelFitter;
        void UpdateScores(string newScoresText)
        {
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
            
            Canvas.ForceUpdateCanvases();
            topPanelLayout.CalculateLayoutInputVertical();
            topPanelLayout.SetLayoutVertical();
            topPanelFitter.SetLayoutVertical();
        }
        public void GetMoreScores()
        {
            Assert.IsFalse(currentIdx==null, "no level selected");

            topScores.text += "loading...";
            GameManager.Instance.GetRankedScoresRemote((int)currentIdx, topScoresShowing, 10, (b,s)=>{ UpdateScores(s); });
        }

        // [SerializeField] TMPro.TextMeshProUGUI titleText, nameText, scoreText;
        // [SerializeField] GameObject lockShade;

        // public Level LevelToPlay { get { return levelParent.GetComponentInChildren<Level>(); } }
        // public Level GiveLevelPrefab(Level levelPrefab)
        // {
        //     name = levelPrefab.Details.Idx.ToString();
        //     titleText.text = levelPrefab.Details.Title;
        //     if (GameManager.Instance.GetHighScoreLocal(levelPrefab.Details.Idx) >= 0)
        //     {
        //         lockShade.SetActive(false);
        //     }
        //     long playerScore = GameManager.Instance.GetHighScoreLocal(levelPrefab.Details.Idx);
        //     nameText.text = "loading high scores...\n\n\nyour score";
        //     scoreText.text = "\n\n\n" + playerScore;
        //     return Instantiate(levelPrefab, levelParent);
        // }
        // public void SetFromGameManagerCache()
        // {
        //     var scores = GameManager.Instance.GetLeaderboardScores(LevelToPlay.Details.Idx);
        //     if (scores == null) {
        //         // in case it has never been cached
        //         return;
        //     }
        //     nameText.text = "";
        //     scoreText.text = "";
        //     for (int i=0; i<3; i++)
        //     {
        //         nameText.text += (i+1) + " " + (scores.Count>i? scores[i].Item1 : "n/a") + "\n";
        //         scoreText.text += (scores.Count>i? scores[i].Item2.ToString() : "") + "\n";
        //     }
        //     nameText.text += "you\nworld average";
        //     long playerScore = GameManager.Instance.GetHighScoreLocal(LevelToPlay.Details.Idx);

        //     long? median = GameManager.Instance.GetLeaderboardMedian(LevelToPlay.Details.Idx);
        //     scoreText.text += playerScore + "\n" + median;
        // }
    }
}