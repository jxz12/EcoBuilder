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
            return Instantiate(levelPrefab, levelParent);
        }
        public void SetScoreFromGameManagerCache(bool successful, string msg)
        {
            if (successful) {
                var scores = GameManager.Instance.GetLeaderboardLocal(LevelToPlay.Details.idx);
                nameText.text = scores.Item1;
                scoreText.text = scores.Item2;
            }
        }
    }
}