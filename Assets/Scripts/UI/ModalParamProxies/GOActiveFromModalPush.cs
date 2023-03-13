using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOActiveFromModalPush : MonoBehaviour, M8.IModalPush {
    public GameObject targetGO;
    public string parmKey; //ensure it is a boolean

    void M8.IModalPush.Push(M8.GenericParams parms) {
        bool isActive = false;

        if(parms != null && parms.ContainsKey(parmKey))
            isActive = parms.GetValue<bool>(parmKey);

        targetGO.SetActive(isActive);
    }
}
