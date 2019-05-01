﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JonnySpecies : MonoBehaviour
{
    [SerializeField] MeshFilter body, eyes, nose, mouth;
    public void SetBodyMesh(Mesh mesh)
    {
        body.mesh = mesh;
    }
    public void SetEyesMesh(Mesh mesh)
    {
        eyes.mesh = mesh;
    }
    public void SetNoseMesh(Mesh mesh)
    {
        nose.mesh = mesh;
    }
    public void SetMouthMesh(Mesh mesh)
    {
        mouth.mesh = mesh;
    }
}
