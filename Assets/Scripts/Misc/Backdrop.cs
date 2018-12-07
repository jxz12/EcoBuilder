﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Backdrop : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] UnityEvent ClickedEvent;

    public void OnPointerClick(PointerEventData ped)
    {
        ClickedEvent.Invoke();
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
