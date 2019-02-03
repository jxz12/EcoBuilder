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
            button.interactable = false;
            button.GetComponent<Animator>().SetTrigger("Hatch");
            OnHatched();
        }
        public void Enter()
        {
            button.GetComponent<Animator>().SetTrigger("Enter");
        }
        public void MakeHatchable()
        {
            button.GetComponent<Animator>().SetTrigger("Hatchable");
            button.interactable = true;
        }
        public void Exit()
        {
            button.interactable = false;
            button.GetComponent<Animator>().SetTrigger("Exit");
            // button.GetComponent<Animator>().ResetTrigger("Disabled");
        }
    }
}