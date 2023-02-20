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

    private int mFillCount;

    public void Init(MistakeInfo mistakeInfo) {
        var mistakeMax = Mathf.Clamp(mistakeInfo.maxMistakeCount, 0, _Items.Length);
        var mistakeCurrent = Mathf.Clamp(mistakeInfo.totalMistakeCount, 0, mistakeMax);

        //setup initial display

        //fill
        mFillCount = mistakeMax - mistakeCurrent;
        for(int i = 0; i < mFillCount; i++) {
            _Items[i].active = true;
            _Items[i].filled = true;
        }

        //empty
        for(int i = mFillCount; i < mistakeMax; i++) {
            _Items[i].active = true;
            _Items[i].filled = false;
        }

        //hide any excess
        for(int i = mistakeMax; i < _Items.Length; i++)
            _Items[i].active = false;
    }

    public void UpdateMistakeCount(MistakeInfo mistakeInfo) {
        var mistakeMax = Mathf.Clamp(mistakeInfo.maxMistakeCount, 0, _Items.Length);
        var mistakeCurrent = Mathf.Clamp(mistakeInfo.totalMistakeCount, 0, mistakeMax);

        var newFillCount = mistakeMax - mistakeCurrent;

        if(newFillCount < mFillCount) { //empty out items            
            for(int i = mFillCount - 1; i >= newFillCount; i--) {
                //TODO: animation from fill to empty
                _Items[i].filled = false;
            }
        }
        else { //fill out new items
            for(int i = mFillCount - 1; i < newFillCount; i++) {
                //TODO: animation from empty to fill
                _Items[i].filled = true;
            }
        }

        mFillCount = newFillCount;
    }
}
