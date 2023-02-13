using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MistakeCounterWidget : MonoBehaviour {
    //TODO: animation, etc.
    [System.Serializable]
    public class ItemData {
        public GameObject rootGO;
        public GameObject filledGO;

        public bool active {
            get { return rootGO ? rootGO.activeSelf : false; }
            set {
                if(rootGO)
                    rootGO.SetActive(value);
            }
        }

        public bool filled {
            get { return filledGO ? filledGO.activeSelf : false; }
            set {
                if(filledGO) {
                    //animation?
                    filledGO.SetActive(value);
                }
            }
        }
    }

    [Header("Display")]
    [SerializeField]
    ItemData[] _Items;

    private int mMistakeCurrentCount;
    private int mMistakeCount;

    public void Init(int mistakeCurrentCount, int mistakeCount) {        
        mMistakeCount = Mathf.Clamp(mistakeCount, 0, _Items.Length);
        mMistakeCurrentCount = Mathf.Clamp(mistakeCurrentCount, 0, mMistakeCount);

        //setup initial display
        int fillCount = mMistakeCount - mMistakeCurrentCount;
        for(int i = 0; i < fillCount; i++) {
            _Items[i].active = true;
            _Items[i].filled = true;
        }

        //hide any excess
        for(int i = mMistakeCount; i < _Items.Length; i++)
            _Items[i].active = false;
    }

    public void SetMistakeCount(int mistakeCurrentCount) {
        int mistakeCountDelta = mistakeCurrentCount - mMistakeCurrentCount;
        if(mistakeCountDelta < 0) { //set some items to empty based on delta
            var count = Mathf.Abs(mistakeCountDelta);
            for(int i = 0; i < count; i++) {
                //TODO: do animation of going empty
                _Items[mistakeCurrentCount - i - 1].filled = false;
            }

            mMistakeCurrentCount = mistakeCurrentCount;
        }
        else if(mistakeCountDelta > 0) { //regenerate, if ever it's a feature
            for(int i = 0; i < mistakeCountDelta && i + mistakeCurrentCount < mMistakeCount; i++) {
                _Items[mistakeCurrentCount + i].filled = true;
            }

            mMistakeCurrentCount = mistakeCurrentCount;
        }
    }
}
