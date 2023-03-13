using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonInvokeKeyPressed : MonoBehaviour {
    public Button targetButton;
    public KeyCode[] keyCodes;    

    void Update() {
        if(targetButton && targetButton.IsInteractable()) {
            bool isDown = false;
            for(int i = 0; i < keyCodes.Length; i++) {
                if(Input.GetKeyDown(keyCodes[i])) {
                    isDown = true;
                    break;
                }
            }

            if(isDown)
                targetButton.onClick.Invoke();
        }
    }
}
