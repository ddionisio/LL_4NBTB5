using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundWidget : MonoBehaviour {
    [Header("Display")]
    public GameObject activeGO;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeClear;

    public void Setup(bool isActive) {
        if(activeGO)
            activeGO.SetActive(isActive);
    }

    public void Clear() {
        if(animator && !string.IsNullOrEmpty(takeClear)) //assume this will deactivate activeGO
            animator.Play(takeClear);
        else if(activeGO)
            activeGO.SetActive(false);
    }
}
