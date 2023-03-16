using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameData))]
public class GameDataInspector : Editor {
    public int roundCount;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        M8.EditorExt.Utility.DrawSeparator();

        var dat = this.target as GameData;

        GUILayout.Label("Score Calculations");

        roundCount = EditorGUILayout.IntField("Round Count", roundCount);

        int maxScore = dat.GetMaxScore(roundCount);

        EditorGUILayout.LabelField("Max Score", maxScore.ToString());

        for(int i = 0; i < dat.ranks.Length; i++) {
            var rank = dat.ranks[i];
            EditorGUILayout.LabelField(rank.grade, Mathf.RoundToInt(maxScore*rank.scale).ToString());
        }

        //var maxScore = dat.ScoreApply
    }
}