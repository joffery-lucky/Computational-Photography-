using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CreateMesh))]
public class CreateMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CreateMesh myScript = (CreateMesh)target;
        if(GUILayout.Button("Build Object"))
        {
            myScript.BuildObject();
        }
    }
}
