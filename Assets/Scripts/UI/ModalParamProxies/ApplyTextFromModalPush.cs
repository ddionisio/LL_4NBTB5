using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ApplyTextFromModalPush : MonoBehaviour, M8.IModalPush {
    public string paramKey;
    public TMP_Text label;

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(label) {
            if(parms != null && parms.ContainsKey(paramKey))
                label.text = parms.GetValue<string>(paramKey);
        }
    }
}
