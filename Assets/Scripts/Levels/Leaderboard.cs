using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Levels
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] RectTransform levelParent;
        [SerializeField] TMPro.TextMeshProUGUI titleText, scoreText;
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
        public void SetScores(int first, int second, int third)
        {
            scoreText.text = "#1 "+first+"\n#2 "+second+"\n#3 "+third;
        }
    }
}