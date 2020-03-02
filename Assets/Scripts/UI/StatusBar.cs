using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace EcoBuilder.UI
{
    public class StatusBar : MonoBehaviour
    {
        [SerializeField] Image health, healthBG;
        [SerializeField] TMPro.TextMeshProUGUI traitsText;

        RectTransform rt;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
        }
        Camera eye;
        Transform target;
        public void FollowSpecies(Transform target, int size, int greed)
        {
            SetSize(size);
            SetGreed(greed);
            ShowTraits(true);
            ShowHealth(false);

            this.target = target;
            this.eye = Camera.main;

            // rt.SetParent(target);
            // rt.localScale = new Vector3(.01f, .01f, .01f);
            // rt.localPosition = new Vector3(0, 0, -.5f);
            // rt.localRotation = Quaternion.identity;
        }
        public void ShowTraits(bool visible)
        {
            traitsText.enabled = true;
        }
        public void ShowHealth(bool visible)
        {
            health.enabled = visible;
            healthBG.enabled = visible;
        }
        public void SetHealth(float normalisedHealth)
        {
            Assert.IsFalse(normalisedHealth < -1 || normalisedHealth > 1, $"given health of {normalisedHealth}");

            if (normalisedHealth >= 0)
            {
                health.color = Color.green;
                health.fillAmount = normalisedHealth;
            }
            else
            {
                health.color = Color.grey;
                health.fillAmount = -normalisedHealth;
            }
        }
        public void SetSize(int size)
        {
            traitsText.text = $" {size}";
        }
        public void SetGreed(int greed)
        {
            print("TODO: greed status bar");
        }
        void LateUpdate()
        {
            if (target == null) {
                Destroy(gameObject);
            } else {
                rt.position = eye.WorldToScreenPoint(target.position);
            }
        }
    }
}

/*
using UnityEngine;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] Vector3 offset = new Vector3(.4f,-.4f,-.5f);
        [SerializeField] float width=.15f, height=.04f, depth=.02f;
        MeshFilter mf;

        List<Vector3> verts;
        void Awake()
        {
            mf = GetComponent<MeshFilter>();

            verts = new List<Vector3>()
            {
                new Vector3(-width,-height,-depth),
                new Vector3(-width,-height, depth),
                new Vector3(-width, height, depth),
                new Vector3(-width, height,-depth),

                new Vector3( 0,-height,-depth),
                new Vector3( 0,-height, depth),
                new Vector3( 0, height, depth),
                new Vector3( 0, height,-depth),

                new Vector3( width,-height,-depth),
                new Vector3( width,-height, depth),
                new Vector3( width, height, depth),
                new Vector3( width, height,-depth),
            };
            for (int i=0; i<verts.Count; i++)
                verts[i] += offset;

            mf.mesh.SetVertices(verts);

            mf.mesh.subMeshCount = 2;
            mf.mesh.SetTriangles(new int[]
            {
                0,1,2,
                2,3,0,

                1,0,4,
                4,5,1,
                2,1,5,
                5,6,2,
                3,2,6,
                6,7,3,
                0,3,7,
                7,4,0,

                6,5,4,
                4,7,6,
            }, 0);
            mf.mesh.SetTriangles(new int[]
            {
                4,5,6,
                6,7,4,

                5,4,8,
                8,9,5,
                6,5,9,
                9,10,6,
                7,6,10,
                10,11,7,
                4,7,11,
                11,8,4,

                10,9,8,
                8,11,10,
            }, 1);

            mf.mesh.RecalculateNormals();
        }
        public float TargetHealth { get; set; }
        float health = 0;
        public void TweenHealth(float healthTween)
        {
            health = Mathf.Lerp(health, TargetHealth, healthTween);

            float mid = Mathf.Min(-1.01f + 2.02f*health, 1.01f) * width;
            verts[4] = new Vector3( mid,-height,-depth) + offset;
            verts[5] = new Vector3( mid,-height, depth) + offset;
            verts[6] = new Vector3( mid, height, depth) + offset;
            verts[7] = new Vector3( mid, height,-depth) + offset;

            mf.mesh.SetVertices(verts);
        }
        public void Magnify(float magnification)
        {
            verts = new List<Vector3>()
            {
                magnification * new Vector3(-width,-height,-depth),
                magnification * new Vector3(-width,-height, depth),
                magnification * new Vector3(-width, height, depth),
                magnification * new Vector3(-width, height,-depth),

                magnification * new Vector3( 0,-height,-depth),
                magnification * new Vector3( 0,-height, depth),
                magnification * new Vector3( 0, height, depth),
                magnification * new Vector3( 0, height,-depth),

                magnification * new Vector3( width,-height,-depth),
                magnification * new Vector3( width,-height, depth),
                magnification * new Vector3( width, height, depth),
                magnification * new Vector3( width, height,-depth),
            };
            for (int i=0; i<verts.Count; i++)
                verts[i] += offset;
            
            mf.mesh.SetVertices(verts);
        }
    }
}
*/