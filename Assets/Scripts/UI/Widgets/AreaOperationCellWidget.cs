using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class AreaOperationCellWidget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    [Header("Display")]
    [SerializeField]
    GameObject _opRootGO;
    [SerializeField]
    TMP_Text _opLabel;
    [SerializeField]
    GameObject _factorLeftRootGO;
    [SerializeField]
    TMP_Text _factorLeftLabel;
    [SerializeField]
    GameObject _factorRightRootGO;
    [SerializeField]
    TMP_Text _factorRightLabel;
    [SerializeField]
    M8.UI.Graphics.ColorGroup _panelColorGroup;
    [SerializeField]
    GameObject _highlightGO;
    [SerializeField]
    GameObject _solvedRootGO;
    [SerializeField]
    GameObject _errorRootGO;
    [SerializeField]
    float _errorDelay = 0.5f;

    [Header("Anchors")] //use for attaching digits and operators
    [SerializeField]
    RectTransform[] _anchors;

    [Header("Data")]
    string _opFormat = "{0} {1} {2}";

    public event System.Action<AreaOperationCellWidget> clickCallback;

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

    public bool factorLeftVisible {
        get { return _factorLeftRootGO ? _factorLeftRootGO.activeSelf : false; }
        set {
            if(_factorLeftRootGO)
                _factorLeftRootGO.SetActive(value);
        }
    }

    public bool factorRightVisible {
        get { return _factorRightRootGO ? _factorRightRootGO.activeSelf : false; }
        set {
            if(_factorRightRootGO)
                _factorRightRootGO.SetActive(value);
        }
    }

    public bool interactable {
        get { return mGraphicRoot ? mGraphicRoot.raycastTarget : false; }
        set {
            if(mGraphicRoot)
                mGraphicRoot.raycastTarget = value;

            if(!value && mIsPointerEnter) {
                mIsPointerEnter = false;
                RefreshHighlight();
            }
        }
    }

    public bool isHighlight {
        get { return mIsHighlight; }
        set {
            mIsHighlight = value;
            RefreshHighlight();
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

    private bool mIsPointerEnter;
    private bool mIsHighlight;

    private Coroutine mShowErrorRout;

    public void Init() {
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
    }

    public void ApplyCell(AreaOperation.Cell cell, bool ignoreSolved) {
        cellData = cell;

        RefreshDisplay(ignoreSolved);
    }

    public void ShowError() {
        if(_errorRootGO) {
            if(mShowErrorRout != null)
                StopCoroutine(mShowErrorRout);

            mShowErrorRout = StartCoroutine(DoShowError());
        }
    }

    void OnApplicationFocus(bool focus) {
        if(!focus) {
            mIsPointerEnter = false;
            RefreshHighlight();
        }
    }

    void OnDisable() {
        if(mShowErrorRout != null) {
            StopCoroutine(mShowErrorRout);
            mShowErrorRout = null;
        }
    }

    void OnEnable() {
        mIsPointerEnter = false;
        RefreshHighlight();

        if(_errorRootGO)
            _errorRootGO.SetActive(false);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        clickCallback?.Invoke(this);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        mIsPointerEnter = true;
        RefreshHighlight();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        mIsPointerEnter = false;
        RefreshHighlight();
    }

    IEnumerator DoShowError() {
        _errorRootGO.SetActive(true);

        yield return new WaitForSeconds(_errorDelay);

        _errorRootGO.SetActive(false);

        mShowErrorRout = null;
    }

    private void RefreshDisplay(bool ignoreSolved) {
        if(!ignoreSolved && cellData.isSolved) {
            if(_opLabel)
                _opLabel.text = cellData.op.equal.ToString();

            if(_factorLeftLabel)
                _factorLeftLabel.text = "";
            if(_factorRightLabel)
                _factorRightLabel.text = "";
        }
        else {
            if(_opLabel)
                _opLabel.text = string.Format(_opFormat, cellData.op.operand1, Operation.GetOperatorTypeChar(cellData.op.op), cellData.op.operand2);

            if(_factorLeftLabel)
                _factorLeftLabel.text = cellData.op.operand1.ToString();
            if(_factorRightLabel)
                _factorRightLabel.text = cellData.op.operand2.ToString();
        }
    }

    private void RefreshHighlight() {
        if(_highlightGO) {
            _highlightGO.SetActive(mIsHighlight || (mIsPointerEnter && interactable));
        }
    }
}
