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

    public void Init(MistakeInfo mistakeInfo) {
        var mistakeMax = Mathf.Clamp(mistakeInfo.maxMistakeCount, 0, _Items.Length);

        mMistakeCurrentCount = Mathf.Clamp(mistakeInfo.totalMistakeCount, 0, mistakeMax);

        //setup initial display

        //fill
        int fillCount = mistakeMax - mMistakeCurrentCount;
        for(int i = 0; i < fillCount; i++) {
            _Items[i].active = true;
            _Items[i].filled = true;
        }

        //empty
        for(int i = fillCount; i < mistakeMax; i++) {
            _Items[i].active = true;
            _Items[i].filled = false;
        }

        //hide any excess
        for(int i = mistakeMax; i < _Items.Length; i++)
            _Items[i].active = false;
    }

    public void UpdateMistakeCount(MistakeInfo mistakeInfo) {
        int newMistakeCount = mistakeInfo.totalMistakeCount;

        int mistakeCountDelta = newMistakeCount - mMistakeCurrentCount;
        if(mistakeCountDelta < 0) { //set some items to empty based on delta
            var count = Mathf.Abs(mistakeCountDelta);
            for(int i = 0; i < count; i++) {
                //TODO: do animation of going empty
                _Items[newMistakeCount - i - 1].filled = false;
            }
        }
        else if(mistakeCountDelta > 0) { //regenerate, if ever it's a feature
            var mistakeMax = Mathf.Clamp(mistakeInfo.maxMistakeCount, 0, _Items.Length);

            for(int i = 0; i < mistakeCountDelta && i + newMistakeCount < mistakeMax; i++) {
                _Items[i + newMistakeCount].filled = true;
            }
        }

        mMistakeCurrentCount = newMistakeCount;
    }
}
