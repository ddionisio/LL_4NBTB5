using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Operation))]
public class OperationInspector : PropertyDrawer {
    private string[] mOpTexts = new string[] { "x", "÷" };
    private int[] mOpVals = new int[] { 0, 1 };
    private const int opValOfs = 1;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var propOp1 = property.FindPropertyRelative("operand1");
        var propOp = property.FindPropertyRelative("op");
        var propOp2 = property.FindPropertyRelative("operand2");

        //hack
        /*if(position.width < 350f) {
            position.x -= 60;
            position.width += 60;
        }

        var scale = 1f / 5f;
        var width = position.width * scale;

        var rect = new Rect(position.position, new Vector2(width, position.height));*/

        var rect = new Rect(position.position, new Vector2(80f, position.height));
                
        //op 1
        propOp1.intValue = EditorGUI.IntField(rect, propOp1.intValue);

        //operator
        var opInd = propOp.enumValueIndex;
        if(opInd > 0)
            opInd -= opValOfs;

        if(opInd >= mOpVals.Length)
            opInd = 0;

        rect.x += 40f;
        propOp.enumValueIndex = EditorGUI.IntPopup(rect, opInd, mOpTexts, mOpVals) + opValOfs;

        //op 2
        rect.x += 40f;
        propOp2.intValue = EditorGUI.IntField(rect, propOp2.intValue);

        EditorGUI.EndProperty();
    }
}