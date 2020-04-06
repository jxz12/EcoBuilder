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
            // var sb = new StringBuilder($"{type.Name} {obj.name}: ");
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                        .Where(f=> Attribute.IsDefined(f, typeof(SerializeField))))
            {
                // sb.Append($"{field.Name} {field.GetValue(obj)}");
                Assert.IsFalse(field.GetValue(monoB)==null, $"SerializeField {field.Name} in {monoB.name} is null");
            }
            // Debug.Log(sb.ToString());
        }

        [Test]
        public void SmellTutorialSmellyListeners()
        {

        }

        // // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // // `yield return null;` to skip a frame.
        // [UnityTest]
        // public IEnumerator UITestsWithEnumeratorPasses()
        // {
        //     // Use the Assert class to test conditions.
        //     // Use yield to skip a frame.
        //     yield return null;
        // }
    }
}
