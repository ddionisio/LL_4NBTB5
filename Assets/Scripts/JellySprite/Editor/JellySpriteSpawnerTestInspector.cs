using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(JellySpriteSpawnerTest))]
public class JellySpriteSpawnerTestInspector : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        M8.EditorExt.Utility.DrawSeparator();

        var dat = target as JellySpriteSpawnerTest;

        if(GUILayout.Button("Spawn")) {
            dat.Spawn();
        }

        if(GUILayout.Button("Despawn")) {
            dat.Despawn();
        }
    }
}
