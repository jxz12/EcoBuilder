using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Coin : MonoBehaviour
    {
        Animator anim;
        [SerializeField] Button button;
        void Awake()
        {
            anim = GetComponent<Animator>();
            GetComponentInParent<Canvas>().worldCamera = Camera.main;
            GetComponentInParent<Canvas>().planeDistance = -Camera.main.transform.position.z;

            button.onClick.AddListener(Flip);
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, 110));
        }
        bool heads = false;
        void Flip()
        {
            heads = UnityEngine.Random.Range(0, 2) == 0;
            anim.SetBool("Heads", heads);
            anim.SetTrigger("Flip");
            button.onClick.RemoveListener(Flip);
            button.interactable = false;
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, -110));
        }
        public event Action<bool> OnLanded;
        public event Action OnFinished;
        void Land()
        {
            // TODO: add a nice glowing effect for this
            button.interactable = true;
            button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Continue";
            button.onClick.AddListener(Exit);
            OnLanded.Invoke(heads);
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, 110));
        }
        void Exit()
        {
            button.onClick.RemoveListener(Exit);
            anim.SetTrigger("Exit");
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, -110));
            OnFinished.Invoke();
        }
        public void InitializeFlipped(bool flipped)
        {
            ParentToCorner();
            anim.SetBool("Heads", flipped);
            anim.SetTrigger("Init");
        }
        [SerializeField] RectTransform corner;
        void ParentToCorner()
        {
            transform.SetParent(corner, false);
        }
        public void ShowInCorner()
        {
            StartCoroutine(TweenY(corner, 1, 110));
        }
        public void HideInCorner()
        {
            StartCoroutine(TweenY(corner, 1, -110));
        }
        IEnumerator TweenY(RectTransform toMove, float duration, float yEnd)
        {
            Vector2 startPos = toMove.anchoredPosition;
            Vector2 endPos = new Vector2(toMove.anchoredPosition.x, yEnd);

            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = (Time.time-startTime)/duration;
                // quadratic ease in-out
                if (t < .5f)
                    t = 2*t*t;
                else
                    t = -1 + (4-2*t)*t;

                toMove.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
        }
    }
}