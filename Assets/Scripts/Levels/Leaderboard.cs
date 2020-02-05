using UnityEngine;

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
            nameText.text = "loading high scores...\n\n\nyour score";
            scoreText.text = "\n\n\n" + playerScore;
            return Instantiate(levelPrefab, levelParent);
        }
        public void SetFromGameManagerCache()
        {
            var cached = GameManager.Instance.GetCachedLeaderboard(LevelToPlay.Details.idx);
            if (cached == null) {
                return;
            }
            nameText.text = "";
            scoreText.text = "";
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