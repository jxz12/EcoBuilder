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
            moreButton.onClick.AddListener(UpdateTopScores);
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
            topPanelLayout.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            ForceUpdateLayout();

            UpdateTopScores();
            UpdateLowerScores();
        }
        [SerializeField] VerticalLayoutGroup topPanelLayout;
        [SerializeField] ContentSizeFitter topPanelFitter;
        void UpdateTopScores()
        {
            Assert.IsFalse(currentIdx==null);

            int updatingIdx = (int)currentIdx;
            topScores.text = topScoresText + (topScoresText==""? "loading...":$"\nloading...");
            moreButton.interactable = false;
            GameManager.Instance.GetRankedScoresRemote(updatingIdx, topScoresShowing, 10, Update);
            void Update(bool successful, string newScoresText)
            {
                if (currentIdx != updatingIdx) {
                    return;
                }
                if (!successful) {
                    topScores.text = topScoresText + "Please try again later.";
                    moreButton.interactable = true;
                    return;
                }
                topScoresText += topScoresText==""? newScoresText : $"\n{newScoresText}";
                topScores.text = topScoresText;
                int newLines = CountLines(newScoresText);
                topScoresShowing += newLines;
                if (newLines < 10)
                {
                    moreButton.interactable = false;
                    topScores.text = topScoresText==""? "No more scores!" : topScoresText+"\nNo more scores!";
                }
                else
                {
                    moreButton.interactable = true;
                }
                ForceUpdateLayout();
            }
        }
        void UpdateLowerScores()
        {
            Assert.IsFalse(currentIdx==null);

            int updatingIdx = (int)currentIdx;
            long? playerScore = GameManager.Instance.GetHighScoreLocal(updatingIdx);
            long? median = GameManager.Instance.GetCachedMedian(updatingIdx);
            SetText(null);
            if (playerScore != null) {
                GameManager.Instance.GetSingleRankRemote(updatingIdx, (long)playerScore, Update);
            }
            void SetText(string rank)
            {
                var lowerScoresText = new StringBuilder();
                if (rank == null) {
                    lowerScoresText.Append($"You: {(playerScore??0).ToString("N0")}");
                } else {
                    lowerScoresText.Append($"{rank}: {GameManager.Instance.Username} {(playerScore??0).ToString("N0")}");
                }
                if (median != null) {
                    lowerScoresText.Append($"\nAverage: {(median??0).ToString("N0")}");
                }
                lowerScores.text = lowerScoresText.ToString();
            }
            void Update(bool successful, string rank)
            {
                if (!successful || currentIdx != updatingIdx) {
                    return;
                }
                SetText(rank);
            }
        }
        void ForceUpdateLayout()
        {
            Canvas.ForceUpdateCanvases();
            topPanelLayout.CalculateLayoutInputVertical();
            topPanelLayout.SetLayoutVertical();
            topPanelFitter.SetLayoutVertical();
        }
        private static int CountLines(string str)
        {
            Assert.IsNotNull(str);
            if (str == string.Empty) {
                return 0;
            }
            int index = -1;
            int count = 0;
            while (-1 != (index = str.IndexOf('\n', index + 1))) {
                count++;
            }
            return count + 1;
        }
    }
}