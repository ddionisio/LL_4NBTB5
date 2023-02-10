using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class AreaOperationCellWidget : MonoBehaviour, IPointerClickHandler {
    [Header("Display")]    
    [SerializeField]
    TMP_Text _opLabel;
    [SerializeField]
    Image _panel;
    [SerializeField]
    GameObject _solvedRootGO;

    [Header("Data")]
    string _opFormat = "{0} {1} {2}";

    [Header("Signal Invokes")]
    public SignalAreaOperationCellWidget signalInvokeClick;

    public int row { get; private set; }
    public int col { get; private set; }
    public AreaOperation.Cell cellData { get; private set; }

    public RectTransform rectTransform {
        get {
            if(!mRectTrans)
                mRectTrans = GetComponent<RectTransform>();
            return mRectTrans;
        }
    }

    public Graphic graphicRoot {
        get {
            if(!mGraphicRoot)
                mGraphicRoot = GetComponent<Graphic>();
            return mGraphicRoot;
        }
    }

    private RectTransform mRectTrans;
    private Graphic mGraphicRoot;

    public void SetCellIndex(int aRow, int aCol) {
        row = aRow;
        col = aCol;
    }

    public void ApplyCell(AreaOperation.Cell cell) {
        cellData = cell;

        RefreshDisplay();
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        signalInvokeClick?.Invoke(this);
    }

    private void RefreshDisplay() {
        if(cellData.isSolved) {
            if(_opLabel)
                _opLabel.text = cellData.op.equal.ToString();

            if(graphicRoot)
                graphicRoot.raycastTarget = false;

            if(_solvedRootGO)
                _solvedRootGO.SetActive(true);
        }
        else {
            if(_opLabel)
                _opLabel.text = string.Format(_opFormat, cellData.op.operand1, Operation.GetOperatorTypeChar(cellData.op.op), cellData.op.operand2);

            if(graphicRoot)
                graphicRoot.raycastTarget = true;

            if(_solvedRootGO)
                _solvedRootGO.SetActive(false);
        }
    }
}