using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class DigitWidget : MonoBehaviour, IPointerClickHandler {
    [Header("Display")]
    GameObject _interactiveDisplayGO;
    [SerializeField]
    Vector2 _nonInteractiveSizeDelta;
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
            if(mGraphicRoot)
                mGraphicRoot.raycastTarget = value;

            if(_interactiveDisplayGO)
                _interactiveDisplayGO.SetActive(value);

            rectTransform.sizeDelta = value ? mDefaultSizeDelta : _nonInteractiveSizeDelta;
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

    private int mNumber;
    private Vector2 mDefaultSizeDelta;
    private bool mIsInit;

    public void Init(int aInd) {
        index = aInd;

        if(!mIsInit) {
            mRectTrans = GetComponent<RectTransform>();
            mGraphicRoot = GetComponent<Graphic>();

            if(mRectTrans)
                mDefaultSizeDelta = mRectTrans.sizeDelta;
            else
                mDefaultSizeDelta = _nonInteractiveSizeDelta;

            mIsInit = true;
        }
    }

    public void SetNumberEmpty() {
        mNumber = 0;
        if(_numberLabel) _numberLabel.text = "";
    }

    void OnDrawGizmos() {
        var rTrans = GetComponent<RectTransform>();
        if(rTrans) {
            var corners = new Vector3[4];
            rTrans.GetLocalCorners(corners);

            var center = Vector3.Lerp(corners[0], corners[2], 0.5f);

            Vector3 ext = _nonInteractiveSizeDelta * 0.5f;

            corners[0] = center - ext;
            corners[1] = center - ext;
            corners[2] = center + ext;
            corners[3] = center + ext;

            var mtx = rTrans.localToWorldMatrix;

            corners[0] = mtx.MultiplyPoint3x4(corners[0]);
            corners[1] = mtx.MultiplyPoint3x4(corners[1]);
            corners[2] = mtx.MultiplyPoint3x4(corners[2]);
            corners[3] = mtx.MultiplyPoint3x4(corners[3]);

            Gizmos.color = Color.yellow;
            M8.Gizmo.DrawWireRect(corners);
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        clickCallback?.Invoke(index);
    }
}