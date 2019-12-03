using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace EcoBuilder.UI
{
    public class Survey : MonoBehaviour
    {
        public event Action OnRegistered;
        public event Action<int> OnAgeSet, OnGenderSet, OnEduSet;
        public void SetAge(int age)
        {
            OnAgeSet.Invoke(age);
        }
        public void SetGender(int gen)
        {
            OnGenderSet.Invoke(gen);
        }
        public void SetEducation(int edu)
        {
            OnEduSet.Invoke(edu);
        }
    }
}