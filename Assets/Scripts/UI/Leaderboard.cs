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
            name = levelPrefab.Idx.ToString();
            titleText.text = levelPrefab.Title;
            if (GameManager.Instance.GetHighScoreLocal(levelPrefab.Idx) >= 0)
            {
                lockShade.SetActive(false);
            }
            int playerScore = GameManager.Instance.GetHighScoreLocal(levelPrefab.Idx);
            nameText.text = "loading high scores...\n\n\nyour score";
            scoreText.text = "\n\n\n" + playerScore;
            return Instantiate(levelPrefab, levelParent);
        }
        public void SetFromGameManagerCache()
        {
            var cached = GameManager.Instance.GetCachedLeaderboard(LevelToPlay.Idx);
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
            int playerScore = GameManager.Instance.GetHighScoreLocal(LevelToPlay.Idx);
            scoreText.text += playerScore + "\n" + cached.median;
        }
    }
}