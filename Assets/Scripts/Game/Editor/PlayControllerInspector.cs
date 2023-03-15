using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayController))]
public class PlayControllerInspector : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if(Application.isPlaying) {
            var dat = this.target as PlayController;

            M8.EditorExt.Utility.DrawSeparator();

            if(GUILayout.Button("Clear Blobs"))
                dat.DebugClearActiveBlobs();

            if(GUILayout.Button("Auto Connect Blob"))
                dat.DebugAutoConnect();

            if(GUILayout.Button("Decrement Combo"))
                dat.DebugComboSubtract();
        }
    }
}