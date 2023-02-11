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
    float _nonInteractiveWidth;
    [SerializeField]
    TMP_Text _numberLabel;

    public event System.Action<int> clickCallback;

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

            var rectSize = rectTransform.sizeDelta;
            rectSize.x = value ? mDefaultRectWidth : _nonInteractiveWidth;
            rectTransform.sizeDelta = rectSize;
        }
    }

    public RectTransform rectTransform {
        get {
            return mRectTrans;
        }
    }

    private RectTransform mRectTrans;
    private Graphic mGraphicRoot;

    private int mNumber;
    private float mDefaultRectWidth;

    public void Init(int aInd) {
        index = aInd;
    }

    void Awake() {
        mRectTrans = GetComponent<RectTransform>();
        mGraphicRoot = GetComponent<Graphic>();

        if(mRectTrans)
            mDefaultRectWidth = mRectTrans.sizeDelta.x;
        else
            mDefaultRectWidth = _nonInteractiveWidth;
    }

    void OnDrawGizmos() {
        var rTrans = GetComponent<RectTransform>();
        if(rTrans) {
            var corners = new Vector3[4];
            rTrans.GetLocalCorners(corners);

            var center = Vector3.Lerp(corners[0], corners[2], 0.5f);

            var extX = _nonInteractiveWidth * 0.5f;

            corners[0].x = center.x - extX;
            corners[1].x = center.x - extX;
            corners[2].x = center.x + extX;
            corners[3].x = center.x + extX;

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