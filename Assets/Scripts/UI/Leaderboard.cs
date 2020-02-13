using UnityEngine;

namespace EcoBuilder.UI
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] RectTransform levelParent;
        [SerializeField] TMPro.TextMeshProUGUI titleText, nameText, scoreText;
        [SerializeField] GameObject lockShade;

        public Level LevelToPlay { get { return levelParent.GetComponentInChildren<Level>(); } }
        public Level GiveLevelPrefab(Level levelPrefab)
        {
            print("TODO: do not show scores if metric is None");
            name = levelPrefab.Details.Idx.ToString();
            titleText.text = levelPrefab.Details.Title;
            if (GameManager.Instance.GetHighScoreLocal(levelPrefab.Details.Idx) >= 0)
            {
                lockShade.SetActive(false);
            }
            int playerScore = GameManager.Instance.GetHighScoreLocal(levelPrefab.Details.Idx);
            nameText.text = "loading high scores...\n\n\nyour score";
            scoreText.text = "\n\n\n" + playerScore;
            return Instantiate(levelPrefab, levelParent);
        }
        public void SetFromGameManagerCache()
        {
            int median = GameManager.Instance.GetLeaderboardMedian(LevelToPlay.Details.Idx);
            var scores = GameManager.Instance.GetLeaderboardScores(LevelToPlay.Details.Idx);
            if (median == -1) {
                return;
            }
            nameText.text = "";
            scoreText.text = "";
            for (int i=0; i<3; i++)
            {
                nameText.text += (i+1) + " " + (scores.Count>i? scores[i].Item1 : "n/a") + "\n";
                scoreText.text += (scores.Count>i? scores[i].Item2.ToString() : "") + "\n";
            }
            nameText.text += "you\nworld average";
            int playerScore = GameManager.Instance.GetHighScoreLocal(LevelToPlay.Details.Idx);
            scoreText.text += playerScore + "\n" + median;
        }
    }
}