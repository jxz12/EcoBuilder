using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System;
using System.Collections;

namespace EcoBuilder.UI
{
    public class ReportCard : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI title, rank, current, currentMsg, average, averageMsg;
        [SerializeField] Image points, globe, shade;
        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Button quitBtn, infoBtn;

        [SerializeField] GameObject starsParent, rankParent, currentParent, averageParent;
        [SerializeField] RectTransform rankTextAnchor, prevLevelAnchor, nextLevelAnchor;

        RectTransform rt;
        Canvas canvas;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            canvas = GetComponent<Canvas>();
            WarpText();

            quitBtn.onClick.AddListener(Quit);
            infoBtn.onClick.AddListener(Info);
        }

        [SerializeField] TMPro.TextMeshProUGUI nextLvlMsg;
        [SerializeField] Image nextLvlCompleteIcon;
        Level currLvl, nextLvl;
        public void GiveNavigation(Level currLvl, Level nextLvl)
        {
            // ensure Hide() has been called beforehand
            Assert.IsNull(this.currLvl);
            Assert.IsNull(this.nextLvl);

            Assert.IsNotNull(currLvl);
            var currRT = currLvl.GetComponent<RectTransform>();
            var currSize = currRT.sizeDelta;
            currLvl.transform.SetParent(prevLevelAnchor);
            currRT.localScale = Vector3.one;
            currRT.anchoredPosition = Vector2.zero;
            currRT.sizeDelta = currSize;

            currLvl.SetLocalHighScore();

            if (nextLvl != null)
            {
                var nextRT = nextLvl.GetComponent<RectTransform>();
                var nextSize = nextRT.sizeDelta;
                nextRT.SetParent(nextLevelAnchor);
                nextRT.localScale = Vector3.one;
                nextRT.anchoredPosition = Vector2.zero;
                nextRT.sizeDelta = nextSize;

                nextLvl.Unlock();
                nextLvlMsg.text = "Next Level";
                nextLvlCompleteIcon.enabled = false;
            }
            else
            {
                nextLvlMsg.text = "World Finished!";
                nextLvlCompleteIcon.enabled = true;
            }
            this.currLvl = currLvl;
            this.nextLvl = nextLvl;
        }
        void Quit()
        {
            GameManager.Instance.ReturnToMenu(ClearLeaks, null);
            void ClearLeaks()
            {
                quitBtn.interactable = false;
                if (currLvl != null) {
                    Destroy(currLvl.gameObject);
                }
                if (nextLvl != null) {
                    Destroy(nextLvl.gameObject);
                }
                this.currLvl = null;
                this.nextLvl = null;
            }
        }
        void Info()
        {
            GameManager.Instance.ShowAlert(information);
        }

        [SerializeField] Sprite pointSprite, trophySprite;
        int numStars;
        string information;
        public void SetResults(int? numStars, long? score, long? prevScore, string rank, long? worldAvg, string scoreInfo)
        {
            StopAllCoroutines();

            if (numStars != null) // in case no stars
            {
                starsParent.SetActive(true);
                this.numStars = (int)numStars;
            }
            else
            {
                starsParent.SetActive(false);
            }

            if (rank != null)
            {
                rankParent.SetActive(true);
                this.rank.text = rank;
            }
            else
            {
                rankParent.SetActive(false);
            }

            if (score != null) // in case no score
            {
                currentParent.SetActive(true);
                current.text = ((long)score).ToString("N0");
                if ((long)score > (prevScore??0))
                {
                    currentMsg.text = "You got a new high score!";
                    var margin = currentMsg.margin;
                    margin.z = 40;
                    currentMsg.margin = margin;

                    points.sprite = trophySprite;
                    StartCoroutine(WiggleSprite(points));
                }
                else
                {
                    currentMsg.text = "Well done!";
                    var margin = currentMsg.margin;
                    margin.z = 0;
                    currentMsg.margin = margin;

                    points.sprite = pointSprite;
                    points.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                currentParent.SetActive(false);
            }

            if (worldAvg != null) // in case average was not cached
            {
                averageParent.SetActive(true);
                average.text = ((long)worldAvg).ToString("N0");
                if (score >= worldAvg)
                {
                    averageMsg.text = "You beat the world average!";
                    StartCoroutine(WiggleSprite(globe));
                }
                else
                {
                    averageMsg.text = "World average";
                    globe.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                averageParent.SetActive(false);
            }
            information = scoreInfo;
        }
        public void ShowResults()
        {
            quitBtn.interactable = true;

            StartCoroutine(ShowRoutine(1f, -1000,0, 0,.5f));
            if (starsParent.activeSelf) {
                StartCoroutine(StarRoutine(1, .5f, .5f, numStars));
            }
            if (rankParent.activeSelf) {
                StartCoroutine(RankRoutine(2,4));
            }
        }
        public void ClearNavigation(Level notToDestroy=null)
        {
            // remove any leaks for levels that would never be used again
            if (currLvl != null && currLvl != notToDestroy) {
                Destroy(currLvl.gameObject);
            }
            currLvl = null;
            if (nextLvl != null && nextLvl != notToDestroy) {
                Destroy(nextLvl.gameObject);
            }
            nextLvl = null;
        }
        public void Hide()
        {
            if (canvas.enabled)
            {
                StopAllCoroutines();
                StartCoroutine(ShowRoutine(1f, 0, -1000, .5f, 0));
            }
        }
        IEnumerator ShowRoutine(float duration, float y0, float y1, float a0, float a1, Action OnCompletion=null)
        {
            canvas.enabled = true;
            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = Tweens.CubicInOut((Time.time-startTime)/duration);
                float y = Mathf.Lerp(y0, y1, t);
                float a = Mathf.Lerp(a0, a1, t);

                rt.anchoredPosition = new Vector2(0, y);
                shade.color = new Color(0,0,0,a);
                yield return null;
            }
            rt.anchoredPosition = new Vector2(0, y1);
            shade.color = new Color(0,0,0,a1);
            if (a1 == 0) {
                canvas.enabled = false;
            }
            OnCompletion?.Invoke();
        }
        IEnumerator StarRoutine(float delay1, float delay2, float delay3, int numStars)
        {
            star1.SetTrigger("Reset");
            star1.SetBool("Filled", false);
            star2.SetTrigger("Reset");
            star2.SetBool("Filled", false);
            star3.SetTrigger("Reset");
            star3.SetBool("Filled", false);

            yield return new WaitForSeconds(delay1);
            star1.SetBool("Filled", true);
            yield return new WaitForSeconds(delay2);
            star2.SetBool("Filled", numStars >= 2);
            yield return new WaitForSeconds(delay3);
            star3.SetBool("Filled", numStars >= 3);
        }
        IEnumerator RankRoutine(float duration, int rotations)
        {
            rankTextAnchor.localScale = Vector3.zero;
            float tStart = Time.time;
            while (Time.time < tStart+duration)
            {
                float t = Tweens.CubicInOut((Time.time-tStart)/duration);
                rankTextAnchor.localScale = t * Vector3.one;
                rankTextAnchor.localRotation = Quaternion.Euler(0,0,t*rotations*360);
                yield return null;
            }
            rankTextAnchor.localScale = Vector3.one;
            rankTextAnchor.localRotation = Quaternion.identity;
        }
        IEnumerator WiggleSprite(Image image)
        {
            float startTime = Time.time;
            while (true)
            {
                float t = Time.time - startTime;
                image.transform.localRotation = Quaternion.Euler(0, 0, 10*Mathf.Sin(2*t));
                yield return true;
            }
        }

        // from TMPro namespace
        // public AnimationCurve VertexCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, 2.0f), new Keyframe(0.5f, 0), new Keyframe(0.75f, 2.0f), new Keyframe(1, 0f));
        public AnimationCurve VertexCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 0), new Keyframe(1, 0f));
        public float CurveScale = 1.0f;

        private AnimationCurve CopyAnimationCurve(AnimationCurve curve)
        {
            AnimationCurve newCurve = new AnimationCurve();
            newCurve.keys = curve.keys;
            return newCurve;
        }


        // from textmeshpro examples
        void WarpText()
        {
            VertexCurve.preWrapMode = WrapMode.Clamp;
            VertexCurve.postWrapMode = WrapMode.Clamp;

            //Mesh mesh = m_TextComponent.textInfo.meshInfo[0].mesh;

            Vector3[] vertices;
            Matrix4x4 matrix;

            var m_TextComponent = title;
            m_TextComponent.havePropertiesChanged = true; // Need to force the TextMeshPro Object to be updated.
            CurveScale *= 10;
            float old_CurveScale = CurveScale;
            AnimationCurve old_curve = CopyAnimationCurve(VertexCurve);


            old_CurveScale = CurveScale;
            old_curve = CopyAnimationCurve(VertexCurve);

            m_TextComponent.ForceMeshUpdate(); // Generate the mesh and populate the textInfo with data we can use and manipulate.

            TMPro.TMP_TextInfo textInfo = m_TextComponent.textInfo;
            int characterCount = textInfo.characterCount;

            if (characterCount == 0) return;

            //vertices = textInfo.meshInfo[0].vertices;
            //int lastVertexIndex = textInfo.characterInfo[characterCount - 1].vertexIndex;

            float boundsMinX = m_TextComponent.bounds.min.x;  //textInfo.meshInfo[0].mesh.bounds.min.x;
            float boundsMaxX = m_TextComponent.bounds.max.x;  //textInfo.meshInfo[0].mesh.bounds.max.x;



            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the index of the mesh used by this character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                vertices = textInfo.meshInfo[materialIndex].vertices;

                // Compute the baseline mid point for each character
                Vector3 offsetToMidBaseline = new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine);
                //float offsetY = VertexCurve.Evaluate((float)i / characterCount + loopCount / 50f); // Random.Range(-0.25f, 0.25f);

                // Apply offset to adjust our pivot point.
                vertices[vertexIndex + 0] += -offsetToMidBaseline;
                vertices[vertexIndex + 1] += -offsetToMidBaseline;
                vertices[vertexIndex + 2] += -offsetToMidBaseline;
                vertices[vertexIndex + 3] += -offsetToMidBaseline;

                // Compute the angle of rotation for each character based on the animation curve
                float x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX); // Character's position relative to the bounds of the mesh.
                float x1 = x0 + 0.0001f;
                float y0 = VertexCurve.Evaluate(x0) * CurveScale;
                float y1 = VertexCurve.Evaluate(x1) * CurveScale;

                Vector3 horizontal = new Vector3(1, 0, 0);
                //Vector3 normal = new Vector3(-(y1 - y0), (x1 * (boundsMaxX - boundsMinX) + boundsMinX) - offsetToMidBaseline.x, 0);
                Vector3 tangent = new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) - new Vector3(offsetToMidBaseline.x, y0);

                float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * 57.2957795f;
                Vector3 cross = Vector3.Cross(horizontal, tangent);
                float angle = cross.z > 0 ? dot : 360 - dot;

                matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                vertices[vertexIndex + 0] += offsetToMidBaseline;
                vertices[vertexIndex + 1] += offsetToMidBaseline;
                vertices[vertexIndex + 2] += offsetToMidBaseline;
                vertices[vertexIndex + 3] += offsetToMidBaseline;
            }


            // Upload the mesh with the revised information
            m_TextComponent.UpdateVertexData();
        }
    }
}