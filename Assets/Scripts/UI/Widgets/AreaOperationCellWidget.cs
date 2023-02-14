using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class AreaOperationCellWidget : MonoBehaviour, IPointerClickHandler {
    [Header("Display")]
    [SerializeField]
    GameObject _opRootGO;
    [SerializeField]
    TMP_Text _opLabel;
    [SerializeField]
    M8.UI.Graphics.ColorGroup _panelColorGroup;
    [SerializeField]
    GameObject _solvedRootGO;

    [Header("Anchors")] //use for attaching digits and operators
    [SerializeField]
    RectTransform[] _anchors;

    [Header("Data")]
    string _opFormat = "{0} {1} {2}";

    [Header("Signal Invokes")]
    public SignalAreaOperationCellWidget signalInvokeClick;

    public int row { get; private set; }
    public int col { get; private set; }
    public AreaOperation.Cell cellData { get; private set; }

    public bool operationVisible {
        get { return _opRootGO ? _opRootGO.activeSelf : false; }
        set {
            if(_opRootGO)
                _opRootGO.SetActive(value);
        }
    }

    public bool solvedVisible {
        get { return _solvedRootGO ? _solvedRootGO.activeSelf : false; }
        set {
            if(_solvedRootGO)
                _solvedRootGO.SetActive(value);
        }
    }

    public bool interactable {
        get { return mGraphicRoot ? mGraphicRoot.raycastTarget : false; }
        set {
            if(mGraphicRoot)
                mGraphicRoot.raycastTarget = value;
        }
    }

    public M8.UI.Graphics.ColorGroup panelColorGroup {
        get {
            return _panelColorGroup;
        }
    }

    public RectTransform rectTransform {
        get {
            return mRectTrans;
        }
    }

    private RectTransform mRectTrans;
    private Graphic mGraphicRoot;

    private Dictionary<string, RectTransform> mAnchors;

    private bool mIsInit;

    public RectTransform GetAnchor(string anchorName) {
        if(mAnchors == null)
            return null;

        RectTransform ret;
        mAnchors.TryGetValue(anchorName, out ret);

        return ret;
    }

    public void SetCellIndex(int aRow, int aCol) {
        row = aRow;
        col = aCol;

        Init();
    }

    public void ApplyCell(AreaOperation.Cell cell) {
        cellData = cell;

        RefreshDisplay();
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        signalInvokeClick?.Invoke(this);
    }

    private void Init() {
        if(!mIsInit) {
            mRectTrans = GetComponent<RectTransform>();
            mGraphicRoot = GetComponent<Graphic>();

            if(_anchors != null) {
                mAnchors = new Dictionary<string, RectTransform>(_anchors.Length);

                for(int i = 0; i < _anchors.Length; i++) {
                    var anchor = _anchors[i];
                    if(anchor)
                        mAnchors.Add(anchor.name, anchor);
                }
            }
            else
                mAnchors = new Dictionary<string, RectTransform>();

            mIsInit = true;
        }
    }

    private void RefreshDisplay() {
        if(cellData.isSolved) {
            if(_opLabel)
                _opLabel.text = cellData.op.equal.ToString();
        }
        else {
            if(_opLabel)
                _opLabel.text = string.Format(_opFormat, cellData.op.operand1, Operation.GetOperatorTypeChar(cellData.op.op), cellData.op.operand2);
        }
    }
}
