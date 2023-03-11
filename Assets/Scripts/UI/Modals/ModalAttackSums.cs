using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModalAttackSums : M8.ModalController, M8.IModalPush, M8.IModalPop {
    [Header("Factors")]
    public DigitGroupWidget factorTemplate;
    public int factorCapacity;
    public Transform factorCacheRoot;
    public RectTransform factorRoot;
    //public Grid

    [Header("Carry Over")]
    public DigitGroupWidget carryOverGroup;

    [Header("Input Answer")]
    public DigitGroupWidget inputAnswerGroup;

    [Header("Mistake Display")]
    public MistakeCounterWidget mistakeCounterDisplay;

    [Header("Highlight Display")]
    public RectTransform highlightRoot; //assume it is in the same space as the factors
    public float highlightMoveDelay = 0.3f;
    public DG.Tweening.Ease highlightMoveEase = DG.Tweening.Ease.InOutSine;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeFinish;

    [Header("Error Info")]
    public float errorEndDelay = 0.5f;

    [Header("Finish Info")]
    public float finishEndDelay = 1f;

    [Header("Signal Invoke")]
    public SignalAttackState signalInvokeAttackStateChange;
    public M8.SignalBoolean signalInvokeInputActive;
    public M8.Signal signalInvokeCorrect;
    public M8.Signal signalInvokeError;
    public M8.SignalFloat signalInvokeValueChange;
    public M8.SignalString signalInvokeChangeOpText;

    [Header("Signal Listen")]
    public M8.SignalFloat signalListenNumpadProceed;

    public bool isAnswerProcessing { get { return mAnswerProcessRout != null; } }

    private AreaOperation mAreaOp;
    private MistakeInfo mMistakeInfo;

    private int mAnswerDigitCount;

    private int mInputAnswerDigitIndex;
    private int mInputAnswerDigitNumber;

    private float mFactorElementWidth; //assume fixed width per digit element of each factor

    private ModalAttackParams mAttackParms;

    private M8.GenericParams mNumpadParms = new M8.GenericParams();

    private bool mIsInit;

    private List<DigitGroupWidget> mFactorActives;
    private M8.CacheList<DigitGroupWidget> mFactorCache;

    private System.Text.StringBuilder mSumsStringBuild = new System.Text.StringBuilder();

    private DG.Tweening.EaseFunction mHighlightMoveFunc;

    private Coroutine mAnswerProcessRout;

    private bool mIsCorrectAnswerProcessed;

    public void Back() {
        Close();

        M8.ModalManager.main.Open(GameData.instance.modalAttackAreaEvaluate, mAttackParms);
    }

    public void Proceed() {
        if(inputAnswerGroup.number == mAreaOp.operation.equal) {
            Close();

            signalInvokeAttackStateChange?.Invoke(AttackState.Success);
        }
        else {
            //show message (shouldn't get here)
        }
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit) {
            //init factors cache
            mFactorActives = new List<DigitGroupWidget>(factorCapacity);
            mFactorCache = new M8.CacheList<DigitGroupWidget>(factorCapacity);

            for(int i = 0; i < factorCapacity; i++) {
                var newFactorWidget = Instantiate(factorTemplate);
                newFactorWidget.Init();

                newFactorWidget.rectTransform.SetParent(factorCacheRoot, false);

                mFactorCache.Add(newFactorWidget);
            }

            //init carry-over
            carryOverGroup.Init();

            //init input answer
            inputAnswerGroup.Init();

            //init highlight
            var factorRootSizeDelta = factorRoot.sizeDelta;

            mFactorElementWidth = factorRootSizeDelta.x / inputAnswerGroup.digitCapacity;

            var highlightSizeDelta = highlightRoot.sizeDelta;
            highlightSizeDelta.x = mFactorElementWidth;
            highlightRoot.sizeDelta = highlightSizeDelta;

            mHighlightMoveFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(highlightMoveEase);

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
            mAnswerDigitCount = WholeNumber.DigitCount(mAreaOp.operation.equal);

            //setup factors
            for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                for(int col = 0; col < mAreaOp.areaColCount; col++) {
                    var cell = mAreaOp.GetAreaOperation(row, col);
                    if(cell.isSolved && cell.op.equal > 0) {
                        var factorWidget = GenerateFactor();

                        factorWidget.number = cell.op.equal;
                    }
                }
            }

            //sort factors from highest to lowest
            mFactorActives.Sort(FactorSortCompare);

            //add factors to layout
            float factorsHeight = 0; //assume size delta is the actual size of the factor widget

            for(int i = 0; i < mFactorActives.Count; i++) {
                factorsHeight += mFactorActives[i].rectTransform.sizeDelta.y;

                mFactorActives[i].rectTransform.SetParent(factorRoot);
            }

            //adjust factor root size
            //factorRoot.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetPreferredSize(factorRoot, axis)); //Axis-Y
            var factorRootSizeDelta = factorRoot.sizeDelta;
            factorRootSizeDelta.y = factorsHeight;
            factorRoot.sizeDelta = factorRootSizeDelta;
        }

        //setup mistake counter
        if(mMistakeInfo != null) {
            if(mistakeCounterDisplay)
                mistakeCounterDisplay.Init(mMistakeInfo);
        }

        //reset carry-over
        carryOverGroup.SetDigitsEmpty(carryOverGroup.digitCapacity);

        //setup input answer
        inputAnswerGroup.SetDigitInteractiveAll(false);
        inputAnswerGroup.SetDigitVisibleAll(false);

        inputAnswerGroup.number = 0;

        inputAnswerGroup.SetDigitEmpty(0);
        inputAnswerGroup.SetDigitVisible(0, true);
        inputAnswerGroup.SetDigitInteractive(0, true);
        inputAnswerGroup.SetDigitHighlight(0, true);

        //setup initial highlight pos
        var highlightAnchorPos = highlightRoot.anchoredPosition;
        highlightAnchorPos.x = -mFactorElementWidth * 0.5f;
        highlightRoot.anchoredPosition = highlightAnchorPos;

        highlightRoot.gameObject.SetActive(true);

        mInputAnswerDigitIndex = -1;

        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback += OnNumpadProceed;

        StartCoroutine(DoSums());
    }

    void M8.IModalPop.Pop() {
        StopAnswerProcess();

        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback -= OnNumpadProceed;

        ClearFactors();
    }

    void OnNumpadProceed(float val) {
        signalInvokeInputActive?.Invoke(false);

        int iVal = Mathf.RoundToInt(val);

        if(mInputAnswerDigitNumber == iVal) { //correct
            StartCorrect(mInputAnswerDigitIndex, mInputAnswerDigitNumber);
        }
        else { //error
            //update mistake count in shared data
            mMistakeInfo.AppendAreaEvaluateCount();

            StartError();
        }
    }

    IEnumerator DoSums() {
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

        mInputAnswerDigitIndex = 0;

        while(inputAnswerGroup.number != mAreaOp.operation.equal) {
            //setup string and sums
            mInputAnswerDigitNumber = 0;

            mSumsStringBuild.Clear();

            //check if there's a carry-over
            var carryOverDigit = carryOverGroup.GetDigitNumber(mInputAnswerDigitIndex);
            if(carryOverDigit > 0) {
                mSumsStringBuild.Append(carryOverDigit);

                mInputAnswerDigitNumber += carryOverDigit;
            }

            for(int i = 0; i < mFactorActives.Count; i++) {
                var factorWidget = mFactorActives[i];

                if(mInputAnswerDigitIndex < factorWidget.digitCount) {
                    if(i > 0 || carryOverDigit > 0)
                        mSumsStringBuild.Append(" + ");

                    var digitNum = factorWidget.GetDigitNumber(mInputAnswerDigitIndex);

                    mInputAnswerDigitNumber += digitNum;

                    mSumsStringBuild.Append(digitNum);
                }
            }

            mSumsStringBuild.Append(" =");
            //

            signalInvokeChangeOpText?.Invoke(mSumsStringBuild.ToString());
            signalInvokeValueChange?.Invoke(0f);

            signalInvokeInputActive?.Invoke(true);

            //wait for correct answer
            mIsCorrectAnswerProcessed = false;
            while(!mIsCorrectAnswerProcessed) {
                if(mMistakeInfo.isFull)
                    yield break;

                yield return null;
            }

            mInputAnswerDigitIndex++;
        }

        //close up numpad
        modalMain.CloseUpTo(GameData.instance.modalNumpad, true);

        while(modalMain.isBusy)
            yield return null;

        //success
        if(animator && !string.IsNullOrEmpty(takeFinish))
            yield return animator.PlayWait(takeFinish);

        yield return new WaitForSeconds(finishEndDelay);

        Proceed();
    }

    IEnumerator DoError() {
        //update mistake display
        mistakeCounterDisplay.UpdateMistakeCount(mMistakeInfo);

        //do error animation (also send signal for animation in background)
        while(mistakeCounterDisplay.isBusy)
            yield return null;

        mAnswerProcessRout = null;

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

    IEnumerator DoCorrect(int digitIndex, int digitAnswer) {
        int singleDigit = digitAnswer % 10;

        int carryOverDigit = (digitAnswer / 10) % 10;

        //apply single digit
        inputAnswerGroup.SetDigitNumber(digitIndex, singleDigit);

        //hide interactive
        inputAnswerGroup.SetDigitInteractive(digitIndex, false);
        inputAnswerGroup.SetDigitHighlight(digitIndex, false);

        int nextDigitIndex = digitIndex + 1;
        if(nextDigitIndex < mAnswerDigitCount && nextDigitIndex < inputAnswerGroup.digitCapacity) {
            //check carry-over
            if(carryOverDigit > 0) {
                var answerDigitWidget = inputAnswerGroup.GetDigitWidget(digitIndex);

                //apply new carryover for next digit
                var carryOverDigitWidget = carryOverGroup.SetDigitNumber(nextDigitIndex, carryOverDigit);

                //do animation
                //-> show carry-over to the left of current digitIndex of input answer
                //-> move carry-over to the top
                //-> move carry-over to its designated space, hide

                carryOverDigitWidget.PlayPulse();
                
            }

            //move highlight
            var curHighlightAnchorPos = highlightRoot.anchoredPosition;

            var toHighlightPosX = -(mFactorElementWidth * nextDigitIndex) - (mFactorElementWidth * 0.5f);

            var curTime = 0f;
            while(curTime < highlightMoveDelay) {
                yield return null;

                curTime += Time.deltaTime;

                var t = mHighlightMoveFunc(curTime, highlightMoveDelay, 0f, 0f);

                 var newX = Mathf.Lerp(curHighlightAnchorPos.x, toHighlightPosX, t);

                highlightRoot.anchoredPosition = new Vector2 { x = newX, y = curHighlightAnchorPos.y };
            }

            //show next input digit interaction
            inputAnswerGroup.SetDigitEmpty(nextDigitIndex);
            inputAnswerGroup.SetDigitInteractive(nextDigitIndex, true);
            inputAnswerGroup.SetDigitVisible(nextDigitIndex, true);
            inputAnswerGroup.SetDigitHighlight(nextDigitIndex, true);
        }
        else {
            //we are finish
            highlightRoot.gameObject.SetActive(false);
        }

        mAnswerProcessRout = null;

        mIsCorrectAnswerProcessed = true;
    }

    private void StartError() {
        signalInvokeError?.Invoke();

        if(mAnswerProcessRout != null)
            StopCoroutine(mAnswerProcessRout);

        mAnswerProcessRout = StartCoroutine(DoError());
    }

    private void StartCorrect(int digitIndex, int digitAnswer) {
        signalInvokeCorrect?.Invoke();

        if(mAnswerProcessRout != null)
            StopCoroutine(mAnswerProcessRout);

        mAnswerProcessRout = StartCoroutine(DoCorrect(digitIndex, digitAnswer));
    }

    private void StopAnswerProcess() {
        if(mAnswerProcessRout != null) {
            StopCoroutine(mAnswerProcessRout);
            mAnswerProcessRout = null;
        }
    }

    private DigitGroupWidget GenerateFactor() {
        if(mFactorCache.Count == 0)
            return null;

        var factorWidget = mFactorCache.RemoveLast();

        mFactorActives.Add(factorWidget);

        return factorWidget;
    }

    private int FactorSortCompare(DigitGroupWidget left, DigitGroupWidget right) {
        if(!left)
            return -1;
        else if(!right)
            return 1;

        return right.number - left.number;
    }

    private void ClearFactors() {
        for(int i = 0; i < mFactorActives.Count; i++) {
            var factorWidget = mFactorActives[i];
            if(factorWidget) {
                factorWidget.rectTransform.SetParent(factorCacheRoot, false);
                mFactorCache.Add(factorWidget);
            }
        }

        mFactorActives.Clear();
    }
}