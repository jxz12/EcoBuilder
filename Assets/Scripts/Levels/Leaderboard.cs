using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Levels
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] RectTransform levelParent;
        [SerializeField] TMPro.TextMeshProUGUI scoreText;

        public Level LevelToPlay { get { return levelParent.GetComponentInChildren<Level>(); } }
        public Level GiveLevelPrefab(Level levelPrefab)
        {
            return Instantiate(levelPrefab, levelParent);
        }
        void SetScores()
        {
            // TODO: get global scores from server in Menu
        }
    }
}