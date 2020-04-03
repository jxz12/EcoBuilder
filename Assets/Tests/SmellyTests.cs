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
        public void SerializeFieldsNotNull()
        {
            foreach (var obj in MonoBehaviour.FindObjectsOfType<MonoBehaviour>())
            {
                Type type = obj.GetType();
                if (type.Namespace.Contains("EcoBuilder"))
                {
                    // var sb = new StringBuilder($"{type.Name} {obj.name}: ");
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                              .Where(f=> Attribute.IsDefined(f, typeof(SerializeField))))
                    {
                        // sb.Append($"{field.Name} {field.GetValue(obj)}");
                        Assert.IsFalse(field.GetValue(obj)==null, $"SerializeField {field.Name} in {obj.name} is null");
                    }
                    // Debug.Log(sb.ToString());
                }
            }
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
