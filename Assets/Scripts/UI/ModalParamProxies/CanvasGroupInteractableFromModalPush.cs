using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasGroupInteractableFromModalPush : MonoBehaviour, M8.IModalPush {
    public CanvasGroup targetCanvasGroup;
    public string parmKey; //ensure it is a boolean

    void M8.IModalPush.Push(M8.GenericParams parms) {
        bool isInteractable = false;

        if(parms != null && parms.ContainsKey(parmKey))
            isInteractable = parms.GetValue<bool>(parmKey);

        targetCanvasGroup.interactable = isInteractable;
    }
}
