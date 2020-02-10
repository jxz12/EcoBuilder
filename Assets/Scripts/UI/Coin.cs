using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Coin : MonoBehaviour
    {
        [SerializeField] Animator coinAnim;
        [SerializeField] Button button;
        void Awake()
        {
            GetComponent<Canvas>().worldCamera = Camera.main;
            GetComponent<Canvas>().planeDistance = -Camera.main.transform.position.z;

            button.onClick.AddListener(Flip);
        }
        Action<bool> LandedCallback;
        Action FinishedCallback;
        public void Reveal(Action<bool> LandedCallback, Action FinishedCallback)
        {
            this.FinishedCallback = FinishedCallback;
            this.LandedCallback = LandedCallback;
            coinAnim.SetTrigger("Show");
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, 110));
        }
        bool? heads = null;
        void Flip()
        {
            heads = UnityEngine.Random.Range(0, 2) == 0;
            coinAnim.SetBool("Heads", (bool)heads);
            coinAnim.SetTrigger("Flip");
            button.onClick.RemoveListener(Flip);
            button.interactable = false;
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, -110));
            StartCoroutine(WaitThenLand(3));
        }
        IEnumerator WaitThenLand(float delay)
        {
            yield return new WaitForSeconds(delay);
            Assert.IsTrue(heads!=null, "coin not flipped");

            print("TODO: add a nice glowing effect for this");
            button.interactable = true;
            button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Continue";
            button.onClick.AddListener(Exit);

            LandedCallback.Invoke((bool)heads);
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, 110));
        }
        void Exit()
        {
            Assert.IsTrue(heads!=null, "coin not flipped");

            button.interactable = false;
            button.onClick.RemoveListener(Exit);
            coinAnim.SetTrigger("Exit");
            StartCoroutine(TweenY(button.GetComponent<RectTransform>(), 1, -110));
            StartCoroutine(WaitThenParentToCorner(1));
            FinishedCallback.Invoke();
        }
        [SerializeField] RectTransform corner;
        IEnumerator WaitThenParentToCorner(float delay)
        {
            yield return new WaitForSeconds(delay);
            coinAnim.transform.SetParent(corner, false);
        }
        public void ShowInCorner()
        {
            StartCoroutine(TweenY(corner, 1, 110));
        }
        public void HideInCorner()
        {
            StartCoroutine(TweenY(corner, 1, -110));
        }
        public void InitializeFlipped(bool flipped)
        {
            StartCoroutine(WaitThenParentToCorner(0));
            coinAnim.SetBool("Heads", flipped);
            coinAnim.SetTrigger("Init");
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
                if (t < .5f) {
                    t = 2*t*t;
                } else {
                    t = -1 + (4-2*t)*t;
                }
                toMove.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
        }
    }
}