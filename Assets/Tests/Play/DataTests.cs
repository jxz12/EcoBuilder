using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using EcoBuilder;

namespace EcoBuilder.Tests
{
    public class DataTests // DataManager tests
    {
        GameManager gm;
        [SetUp]
        public void SetUp()
        {
            var prefab = (GameManager)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Misc/Game Manager.prefab", typeof(GameManager));
            gm = GameObject.Instantiate(prefab);
        }
        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(gm.gameObject);
        }

        [UnityTest]
        public IEnumerator GetRank()
        {
            bool done = false;
            bool successful = false;
            GameManager.Instance.GetSingleRankRemote(101, 10000000, (b,s)=>{ successful=b; done=true; MonoBehaviour.print(s); });
            while (!done) { yield return null; }
            Assert.IsTrue(successful);
        }

        [UnityTest]
        public IEnumerator PopulateDatabaseWithScores()
        {
            var pats = GameObject.FindObjectsOfType<Postman>();
            Assert.IsTrue(pats.Length == 1);
            var pat = pats[0];
            for (int i=0; i<10; i++)
            {
                string name = "alice" + (char)('A'+i);
                GameManager.Instance.RegisterLocal(name, "", name+"@alice.co.uk");
                GameManager.Instance.RegisterRemote((b,s)=>{ Assert.IsTrue(b); MonoBehaviour.print(b+" "+s); }); // assertion probably not necessary
                GameManager.Instance.SetGDPRRemote(true, (b,s)=>{ Assert.IsTrue(b); MonoBehaviour.print(b+" "+s); });
                GameManager.Instance.SetDemographicsRemote(0,1,2);
                for (int j=101; j<=104; j++)
                {
                    GameManager.Instance.SavePlaythroughRemote(j, UnityEngine.Random.Range(10, 10000), "", "");
                }
            }
            while (pat.Busy) { yield return null; }
        }
    }
}