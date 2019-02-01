using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Backdrop : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    [SerializeField] UnityEvent OnClicked;

    public void OnPointerClick(PointerEventData ped)
    {
        OnClicked.Invoke();
    }
	public void OnDrag(PointerEventData ped)
	{

	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
