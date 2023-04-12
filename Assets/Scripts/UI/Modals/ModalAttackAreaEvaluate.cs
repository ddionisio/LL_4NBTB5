using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalAttackAreaEvaluate : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalActive {
    [Header("Templates")]
    public AreaOperationCellWidget areaCellTemplate;
    public int areaCellCapacity = 4;

    public Transform cacheRoot;

    [Header("Area Info")]
    public AreaGridControl[] areaGrids;

    [Header("Mistake Display")]
    public MistakeCounterWidget mistakeCounterDisplay;

    [Header("Numpad Info")]
    public string numpadOpTextFormat = "{0} {1} {2} =";

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeFinish;

    [Header("Error Info")]
    public float errorEndDelay = 0.5f;

    [Header("Finish Info")]
    public float finishEndDelay = 1f;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxCorrect;
    [M8.SoundPlaylist]
    public string sfxError;
    [M8.SoundPlaylist]
    public string sfxSuccess;

    [Header("Signal Invoke")]
    public SignalAttackState signalInvokeAttackStateChange;
    public M8.SignalBoolean signalInvokeActive;
    public M8.SignalBoolean signalInvokeInputActive;
    public M8.Signal signalInvokeCorrect;
    public M8.Signal signalInvokeError;
    public M8.SignalFloat signalInvokeValueChange;
    public M8.SignalString signalInvokeChangeOpText;

    [Header("Signal Listen")]
    public M8.SignalFloat signalListenNumpadProceed;

    public bool isAnswerProcessing { get { return mAnswerProcessRout != null; } }

    public AreaOperationCellWidget areaOpCellWidgetSelected { get; private set; }

    public int lastAnswerNumber { get; private set; }

    private AreaOperation mAreaOp;
    private MistakeInfo mMistakeInfo;

    private int mAreaGridInd = -1;

    private AreaOperationCellWidget[,] mAreaCellActives; //[row, col], count is based on row and col count in mAreaOp
    private M8.CacheList<AreaOperationCellWidget> mAreaCellCache;

    private ModalAttackParams mAttackParms;

    private M8.GenericParams mNumpadParms = new M8.GenericParams();

    private bool mIsInit;

    private Coroutine mAnswerProcessRout;

    public void Back() {
        Close();

        M8.ModalManager.main.Open(GameData.instance.modalAttackDistributive, mAttackParms);
    }

    public void Proceed() {
        //check if we have all the areas solved
        int areaValidCount = 0;
        int areaSolvedCount = 0;

        for(int row = 0; row < mAreaOp.areaRowCount; row++) {
            for(int col = 0; col < mAreaOp.areaColCount; col++) {
                var cell = mAreaOp.GetAreaOperation(row, col);
                if(cell.isValid) {
                    areaValidCount++;

                    if(cell.isSolved)
                        areaSolvedCount++;
                }
            }
        }

        if(areaSolvedCount == areaValidCount) {
            Close();

            //if there's only one area, then it is already solved
            if(areaSolvedCount > 1)
                M8.ModalManager.main.Open(GameData.instance.modalAttackSums, mAttackParms);
            else
                signalInvokeAttackStateChange?.Invoke(AttackState.Success);
        }
        else {
            //show message
        }
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

                            areaCellWidget.isHighlight = false;
                            areaCellWidget.interactable = false;
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
            else
                Debug.LogError("Unable to find matching area grid: [row = " + mAreaOp.areaRowCount + ", col = " + mAreaOp.areaColCount + "]");
        }

        //setup mistake counter
        if(mMistakeInfo != null) {
            if(mistakeCounterDisplay)
                mistakeCounterDisplay.Init(mMistakeInfo);
        }

        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback += OnNumpadProceed;

        StartCoroutine(DoAreaEvaluations());
    }

    void M8.IModalPop.Pop() {
        StopAnswerProcess();

        areaOpCellWidgetSelected = null;

        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback -= OnNumpadProceed;

        ClearAreaCells();

        if(mAreaGridInd != -1) {
            if(areaGrids[mAreaGridInd])
                areaGrids[mAreaGridInd].gameObject.SetActive(false);

            mAreaGridInd = -1;
        }
    }

    void OnNumpadProceed(float val) {
        //M8.ModalManager.main.CloseUpTo(GameData.instance.modalNumpad, true);

        if(!areaOpCellWidgetSelected)
            return;

        signalInvokeInputActive?.Invoke(false);

        var num = Mathf.RoundToInt(val);

        lastAnswerNumber = num;

        //check if matches
        if(num == areaOpCellWidgetSelected.cellData.op.equal) {
            //apply solved to shared data
            mAreaOp.SetAreaOperationSolved(areaOpCellWidgetSelected.row, areaOpCellWidgetSelected.col, true);

            StartCorrect(areaOpCellWidgetSelected);
        }
        else { //error
            //update mistake count in shared data
            mMistakeInfo.AppendAreaEvaluateCount();

            StartError();
        }
    }

    IEnumerator DoAreaEvaluations() {
        var modalMain = M8.ModalManager.main;

        //wait for modal to finish entering
        while(modalMain.isBusy)
            yield return null;

        //open up numpad
        mNumpadParms[ModalCalculator.parmInitValue] = 0;
        mNumpadParms[GameData.modalParamOperationText] = "";

        modalMain.Open(GameData.instance.modalNumpad, mNumpadParms);

        yield return null;

        signalInvokeInputActive?.Invoke(false);

        //go through each areas to evaluate (column to row)
        for(int c = 0; c < mAreaOp.areaColCount; c++) {
            for(int r = 0; r < mAreaOp.areaRowCount; r++) {
                var areaCellWidget = mAreaCellActives[r, c];
                if(areaCellWidget && !areaCellWidget.solvedVisible) {
                    areaCellWidget.isHighlight = true;

                    var cell = areaCellWidget.cellData;

                    signalInvokeValueChange?.Invoke(0f);
                    signalInvokeChangeOpText?.Invoke(string.Format(numpadOpTextFormat, cell.op.operand1, Operation.GetOperatorTypeChar(cell.op.op), cell.op.operand2));

                    areaOpCellWidgetSelected = areaCellWidget;

                    signalInvokeInputActive?.Invoke(true);

                    //wait for correct answer
                    while(!areaCellWidget.solvedVisible) {
                        if(mMistakeInfo.isFull)
                            yield break;

                        yield return null;
                    }
                }
            }
        }

        areaOpCellWidgetSelected = null;

        //close up numpad
        modalMain.CloseUpTo(GameData.instance.modalNumpad, true);

        while(modalMain.isBusy)
            yield return null;

        //success
        if(!string.IsNullOrEmpty(sfxSuccess))
            M8.SoundPlaylist.instance.Play(sfxSuccess, false);

        if(animator && !string.IsNullOrEmpty(takeFinish))
            yield return animator.PlayWait(takeFinish);

        yield return new WaitForSeconds(finishEndDelay);

        Proceed();
    }

    IEnumerator DoError() {
        if(!string.IsNullOrEmpty(sfxError))
            M8.SoundPlaylist.instance.Play(sfxError, false);

        //update mistake display
        mistakeCounterDisplay.UpdateMistakeCount(mMistakeInfo);

        //do error animation (also send signal for animation in background)
        while(mistakeCounterDisplay.isBusy)
            yield return null;

        //check if error is full, if so, close and then send signal
        if(mMistakeInfo.isFull) {
            yield return new WaitForSeconds(errorEndDelay);

            mAnswerProcessRout = null;

            Close();

            signalInvokeAttackStateChange?.Invoke(AttackState.Fail);
        }
        else {
            signalInvokeValueChange?.Invoke(0f);

            signalInvokeInputActive?.Invoke(true);

            mAnswerProcessRout = null;
        }
    }

    IEnumerator DoCorrect(AreaOperationCellWidget areaOpCellWidget) {
        if(!string.IsNullOrEmpty(sfxCorrect))
            M8.SoundPlaylist.instance.Play(sfxCorrect, false);

        //set display as solved
        areaOpCellWidget.ApplyCell(mAreaOp.GetAreaOperation(areaOpCellWidget.row, areaOpCellWidget.col), false);

        areaOpCellWidget.isHighlight = false;
        areaOpCellWidget.solvedVisible = true;

        //do correct animation (also send signal for animation in background)
        yield return new WaitForSeconds(0.5f);

        mAnswerProcessRout = null;
    }

    private void StartError() {
        signalInvokeError?.Invoke();

        if(mAnswerProcessRout != null)
            StopCoroutine(mAnswerProcessRout);

        mAnswerProcessRout = StartCoroutine(DoError());
    }

    private void StartCorrect(AreaOperationCellWidget areaOpCellWidget) {
        signalInvokeCorrect?.Invoke();

        if(mAnswerProcessRout != null)
            StopCoroutine(mAnswerProcessRout);

        mAnswerProcessRout = StartCoroutine(DoCorrect(areaOpCellWidget));
    }

    private void StopAnswerProcess() {
        if(mAnswerProcessRout != null) {
            StopCoroutine(mAnswerProcessRout);
            mAnswerProcessRout = null;
        }
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