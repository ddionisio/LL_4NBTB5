using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class DigitWidget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    //NOTE: assume that interactive and number root have the same anchor config as the digit widget itself, so changing the size will work properly
    [Header("Display")]
    [SerializeField]
    RectTransform _interactiveRoot;
    [SerializeField]
    GameObject _highlightGO;
    [SerializeField]
    RectTransform _numberRoot;
    [SerializeField]
    TMP_Text _numberLabel;

    [Header("Animation")]
    [SerializeField]
    M8.Animator.Animate _animator;
    [M8.Animator.TakeSelector(animatorField = "_animator")]
    [SerializeField]
    string _takePulse;

    [Header("SFX")]
    [M8.SoundPlaylist]
    [SerializeField]
    string _sfxClick;

    public int index { get; private set; }
    public int number {
        get { return mNumber; }
        set {
            if(mNumber != value) {
                mNumber = value;

                if(_numberLabel) _numberLabel.text = mNumber.ToString();
            }
        }
    }

    public bool interactable {
        get { return mGraphicRoot ? mGraphicRoot.raycastTarget : false; }
        set {
            if(mGraphicRoot) {
                mGraphicRoot.raycastTarget = value;

                if(mInteractiveGO)
                    mInteractiveGO.SetActive(value);

                if(value) {
                    if(_interactiveRoot)
                        rectTransform.sizeDelta = _interactiveRoot.sizeDelta;
                }
                else {
                    if(_numberRoot)
                        rectTransform.sizeDelta = _numberRoot.sizeDelta;

                    if(mIsPointerEnter) {
                        mIsPointerEnter = false;
                        RefreshHighlight();
                    }
                }
            }
        }
    }

    public RectTransform rectTransform { get { return mRectTrans; } }

    public RectTransform numberRoot { get { return _numberRoot; } }

    public bool isHighlight {
        get { return mIsHighlight; }
        set {
            mIsHighlight = value;
            RefreshHighlight();
        }
    }

    public event System.Action<int> clickCallback;

    private RectTransform mRectTrans;
    private Graphic mGraphicRoot;
    private GameObject mInteractiveGO;

    private int mNumber;
    private bool mIsInit;

    private bool mIsHighlight;

    private bool mIsPointerEnter;

    private Vector3 mNumberRootLocalPos;

    public void Init(int aInd) {
        index = aInd;

        if(!mIsInit) {
            mRectTrans = GetComponent<RectTransform>();
            mGraphicRoot = GetComponent<Graphic>();

            if(_interactiveRoot)
                mInteractiveGO = _interactiveRoot.gameObject;

            SetNumberEmpty();

            if(_numberRoot)
                mNumberRootLocalPos = _numberRoot.localPosition;

            mIsInit = true;
        }
        else {
            if(_numberRoot) //fail-safe if number root is screwed by something outside before recycled
                _numberRoot.localPosition = mNumberRootLocalPos;
        }

        interactable = false;

        mIsHighlight = false;
        mIsPointerEnter = false;

        RefreshHighlight();
    }

    public void SetNumberEmpty() {
        mNumber = -1;
        if(_numberLabel) _numberLabel.text = "";
    }

    public void PlayPulse() {
        if(_animator && !string.IsNullOrEmpty(_takePulse))
            _animator.Play(_takePulse);
    }

    void OnApplicationFocus(bool focus) {
        if(!focus) {
            mIsPointerEnter = false;
            RefreshHighlight();
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if(!string.IsNullOrEmpty(_sfxClick))
            M8.SoundPlaylist.instance.Play(_sfxClick, false);

        clickCallback?.Invoke(index);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        mIsPointerEnter = true;
        RefreshHighlight();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        mIsPointerEnter = false;
        RefreshHighlight();
    }

    private void RefreshHighlight() {
        if(_highlightGO) {
            _highlightGO.SetActive(mIsHighlight || (mIsPointerEnter && interactable));
        }
    }
}