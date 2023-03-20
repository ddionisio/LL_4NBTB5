using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LoLExt {
    public class AnimatorEnterExit : MonoBehaviour {
        [SerializeField]
        GameObject _rootGO;

        public M8.Animator.Animate animator;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeEnter;
        [M8.Animator.TakeSelector(animatorField = "animator")]
        public string takeExit;

        public bool resetOnEnable = false;

        public GameObject rootGO { 
            get {
                if(!_rootGO)
                    _rootGO = gameObject;

                return _rootGO; 
            } 
        }

        public bool isPlaying { get { return animator ? animator.isPlaying : false; } }
        public bool isEntering { get { return animator ? animator.currentPlayingTakeName == takeEnter : false; } }
        public bool isExiting { get { return animator ? animator.currentPlayingTakeName == takeExit : false; } }

        public void Show() {
            rootGO.SetActive(true);
        }

        public void Hide() {
            rootGO.SetActive(false);
        }

        public void Stop() {
            if(animator)
                animator.Stop();
        }

        public void PlayEnter() {
            if(animator && !string.IsNullOrEmpty(takeEnter))
                animator.Play(takeEnter);
        }

        public IEnumerator PlayEnterWait() {
            if(animator && !string.IsNullOrEmpty(takeEnter)) {
                animator.Play(takeEnter);
                while(animator.isPlaying)
                    yield return null;
            }
        }

        public void PlayExit() {
            if(animator && !string.IsNullOrEmpty(takeExit))
                animator.Play(takeExit);
        }

        public IEnumerator PlayExitWait() {
            if(animator && !string.IsNullOrEmpty(takeExit)) {
                animator.Play(takeExit);
                while(animator.isPlaying)
                    yield return null;
            }
        }

        void OnEnable() {
            if(resetOnEnable) {
                if(animator && !string.IsNullOrEmpty(takeEnter))
                    animator.ResetTake(takeEnter);
            }
        }
    }
}