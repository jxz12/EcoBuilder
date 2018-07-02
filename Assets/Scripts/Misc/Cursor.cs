using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class Cursor : MonoBehaviour {

	private Image img;
	private RectTransform rt;
	void Awake() {
		img = GetComponent<Image>();
		if (img == null) Debug.LogError("Must have Image attached to cursor");
		rt = GetComponent<RectTransform>();
		if (rt == null) Debug.LogError("Must have RectTransform attached to cursor");
		MatchPivot();
	}

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
		var screenPos = Input.mousePosition;
		transform.position = screenPos;
        if (Input.GetMouseButton(0))
            transform.localScale = new Vector3(1, .9f);
        else
            transform.localScale = Vector3.one;
	}

	void MatchPivot() {
		Vector2 size = img.sprite.bounds.size;
		size *= img.sprite.pixelsPerUnit;
		Vector2 pivot = img.sprite.pivot;
		Vector2 percentPivot = new Vector2(pivot.x / size.x, pivot.y / size.y);

		rt.pivot = percentPivot;
	}
}
