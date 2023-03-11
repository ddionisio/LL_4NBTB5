using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DigitGroupWidget : MonoBehaviour {
    [Header("Template")]
    [SerializeField]
    DigitWidget _digitTemplate;
    [SerializeField]
    int _digitCapacity;

    [Header("Display")]
    [SerializeField]
    Transform _digitContainer;
    [SerializeField]
    bool _hideLeadingZeroes;

    [Header("Animation")]
    [SerializeField]
    M8.Animator.Animate _animator;
    [M8.Animator.TakeSelector(animatorField = "_animator")]
    [SerializeField]
    string _takePulse;

    public int digitCapacity { get { return _digitCapacity; } }
    public int digitCount { get { return mNumberDigitCount; } }

    public int number {
        get { return mNumber; }
        set {
            if(mNumber != value) {
                mNumber = value;
                mNumberDigitCount = Mathf.Clamp(WholeNumber.DigitCount(mNumber), 0, _digitCapacity);

                int numberMod = mNumber;

                for(int i = 0; i < mNumberDigitCount; i++) {
                    var digitWidget = mDigitWidgets[i];
                    if(digitWidget) {
                        digitWidget.number = numberMod % 10;
                        digitWidget.gameObject.SetActive(true);

                        numberMod /= 10;
                    }
                }

                for(int i = mNumberDigitCount; i < _digitCapacity; i++) {
                    var digitWidget = mDigitWidgets[i];
                    if(digitWidget)
                        digitWidget.number = 0;
                }

                if(_hideLeadingZeroes)
                    HideLeadingZeroes();
            }
        }
    }

    public RectTransform rectTransform {
        get {
            if(!mRectTrans)
                mRectTrans = GetComponent<RectTransform>();
            return mRectTrans;
        }
    }

    public event System.Action<int> clickCallback;

    private DigitWidget[] mDigitWidgets;
    private RectTransform mRectTrans;
    
    private int mNumber;
    private int mNumberDigitCount;

    public void Init() {
        if(mDigitWidgets == null) {
            mDigitWidgets = new DigitWidget[_digitCapacity];

            for(int i = 0; i < _digitCapacity; i++) {
                var newDigitWidget = Instantiate(_digitTemplate);

                newDigitWidget.transform.SetParent(_digitContainer, false);

                newDigitWidget.Init(i);

                newDigitWidget.clickCallback += OnDigitClick;

                newDigitWidget.gameObject.SetActive(false);

                mDigitWidgets[i] = newDigitWidget;
            }
        }
    }

    public void SetDigitsEmpty(int aDigitCount) {
        mNumberDigitCount = Mathf.Clamp(aDigitCount, 0, _digitCapacity);

        for(int i = 0; i < aDigitCount; i++) {
            var digitWidget = mDigitWidgets[i];
            if(digitWidget) {
                mDigitWidgets[i].SetNumberEmpty();

                mDigitWidgets[i].gameObject.SetActive(!_hideLeadingZeroes);
            }
        }

        for(int i = aDigitCount; i < mDigitWidgets.Length; i++) {
            var digitWidget = mDigitWidgets[i];
            if(digitWidget)
                digitWidget.gameObject.SetActive(false);
        }
    }

    public void SetDigitEmpty(int digitIndex) {
        if(digitIndex >= _digitCapacity)
            return;

        var digitWidget = mDigitWidgets[digitIndex];
        if(digitWidget)
            digitWidget.SetNumberEmpty();

        RefreshNumberFromDigits();
    }

    public int GetDigitNumber(int digitIndex) {
        if(digitIndex >= mNumberDigitCount)
            return 0;

        var digitWidget = mDigitWidgets[digitIndex];

        return digitWidget ? digitWidget.number : 0;
    }

    public DigitWidget GetDigitWidget(int digitIndex) {
        if(digitIndex >= _digitCapacity)
            return null;

        return mDigitWidgets[digitIndex];
    }

    public RectTransform GetDigitTransform(int digitIndex) {
        if(digitIndex >= _digitCapacity)
            return null;

        var digitWidget = mDigitWidgets[digitIndex];

        return digitWidget ? digitWidget.rectTransform : null;
    }

    public DigitWidget SetDigitNumber(int digitIndex, int digitNumber) {
        if(digitIndex >= _digitCapacity)
            return null;

        var digitWidget = mDigitWidgets[digitIndex];
        if(digitWidget)
            digitWidget.number = digitNumber;

        RefreshNumberFromDigits();

        return digitWidget;
    }

    public void SetDigitInteractive(int digitIndex, bool interactive) {
        if(digitIndex >= _digitCapacity)
            return;

        var digitWidget = mDigitWidgets[digitIndex];
        if(digitWidget)
            digitWidget.interactable = interactive;
    }

    public void SetDigitInteractiveAll(bool interactive) {
        for(int i = 0; i < mDigitWidgets.Length; i++) {
            var widget = mDigitWidgets[i];
            if(widget)
                widget.interactable = interactive;
        }
    }

    public void SetDigitVisible(int digitIndex, bool isVisible) {
        if(digitIndex >= _digitCapacity)
            return;

        var digitWidget = mDigitWidgets[digitIndex];
        if(digitWidget)
            digitWidget.gameObject.SetActive(isVisible);
    }

    public void SetDigitVisibleAll(bool isVisible) {
        for(int i = 0; i < mDigitWidgets.Length; i++) {
            var widget = mDigitWidgets[i];
            if(widget)
                widget.gameObject.SetActive(isVisible);
        }
    }

    public void SetDigitHighlight(int digitIndex, bool isHighlight) {
        if(digitIndex >= _digitCapacity)
            return;

        var digitWidget = mDigitWidgets[digitIndex];
        if(digitWidget)
            digitWidget.isHighlight = isHighlight;
    }

    public void PlayPulse() {
        if(_animator && !string.IsNullOrEmpty(_takePulse))
            _animator.Play(_takePulse);
    }

    void OnDigitClick(int index) {
        clickCallback?.Invoke(index);
    }

    private void RefreshNumberFromDigits() {
        mNumber = 0;

        int digitShift = 1;

        for(int i = 0; i < mDigitWidgets.Length; i++) {
            var digitWidget = mDigitWidgets[i];
            if(digitWidget && digitWidget.number >= 0) {
                var digitNumber = digitWidget.number;

                mNumber += digitNumber * digitShift;
            }

            digitShift *= 10;
        }

        mNumberDigitCount = WholeNumber.DigitCount(mNumber);

        if(_hideLeadingZeroes)
            HideLeadingZeroes();
    }

    private void HideLeadingZeroes() {
        for(int i = mNumberDigitCount; i < _digitCapacity; i++) {
            var digitWidget = mDigitWidgets[i];
            if(digitWidget)
                mDigitWidgets[i].gameObject.SetActive(false);
        }
    }
}
