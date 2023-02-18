using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalAttackAreaEvaluate : M8.ModalController, M8.IModalPush, M8.IModalPop {
    [Header("Templates")]
    public AreaOperationCellWidget areaCellTemplate;
    public int areaCellCapacity = 4;

    public Transform cacheRoot;

    [Header("Area Info")]
    public AreaGridControl[] areaGrids;

    [Header("Mistake Display")]
    public MistakeCounterWidget mistakeCounterDisplay;

    [Header("Signal Invoke")]
    public SignalAttackState signalInvokeAttackStateChange;

    [Header("Signal Listen")]
    public M8.SignalFloat signalListenNumpadProceed;

    private AreaOperation mAreaOp;
    private MistakeInfo mMistakeInfo;

    private int mAreaGridInd = -1;

    private AreaOperationCellWidget[,] mAreaCellActives; //[row, col], count is based on row and col count in mAreaOp
    private M8.CacheList<AreaOperationCellWidget> mAreaCellCache;

    private ModalAttackParams mAttackParms;

    private M8.GenericParams mNumpadParms = new M8.GenericParams();

    private bool mIsInit;

    public void Back() {
        Close();

        //return to attack distributive
    }

    public void Proceed() {

    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit) {
            ///////////////////////////////////
            //generate caches from templates

            //area cells
            mAreaCellActives = new AreaOperationCellWidget[areaCellCapacity, areaCellCapacity];
            mAreaCellCache = new M8.CacheList<AreaOperationCellWidget>(areaCellCapacity);

            for(int i = 0; i < areaCellCapacity; i++) {
                var newAreaCell = Instantiate(areaCellTemplate);
                newAreaCell.Init();
                newAreaCell.clickCallback += OnAreaCellClick;

                newAreaCell.rectTransform.SetParent(cacheRoot, false);

                mAreaCellCache.Add(newAreaCell);
            }

            //////////////////////////////////
            //setup area grids

            for(int i = 0; i < areaGrids.Length; i++) {
                var areaGrid = areaGrids[i];

                areaGrid.Init();
                areaGrid.gameObject.SetActive(false);
            }

            mIsInit = true;
        }

        //setup shared data across attack phases
        mAttackParms = parms as ModalAttackParams;
        if(mAttackParms != null) {
            mAreaOp = mAttackParms.GetAreaOperation();
            mMistakeInfo = mAttackParms.GetMistakeInfo();
        }
        else {
            mAreaOp = null;
            mMistakeInfo = null;
        }

        //setup areas
        if(mAreaOp != null) {
            //choose the area grid to work with
            AreaGridControl curAreaGrid = null;
            mAreaGridInd = -1;

            for(int i = 0; i < areaGrids.Length; i++) {
                var areaGrid = areaGrids[i];
                if(areaGrid && areaGrid.numRow == mAreaOp.areaRowCount && areaGrid.numCol == mAreaOp.areaColCount) {
                    curAreaGrid = areaGrid;
                    mAreaGridInd = i;
                    break;
                }
            }

            if(curAreaGrid) {
                //setup initial grid telemetry
                for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                    for(int col = 0; col < mAreaOp.areaColCount; col++) {
                        var cell = mAreaOp.GetAreaOperation(row, col);
                        if(cell.isValid)
                            curAreaGrid.SetAreaScale(row, col, Vector2.one);
                        else
                            curAreaGrid.SetAreaScale(row, col, Vector2.zero);
                    }
                }

                curAreaGrid.RefreshAreas();

                curAreaGrid.gameObject.SetActive(true);

                //insert area widgets
                var gridRoot = curAreaGrid.rectTransform;

                for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                    for(int col = 0; col < mAreaOp.areaColCount; col++) {
                        var cell = mAreaOp.GetAreaOperation(row, col);

                        if(cell.isValid) { //only add valid cells
                            var areaCellWidget = GenerateAreaCell(row, col);

                            areaCellWidget.interactable = !cell.isSolved;
                            areaCellWidget.solvedVisible = cell.isSolved;

                            //TODO: setup panel color

                            areaCellWidget.rectTransform.SetParent(gridRoot, false);
                        
                            areaCellWidget.ApplyCell(cell, false);

                            curAreaGrid.ApplyArea(row, col, areaCellWidget.rectTransform);

                            areaCellWidget.operationVisible = true;
                            areaCellWidget.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
        else
            Debug.LogError("Unable to find matching area grid: [row = " + mAreaOp.areaRowCount + ", col = " + mAreaOp.areaColCount + "]");

        //setup mistake counter
        if(mMistakeInfo != null) {
            if(mistakeCounterDisplay)
                mistakeCounterDisplay.Init(mMistakeInfo);
        }

        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback += OnNumpadProceed;
    }

    void M8.IModalPop.Pop() {
        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback -= OnNumpadProceed;

        ClearAreaCells();

        if(mAreaGridInd != -1) {
            if(areaGrids[mAreaGridInd])
                areaGrids[mAreaGridInd].gameObject.SetActive(false);

            mAreaGridInd = -1;
        }
    }

    void OnAreaCellClick(AreaOperationCellWidget cellWidget) {

    }

    void OnNumpadProceed(float val) {

    }

    private AreaOperationCellWidget GenerateAreaCell(int row, int col) {
        if(mAreaCellActives[row, col]) //fail-safe
            return mAreaCellActives[row, col];

        if(mAreaCellCache.Count == 0)
            return null;

        var newAreaCellWidget = mAreaCellCache.RemoveLast();

        newAreaCellWidget.SetCellIndex(row, col);

        mAreaCellActives[row, col] = newAreaCellWidget;

        return newAreaCellWidget;
    }

    private void ClearAreaCells() {
        for(int r = 0; r < mAreaCellActives.GetLength(0); r++) {
            for(int c = 0; c < mAreaCellActives.GetLength(1); c++) {
                var areaCellWidget = mAreaCellActives[r, c];
                if(areaCellWidget) {
                    areaCellWidget.rectTransform.SetParent(cacheRoot, false);

                    mAreaCellCache.Add(areaCellWidget);

                    mAreaCellActives[r, c] = null;
                }
            }
        }
    }
}