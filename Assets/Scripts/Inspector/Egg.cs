using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Inspector
{
    public class Egg : MonoBehaviour
    {
        [SerializeField] Button button;

        public event Action OnHatched;
        
        void Awake()
        {
            button.onClick.AddListener(()=>Hatch());
        }
        public void Hatch()
        {
            button.GetComponent<Animator>().SetTrigger("Hatch");
            OnHatched();
        }
        public void Enter()
        {
            button.interactable = false;
            gameObject.SetActive(true);
        }
        public void MakeHatchable()
        {
            button.interactable = true;
        }
        public void Reset()
        {
            button.interactable = false;
            gameObject.SetActive(false);
        }
    }
}