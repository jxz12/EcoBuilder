using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlatform : MonoBehaviour
{
    public bool Active { get; private set; } = false;
	public void Spawn(Piece toSpawn)
    {
        gameObject.SetActive(true);
        // make things shiny
        toSpawn.transform.parent = transform;
        toSpawn.transform.localScale = Vector3.one;
        toSpawn.transform.localPosition = Vector3.zero;
        toSpawn.transform.localRotation = Quaternion.identity;
        // make it glow and stuff

        Active = true;
    }
    public void Despawn()
    {
        gameObject.SetActive(false);
        Active = false;
    }
}
