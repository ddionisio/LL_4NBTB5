using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalAttackDistributiveMixUp : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalActive {
    [Header("Templates")]
    public AreaOperationCellWidget areaCellTemplate;
    public int areaCellCapacity = 4;

    public DigitWidget digitFixedTemplate;
    public int digitFixedCapacity = 5;

    public OpWidget opTemplate;
    public int opCapacity = 3;

    public Transform cacheRoot;

    [Header("Area Info")]
    public AreaGridControl[] areaGrids;

    [Header("Mistake Display")]
    public MistakeCounterWidget mistakeCounterDisplay;

    [Header("Error Info")]
    public M8.Animator.Animate errorAnimator;
    [M8.Animator.TakeSelector(animatorField = "errorAnimator")]
    public string takeError;

    [Header("Finish Info")]
    public M8.Animator.Animate finishAnimator;
    [M8.Animator.TakeSelector(animatorField = "finishAnimator")]
    public string takeFinish;
    public float finishEndDelay = 1f;

    [Header("Area Cell Anchors")]
    public string areaCellAnchorTop = "top";
    public string areaCellAnchorLeft = "left";
    public string areaCellAnchorOpTop = "opTop";
    public string areaCellAnchorOpLeft = "opLeft";

    [Header("Signal Invoke")]
    public SignalAttackState signalInvokeAttackStateChange;
    public M8.SignalBoolean signalInvokeActive;

    [Header("Explain Dialog")]
    public LoLExt.ModalDialogFlow explainDialog;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxSuccess;
    [M8.SoundPlaylist]
    public string sfxError;

    private AreaOperation mAreaOp;
    private MistakeInfo mMistakeInfo;

    private int mAreaGridInd = -1;

    private AreaOperationCellWidget[,] mAreaCellActives; //[row, col], count is based on row and col count in mAreaOp
    private M8.CacheList<AreaOperationCellWidget> mAreaCellCache;

    private DigitWidget[] mDigitFixedHorizontalActives; //count is based on mAreaOp's col count
    private DigitWidget[] mDigitFixedVerticalActives; //count is based on mAreaOp's row count
    private M8.CacheList<DigitWidget> mDigitFixedCache;

    private M8.CacheList<OpWidget> mOpActives;
    private M8.CacheList<OpWidget> mOpCache;

    private ModalAttackParams mAttackParms;

    private Coroutine mRout;

    private bool mIsInit;

    public void Back() {
        Close();

        signalInvokeAttackStateChange?.Invoke(AttackState.Cancel);
    }

    public void Proceed() {
        if(mRout != null)
            return;
            
        //check if area product sums match areas
        var areaMatchCount = 0;

        for(int r = 0; r < mAreaOp.areaRowCount; r++) {
            for(int c = 0; c < mAreaOp.areaColCount; c++) {
                var areaOpWidget = mAreaCellActives[r, c];

                if(areaOpWidget.cellData.op.equal == mAreaOp.GetAreaOperation(r, c).op.equal)
                    areaMatchCount++;
                else
                    areaOpWidget.ShowError();
            }
        }

        if(areaMatchCount == mAreaOp.areaRowCount * mAreaOp.areaColCount)
            mRout = StartCoroutine(DoFinish());
        else
            mRout = StartCoroutine(DoError());
    }

    void M8.IModalActive.SetActive(bool aActive) {
        signalInvokeActive?.Invoke(aActive);
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

                newAreaCell.rectTransform.SetParent(cacheRoot, false);

                mAreaCellCache.Add(newAreaCell);
            }

            //digits
            mDigitFixedHorizontalActives = new DigitWidget[digitFixedCapacity];
            mDigitFixedVerticalActives = new DigitWidget[digitFixedCapacity];
            mDigitFixedCache = new M8.CacheList<DigitWidget>(digitFixedCapacity);

            for(int i = 0; i < digitFixedCapacity; i++) {
                var newDigit = Instantiate(digitFixedTemplate);
                newDigit.Init(-1);

                newDigit.rectTransform.SetParent(cacheRoot, false);

                mDigitFixedCache.Add(newDigit);
            }

            //ops
            mOpActives = new M8.CacheList<OpWidget>(opCapacity);
            mOpCache = new M8.CacheList<OpWidget>(opCapacity);

            for(int i = 0; i < opCapacity; i++) {
                var newOp = Instantiate(opTemplate);

                newOp.rectTransform.SetParent(cacheRoot, false);

                mOpCache.Add(newOp);
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

        if(mAreaOp != null) {
            //divide up areas
            mAreaOp.SplitAll();

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

                        var areaCellWidget = GenerateAreaCell(row, col);

                        areaCellWidget.interactable = false;
                        areaCellWidget.solvedVisible = false;

                        //TODO: setup panel color

                        areaCellWidget.rectTransform.SetParent(gridRoot, false);

                        if(cell.isValid) {
                            areaCellWidget.ApplyCell(cell, true);

                            curAreaGrid.ApplyArea(row, col, areaCellWidget.rectTransform);

                            areaCellWidget.operationVisible = true;
                            areaCellWidget.gameObject.SetActive(true);
                        }
                        else {
                            areaCellWidget.operationVisible = false;
                            areaCellWidget.gameObject.SetActive(false);
                        }
                    }
                }

                //insert digits and operators

                //top
                for(int col = 0; col < mAreaOp.areaColCount; col++) {
                    var cell = mAreaOp.GetAreaOperation(0, col);
                    if(cell.isValid) {
                        SetDigitFixedColumn(col, cell.op.operand1);

                        if(col < mAreaOp.areaColCount - 1)
                            GenerateOperatorColumn(col);
                    }
                }

                //left
                for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                    var cell = mAreaOp.GetAreaOperation(row, 0);
                    if(cell.isValid) {
                        SetDigitFixedRow(row, cell.op.operand2);

                        if(row < mAreaOp.areaRowCount - 1)
                            GenerateOperatorRow(row);
                    }
                }

                //mix up the factors between all area cells
                int validCount = 0;
                for(int r = 0; r < mAreaOp.areaRowCount; r++) {
                    for(int c = 0; c < mAreaOp.areaColCount; c++) {
                        var cell = mAreaOp.GetAreaOperation(r, c);
                        if(cell.isValid)
                            validCount++;
                    }
                }

                var factorNumbers = new int[validCount * 2];
                var factorInd = 0;

                for(int r = 0; r < mAreaOp.areaRowCount; r++) {
                    for(int c = 0; c < mAreaOp.areaColCount; c++) {
                        var cell = mAreaOp.GetAreaOperation(r, c);
                        if(cell.isValid) {
                            factorNumbers[factorInd] = cell.op.operand1;
                            factorNumbers[factorInd + 1] = cell.op.operand2;

                            factorInd += 2;
                        }
                    }
                }

                //shuffle
                M8.ArrayUtil.Shuffle(factorNumbers);

                //swap some numbers around if they match
                factorInd = 0;
                for(int r = 0; r < mAreaOp.areaRowCount; r++) {
                    for(int c = 0; c < mAreaOp.areaColCount; c++) {
                        var cell = mAreaOp.GetAreaOperation(r, c);
                        if(cell.isValid) {
                            var factorEq = factorNumbers[factorInd] + factorNumbers[factorInd + 1];
                            if(cell.op.equal == factorEq) {
                                int swapInd = factorInd + 2;
                                if(swapInd >= factorNumbers.Length)
                                    swapInd = 0;

                                int lastNum = factorNumbers[factorInd + 1];
                                factorNumbers[factorInd + 1] = factorNumbers[swapInd];
                                factorNumbers[swapInd] = lastNum;
                            }

                            factorInd += 2;
                        }
                    }
                }

                //apply mixups to area cell widgets
                factorInd = 0;

                for(int r = 0; r < mAreaOp.areaRowCount; r++) {
                    for(int c = 0; c < mAreaOp.areaColCount; c++) {
                        var areaOpWidget = mAreaCellActives[r, c];

                        var cell = areaOpWidget.cellData;
                        if(cell.isValid) {
                            cell.op.operand1 = factorNumbers[factorInd];
                            cell.op.operand2 = factorNumbers[factorInd + 1];

                            areaOpWidget.ApplyCell(cell, false);

                            factorInd += 2;
                        }
                    }
                }
            }
            else
                Debug.LogError("Unable to find matching area grid: [row = " + mAreaOp.areaRowCount + ", col = " + mAreaOp.areaColCount + "]");
        }

        //setup mistake counter
        if(mMistakeInfo != null) {
            if(mistakeCounterDisplay)
                mistakeCounterDisplay.Init(mMistakeInfo);
        }

        var isDialogDone = LoLExt.LoLManager.instance.userData.GetInt(GameData.userDataKeyFTUEDistributeMixup, 0) > 0;
        if(!isDialogDone) {
            LoLExt.LoLManager.instance.userData.SetInt(GameData.userDataKeyFTUEDistributeMixup, 1);

            StartCoroutine(DoDialog());
        }
    }

    void M8.IModalPop.Pop() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }

        ClearAreaCells();
        ClearDigits();
        ClearOps();

        if(mAreaGridInd != -1) {
            if(areaGrids[mAreaGridInd])
                areaGrids[mAreaGridInd].gameObject.SetActive(false);

            mAreaGridInd = -1;
        }
    }

    IEnumerator DoDialog() {
        yield return explainDialog.Play();
    }

    IEnumerator DoError() {
        if(mMistakeInfo != null) {
            if(!string.IsNullOrEmpty(sfxError))
                M8.SoundPlaylist.instance.Play(sfxError, false);

            mMistakeInfo.AppendAreaEvaluateCount();

            //update mistake display
            if(mistakeCounterDisplay)
                mistakeCounterDisplay.UpdateMistakeCount(mMistakeInfo);

            if(errorAnimator && !string.IsNullOrEmpty(takeError))
                yield return errorAnimator.PlayWait(takeError);

            mRout = null;

            if(mMistakeInfo.isFull) {
                Close();

                signalInvokeAttackStateChange?.Invoke(AttackState.Fail);
            }
        }
        else
            mRout = null;
    }

    IEnumerator DoFinish() {
        if(!string.IsNullOrEmpty(sfxSuccess))
            M8.SoundPlaylist.instance.Play(sfxSuccess, false);

        if(finishAnimator && !string.IsNullOrEmpty(takeFinish))
            yield return finishAnimator.PlayWait(takeFinish);

        yield return new WaitForSeconds(finishEndDelay);

        mRout = null;

        Close();

        M8.ModalManager.main.Open(GameData.instance.modalAttackAreaEvaluate, mAttackParms);
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

    private void SetDigitFixedColumn(int colIndex, int number) {
        var digitWidget = GetOrGenerateDigitFixed(mDigitFixedHorizontalActives, colIndex);
        if(!digitWidget)
            return;

        digitWidget.number = number;

        if(digitWidget.numberRoot) {
            digitWidget.numberRoot.anchorMin = digitWidget.numberRoot.anchorMax = digitWidget.numberRoot.pivot = new Vector2 { x = 0.5f, y = 0.5f };
        }

        //setup position
        var areaCellWidget = mAreaCellActives[mAreaOp.areaRowCount - 1, colIndex];
        if(areaCellWidget) {
            var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorTop);

            var rTrans = digitWidget.rectTransform;

            rTrans.SetParent(anchorTrans, false);
            rTrans.pivot = rTrans.anchorMin = rTrans.anchorMax = new Vector2 { x = 0.5f, y = 0f };
            rTrans.anchoredPosition = Vector2.zero;
        }
    }

    private void SetDigitFixedRow(int rowIndex, int number) {
        var digitWidget = GetOrGenerateDigitFixed(mDigitFixedVerticalActives, rowIndex);
        if(!digitWidget)
            return;

        digitWidget.number = number;

        if(digitWidget.numberRoot) {
            digitWidget.numberRoot.anchorMin = digitWidget.numberRoot.anchorMax = digitWidget.numberRoot.pivot = new Vector2 { x = 1.0f, y = 0.5f };
        }

        //setup position
        var areaCellWidget = mAreaCellActives[rowIndex, mAreaOp.areaColCount - 1];
        if(areaCellWidget) {
            var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorLeft);

            var rTrans = digitWidget.rectTransform;

            rTrans.SetParent(anchorTrans, false);
            rTrans.pivot = rTrans.anchorMin = rTrans.anchorMax = new Vector2 { x = 1f, y = 0.5f };
            rTrans.anchoredPosition = Vector2.zero;
        }
    }

    private DigitWidget GetOrGenerateDigitFixed(DigitWidget[] digitWidgets, int index) {
        if(index < 0 || index >= digitWidgets.Length)
            return null;

        var retDigitWidget = digitWidgets[index];
        if(!retDigitWidget) {
            if(mDigitFixedCache.Count != 0) {
                retDigitWidget = mDigitFixedCache.RemoveLast();

                retDigitWidget.Init(index);

                digitWidgets[index] = retDigitWidget;
            }
        }

        return retDigitWidget;
    }

    private OpWidget GenerateOperatorColumn(int colIndex) {
        if(mOpCache.Count == 0 || colIndex < 0 || colIndex >= mAreaOp.areaColCount)
            return null;

        var newOpWidget = mOpCache.RemoveLast();

        mOpActives.Add(newOpWidget);

        var areaCellWidget = mAreaCellActives[mAreaOp.areaRowCount - 1, colIndex];

        var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorOpTop);

        var rectTrans = newOpWidget.rectTransform;

        rectTrans.SetParent(anchorTrans, false);
        rectTrans.anchoredPosition = Vector2.zero;

        return newOpWidget;
    }

    private OpWidget GenerateOperatorRow(int rowIndex) {
        if(mOpCache.Count == 0 || rowIndex < 0 || rowIndex >= mAreaOp.areaRowCount)
            return null;

        var newOpWidget = mOpCache.RemoveLast();

        mOpActives.Add(newOpWidget);

        var areaCellWidget = mAreaCellActives[rowIndex, mAreaOp.areaColCount - 1];

        var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorOpLeft);

        var rectTrans = newOpWidget.rectTransform;

        rectTrans.SetParent(anchorTrans, false);
        rectTrans.anchoredPosition = Vector2.zero;

        return newOpWidget;
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

    private void ClearDigits() {
        for(int i = 0; i < mDigitFixedHorizontalActives.Length; i++) {
            var digitWidget = mDigitFixedHorizontalActives[i];
            if(digitWidget) {
                digitWidget.rectTransform.SetParent(cacheRoot, false);

                mDigitFixedCache.Add(digitWidget);

                mDigitFixedHorizontalActives[i] = null;
            }
        }

        for(int i = 0; i < mDigitFixedVerticalActives.Length; i++) {
            var digitWidget = mDigitFixedVerticalActives[i];
            if(digitWidget) {
                digitWidget.rectTransform.SetParent(cacheRoot, false);

                mDigitFixedCache.Add(digitWidget);

                mDigitFixedVerticalActives[i] = null;
            }
        }
    }

    private void ClearOps() {
        for(int i = 0; i < mOpActives.Count; i++) {
            var opWidget = mOpActives[i];

            opWidget.rectTransform.SetParent(cacheRoot, false);

            mOpCache.Add(opWidget);
        }

        mOpActives.Clear();
    }
}
