using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class DigitWidget : MonoBehaviour, IPointerClickHandler {
    //NOTE: assume that interactive and number root have the same anchor config as the digit widget itself, so changing the size will work properly
    [Header("Display")]
    [SerializeField]
    RectTransform _interactiveRoot;
    [SerializeField]
    RectTransform _numberRoot;
    [SerializeField]
    TMP_Text _numberLabel;

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
                }
            }
        }
    }

    public RectTransform rectTransform {
        get {
            return mRectTrans;
        }
    }

    public event System.Action<int> clickCallback;

    private RectTransform mRectTrans;
    private Graphic mGraphicRoot;
    private GameObject mInteractiveGO;

    private int mNumber;
    private bool mIsInit;

    public void Init(int aInd) {
        index = aInd;

        if(!mIsInit) {
            mRectTrans = GetComponent<RectTransform>();
            mGraphicRoot = GetComponent<Graphic>();

            if(_interactiveRoot)
                mInteractiveGO = _interactiveRoot.gameObject;

            mIsInit = true;
        }

        interactable = false;
    }

    public void SetNumberEmpty() {
        mNumber = 0;
        if(_numberLabel) _numberLabel.text = "";
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        clickCallback?.Invoke(index);
    }
}