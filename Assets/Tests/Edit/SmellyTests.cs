using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;

namespace EcoBuilder.Tests
{
    public class SmellyTests // for 'smelly' things that compiler doesn't catch, such as fields assigned in editor and reflection
    {
        [Test]
        public void SceneSerializeFieldsNotNull()
        {
            foreach (var monoB in MonoBehaviour.FindObjectsOfType<MonoBehaviour>())
            {
                AssertSerializeFieldsNotNull(monoB);
            }
        }
        [Test]
        public void PrefabSerializeFieldsNotNull()
        {
            var paths = AssetDatabase.GetAllAssetPaths().Where(x=>x.Contains(".prefab"));
            foreach (var path in paths)
            {
                var obj = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
                foreach (var monoB in obj.GetComponents<MonoBehaviour>())
                {
                    AssertSerializeFieldsNotNull(monoB);
                }
            }
        }
        private void AssertSerializeFieldsNotNull(MonoBehaviour monoB)
        {
            Type type = monoB.GetType();
            if (!type.Namespace.Contains("EcoBuilder")) {
                return;
            }
            // var sb = new StringBuilder($"{type.Name} {monoB.name}: ");
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                        .Where(f=> Attribute.IsDefined(f, typeof(SerializeField))))
            {
                // sb.Append($"{field.Name} {field.GetValue(monoB)}");
                Assert.IsNotNull(field.GetValue(monoB), $"SerializeField {field.Name} in {monoB.name} is null");
            }
            // Debug.Log(sb.ToString());
        }

        [Test]
        public void SmellTutorialSmellyListeners()
        {
            // TODO:
        }


        [Test]
        public void VectorX()
        {
            Stopwatch sw = new Stopwatch();
            
            sw.Start();
            float total2 = 0;
            for (int i=0; i<1000000; i++)
            {
                Vector2 foo = UnityEngine.Random.insideUnitCircle;
                total2 += foo.magnitude;
            }
            sw.Stop();
            UnityEngine.Debug.Log($"total2={total2}, elapsed={sw.Elapsed}");
            
            sw.Restart();
            float total3 = 0;
            for (int i=0; i<1000000; i++)
            {
                Vector3 foo = UnityEngine.Random.insideUnitSphere;
                total3 += foo.magnitude;
            }
            sw.Stop();
            UnityEngine.Debug.Log($"total3={total3}, elapsed={sw.Elapsed}");

            sw.Restart();
            float total4 = 0;
            for (int i=0; i<1000000; i++)
            {
                Vector3 foo = UnityEngine.Random.insideUnitSphere;
                // Vector2 foo = UnityEngine.Random.insideUnitCircle;
                total4 += Mathf.Sqrt(foo.x*foo.x + foo.y*foo.y);
            }
            sw.Stop();
            UnityEngine.Debug.Log($"total4={total4}, elapsed={sw.Elapsed}");

            sw.Restart();
            float total5 = 0;
            for (int i=0; i<1000000; i++)
            {
                Vector3 foo = UnityEngine.Random.insideUnitSphere;
                // foo.z = 0;
                // total5 += foo.magnitude;
                // Vector2 foo = UnityEngine.Random.insideUnitCircle;
                total5 += Mathf.Sqrt(foo.x*foo.x + foo.y*foo.y + foo.z*foo.z);
            }
            sw.Stop();
            UnityEngine.Debug.Log($"total5={total5}, elapsed={sw.Elapsed}");
        }
    }
}
