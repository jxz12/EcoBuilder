using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlatform : MonoBehaviour
{
	public void Spawn(Piece toSpawn)
    {
        // make things shiny
        //toSpawn.transform.SetParent(transform, false);
        Vector3 oldScale = toSpawn.transform.localScale;
        Quaternion oldRotation = toSpawn.transform.localRotation;
        toSpawn.transform.parent = transform;
        toSpawn.transform.localPosition = Vector3.zero;
        toSpawn.transform.localScale = oldScale;
        toSpawn.transform.localRotation = oldRotation;
    }
}
