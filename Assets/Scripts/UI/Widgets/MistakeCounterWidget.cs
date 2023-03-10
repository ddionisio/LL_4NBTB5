using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MistakeCounterWidget : MonoBehaviour {
    [Header("Fill Info")]
    [SerializeField]
    Slider _fillBar;
    [SerializeField]
    DG.Tweening.Ease _fillChangeEase = DG.Tweening.Ease.OutSine;
    [SerializeField]
    float _fillChangeDelay = 0.5f;
    [SerializeField]
    int _fillDangerMinCount = 1;
    [SerializeField]
    GameObject _fillDangerGO;

    [Header("Animation")]
    [SerializeField]
    M8.Animator.Animate _animator;
    [M8.Animator.TakeSelector(animatorField = "_animator")]
    [SerializeField]
    string _takeHurt;

    public bool isBusy { get { return mRout != null; } }

    private DG.Tweening.EaseFunction mFillChangeEaseFunc;

    private Coroutine mRout;

    public void Init(MistakeInfo mistakeInfo) {
        var curMistakeCount = mistakeInfo.totalMistakeCount;
        var maxMistakeCount = mistakeInfo.maxMistakeCount;

        var mistakeFillCount = maxMistakeCount - curMistakeCount;

        var fillVal = ((float)mistakeFillCount) / maxMistakeCount;

        //setup initial display
        _fillBar.normalizedValue = fillVal;

        if(_fillDangerGO)
            _fillDangerGO.SetActive(mistakeFillCount <= _fillDangerMinCount);

        if(mFillChangeEaseFunc == null)
            mFillChangeEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(_fillChangeEase);
    }

    public void UpdateMistakeCount(MistakeInfo mistakeInfo) {
        if(mRout != null)
            StopCoroutine(mRout);

        StartCoroutine(DoUpdate(mistakeInfo));
    }

    void OnDisable() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }

    IEnumerator DoUpdate(MistakeInfo mistakeInfo) {
        var curMistakeCount = mistakeInfo.totalMistakeCount;
        var maxMistakeCount = mistakeInfo.maxMistakeCount;

        var mistakeFillCount = maxMistakeCount - curMistakeCount;

        var curFillVal = _fillBar.normalizedValue;
        var newFillVal = ((float)mistakeFillCount) / maxMistakeCount;

        if(newFillVal < curFillVal) {
            if(_animator && !string.IsNullOrEmpty(_takeHurt))
                _animator.Play(_takeHurt);
        }

        var curTime = 0f;
        while(curTime < _fillChangeDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mFillChangeEaseFunc(curTime, _fillChangeDelay, 0f, 0f);

            _fillBar.normalizedValue = Mathf.Lerp(curFillVal, newFillVal, t);
        }

        if(_fillDangerGO)
            _fillDangerGO.SetActive(mistakeFillCount <= _fillDangerMinCount);

        mRout = null;
    }
}
