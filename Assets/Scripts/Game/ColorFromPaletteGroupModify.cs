using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorFromPaletteGroupModify : MonoBehaviour {
    public struct ColorFromPaletteData {
        public float brightness;
        public float alpha;
    }
        
    public float brightness {
        get { return mBrightness; }
        set {
            if(mBrightness != value) {
                mBrightness = value;
                Apply();
            }
        }
    }

    public float alpha {
        get { return mAlpha; }
        set {
            if(mAlpha != value) {
                mAlpha = value;
                Apply();
            }
        }
    }

    private M8.ColorFromPaletteBase[] mColorFromPaletteItems;
    private ColorFromPaletteData[] mColorFromPaletteDefaults;

    private float mBrightness = 1.0f; //scale only for now
    private float mAlpha = 1.0f; //scale only for now

    private bool mIsApplied = false;

    public void Init() {
        Revert();

        mColorFromPaletteItems = GetComponentsInChildren<M8.ColorFromPaletteBase>(true);

        InitDefaultData();
    }

    public void Revert() {
        if(mIsApplied) {
            mIsApplied = false;

            if(mColorFromPaletteItems == null || mColorFromPaletteDefaults == null)
                return;

            for(int i = 0; i < mColorFromPaletteItems.Length; i++) {
                var itm = mColorFromPaletteItems[i];
                if(itm) {
                    itm.brightness = mColorFromPaletteDefaults[i].brightness;
                    itm.alpha = mColorFromPaletteDefaults[i].alpha;
                }
            }

            mBrightness = 1.0f;
            mAlpha = 1.0f;
        }
    }

    void OnDestroy() {
        Revert();
    }

    void Awake() {
        if(mColorFromPaletteItems == null || mColorFromPaletteItems.Length == 0)
            Init();
    }

    private void Apply() {
        if(mColorFromPaletteItems == null || mColorFromPaletteItems.Length == 0 || (mColorFromPaletteDefaults != null && mColorFromPaletteItems.Length != mColorFromPaletteDefaults.Length))
            Init();
        else if(mColorFromPaletteDefaults == null)
            InitDefaultData();

        for(int i = 0; i < mColorFromPaletteItems.Length; i++) {
            var itm = mColorFromPaletteItems[i];
            if(itm) {
                itm.brightness = mColorFromPaletteDefaults[i].brightness * mBrightness;
                itm.alpha = mColorFromPaletteDefaults[i].alpha * mAlpha;
            }
        }

        mIsApplied = true;
    }

    private void InitDefaultData() {
        mColorFromPaletteDefaults = new ColorFromPaletteData[mColorFromPaletteItems.Length];

        for(int i = 0; i < mColorFromPaletteItems.Length; i++) {
            mColorFromPaletteDefaults[i].brightness = mColorFromPaletteItems[i].brightness;
            mColorFromPaletteDefaults[i].alpha = mColorFromPaletteItems[i].alpha;
        }
    }
}
