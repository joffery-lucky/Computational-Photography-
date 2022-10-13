using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CreateObject))]
public class CreateObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CreateObject myScript = (CreateObject)target;
        if(GUILayout.Button("Build Object"))
        {
            myScript.BuildObject();
        }
    }
}
