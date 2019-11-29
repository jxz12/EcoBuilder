using UnityEngine;

namespace EcoBuilder.UI
{
    public class Effect : MonoBehaviour
    {
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}