using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAnimatorSignals : MonoBehaviour {
    [System.Serializable]
    public class SignalInfo {
        public M8.Signal signal;
        public M8.Animator.Animate animator;
        public string take;

        public void Play() {
            if(animator && !string.IsNullOrEmpty(take))
                animator.Play(take);
        }

        public void SetRegister(bool isRegister) {
            if(signal) {
                if(isRegister)
                    signal.callback += Play;
                else
                    signal.callback -= Play;
            }
        }
    }

    public SignalInfo[] signalPlayTakes;

    void OnDisable() {
        for(int i = 0; i < signalPlayTakes.Length; i++)
            signalPlayTakes[i].SetRegister(false);
    }

    void OnEnable() {
        for(int i = 0; i < signalPlayTakes.Length; i++)
            signalPlayTakes[i].SetRegister(true);
    }
}
