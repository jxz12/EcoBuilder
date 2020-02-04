using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Levels
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] RectTransform levelParent;
        [SerializeField] TMPro.TextMeshProUGUI titleText, nameText, scoreText;
        [SerializeField] GameObject lockShade;

        public Level LevelToPlay { get { return levelParent.GetComponentInChildren<Level>(); } }
        public Level GiveLevelPrefab(Level levelPrefab)
        {
            name = levelPrefab.Details.idx.ToString();
            titleText.text = levelPrefab.Details.title;
            if (GameManager.Instance.GetHighScoreLocal(levelPrefab.Details.idx) >= 0)
            {
                lockShade.SetActive(false);
            }
            int playerScore = GameManager.Instance.GetHighScoreLocal(levelPrefab.Details.idx);
            nameText.text = "loading...\n\nyou";
            scoreText.text = "\n\n\n" + playerScore;
            return Instantiate(levelPrefab, levelParent);
        }
        public void SetFromGameManagerCache()
        {
            nameText.text = "";
            scoreText.text = "";

            var cached = GameManager.Instance.GetCachedLeaderboard(LevelToPlay.Details.idx);
            for (int i=0; i<3; i++)
            {
                nameText.text += (i+1) + " " + (cached.names.Count>i? cached.names[i] : "n/a") + "\n";
                scoreText.text += (cached.scores.Count>i? cached.scores[i].ToString() : "") + "\n";
            }
            nameText.text += "you\nworld average";
            int playerScore = GameManager.Instance.GetHighScoreLocal(LevelToPlay.Details.idx);
            scoreText.text += playerScore + "\n" + cached.median;
        }
    }
}