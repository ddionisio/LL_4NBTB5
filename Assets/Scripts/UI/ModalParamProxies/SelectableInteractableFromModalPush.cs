using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectableInteractableFromModalPush : MonoBehaviour, M8.IModalPush {
    public Selectable targetSelectable;
    public bool defaultInteractable;
    public string parmKey; //ensure it is a boolean

    void M8.IModalPush.Push(M8.GenericParams parms) {
        bool isInteractable = defaultInteractable;

        if(parms != null && parms.ContainsKey(parmKey))
            isInteractable = parms.GetValue<bool>(parmKey);

        targetSelectable.interactable = isInteractable;
    }
}
