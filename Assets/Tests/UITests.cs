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
        public void ImageMaterialsNotNull()
        {
            foreach (var im in MonoBehaviour.FindObjectsOfType<Image>())
            {
                // smelly but you're smelly
                Assert.IsFalse(im.material.name == "Default UI Material", $"{im.name} has default material");
            }
        }

        [Test]
        public void NoTMProSubMeshes()
        {
            foreach (var text in MonoBehaviour.FindObjectsOfType<TMPro.TMP_SubMeshUI>())
            {
                Assert.True(false, $"{text.name} is a fallback textmeshpro");
            }
        }
        [Test]
        public void NavigationDisabled()
        {
            foreach (var selectable in MonoBehaviour.FindObjectsOfType<Selectable>())
            {
                Assert.IsTrue(selectable.navigation.mode == Navigation.Mode
                .None, $"{selectable.name} has navigation enabled");
            }
        }
        [Test]
        public void ButtonsAssignedInScript()
        {
            foreach (var button in MonoBehaviour.FindObjectsOfType<Button>())
            {
                int count = button.onClick.GetPersistentEventCount();
                Assert.IsTrue(count == 0, $"Button {button.name} has {count} listeners");
            }
        }
    }
}
