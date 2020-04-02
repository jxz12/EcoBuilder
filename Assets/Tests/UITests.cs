using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEditor;

namespace EcoBuilder.Tests
{
    public class UITests
    {
        [Test]
        public void CheckImageMaterialsNotNull()
        {
            foreach (var im in MonoBehaviour.FindObjectsOfType<Image>())
            {
                // smelly but you're smelly
                Assert.IsFalse(im.material.name == "Default UI Material", $"{im.name} has default material");
            }
        }

        [Test]
        public void CheckTextMeshProCharactersExist()
        {
            foreach (var text in MonoBehaviour.FindObjectsOfType<TMPro.TMP_SubMeshUI>())
            {
                Assert.True(false, $"{text.name} is a fallback textmeshpro");
            }
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
