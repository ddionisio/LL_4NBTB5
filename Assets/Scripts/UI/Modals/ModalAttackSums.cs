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

    [Header("Signal Invoke")]
    public SignalAttackState signalInvokeAttackStateChange;

    [Header("Signal Listen")]
    public M8.SignalFloat signalListenNumpadProceed;

    public bool isAnswerProcessing { get { return mAnswerProcessRout != null; } }

    private AreaOperation mAreaOp;
    private MistakeInfo mMistakeInfo;

    private int mAnswerDigitCount;

    private int mInputDigitAnswerLastIndex;
    private int mInputDigitAnswerNumber;

    private float mFactorElementWidth; //assume fixed width per digit element of each factor

    private ModalAttackParams mAttackParms;

    private M8.GenericParams mNumpadParms = new M8.GenericParams();

    private bool mIsInit;

    private List<DigitGroupWidget> mFactorActives;
    private M8.CacheList<DigitGroupWidget> mFactorCache;

    private System.Text.StringBuilder mSumsStringBuild = new System.Text.StringBuilder();

    private DG.Tweening.EaseFunction mHighlightMoveFunc;

    private Coroutine mAnswerProcessRout;

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
            //show message
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
            inputAnswerGroup.clickCallback += OnInputAnswerDigitClick;

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
                    if(cell.isSolved) {
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
        carryOverGroup.number = 0;
        carryOverGroup.SetDigitVisibleAll(false);

        //setup input answer
        inputAnswerGroup.SetDigitInteractiveAll(false);
        inputAnswerGroup.SetDigitVisibleAll(false);

        inputAnswerGroup.SetDigitEmpty(0);
        inputAnswerGroup.SetDigitVisible(0, true);
        inputAnswerGroup.SetDigitInteractive(0, true);

        //setup initial highlight pos
        var highlightAnchorPos = highlightRoot.anchoredPosition;
        highlightAnchorPos.x = -mFactorElementWidth * 0.5f;
        highlightRoot.anchoredPosition = highlightAnchorPos;

        highlightRoot.gameObject.SetActive(true);

        mInputDigitAnswerLastIndex = -1;

        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback += OnNumpadProceed;
    }

    void M8.IModalPop.Pop() {
        StopAnswerProcess();

        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback -= OnNumpadProceed;

        ClearFactors();
    }

    void OnInputAnswerDigitClick(int digitIndex) {
        if(mInputDigitAnswerLastIndex != digitIndex) {
            //setup string and sums
            mInputDigitAnswerNumber = 0;

            mSumsStringBuild.Clear();

            //check if there's a carry-over
            var carryOverDigit = carryOverGroup.GetDigitNumber(digitIndex);
            if(carryOverDigit > 0) {
                mSumsStringBuild.Append(carryOverDigit);

                mInputDigitAnswerNumber += carryOverDigit;
            }

            for(int i = 0; i < mFactorActives.Count; i++) {
                var factorWidget = mFactorActives[i];

                if(digitIndex < factorWidget.digitCount) {
                    if(i > 0 || carryOverDigit > 0)
                        mSumsStringBuild.Append(" + ");

                    var digitNum = factorWidget.GetDigitNumber(digitIndex);

                    mInputDigitAnswerNumber += digitNum;

                    mSumsStringBuild.Append(digitNum);
                }
            }

            mSumsStringBuild.Append(" =");

            mInputDigitAnswerLastIndex = digitIndex;

            mNumpadParms[GameData.modalParamOperationText] = mSumsStringBuild.ToString();
        }

        //call numpad
        mNumpadParms[ModalCalculator.parmInitValue] = 0;

        M8.ModalManager.main.Open(GameData.instance.modalNumpad, mNumpadParms);
    }

    void OnNumpadProceed(float val) {
        M8.ModalManager.main.CloseUpTo(GameData.instance.modalNumpad, true);

        int iVal = Mathf.RoundToInt(val);

        if(mInputDigitAnswerNumber == iVal) { //correct
            StartCorrect(mInputDigitAnswerLastIndex, mInputDigitAnswerNumber);
        }
        else { //error
            //update mistake count in shared data
            mMistakeInfo.AppendAreaEvaluateCount();

            StartError();
        }
    }

    IEnumerator DoError() {
        //wait for modal to finish
        while(M8.ModalManager.main.isBusy)
            yield return null;

        //update mistake display
        mistakeCounterDisplay.UpdateMistakeCount(mMistakeInfo);

        //do error animation (also send signal for animation in background)

        mAnswerProcessRout = null;

        //check if error is full, if so, close and then send signal
        if(mMistakeInfo.isFull) {
            Close();

            signalInvokeAttackStateChange?.Invoke(AttackState.Fail);
        }
    }

    IEnumerator DoCorrect(int digitIndex, int digitAnswer) {
        //wait for modal to finish
        while(M8.ModalManager.main.isBusy)
            yield return null;

        int singleDigit = digitAnswer % 10;

        int carryOverDigit = (digitAnswer / 10) % 10;

        //apply single digit
        inputAnswerGroup.SetDigitNumber(digitIndex, singleDigit);

        //hide interactive
        inputAnswerGroup.SetDigitInteractive(digitIndex, false);

        int nextDigitIndex = digitIndex + 1;
        if(nextDigitIndex < mAnswerDigitCount && nextDigitIndex < inputAnswerGroup.digitCapacity) {
            //check carry-over
            if(carryOverDigit > 0) {
                //do animation
                //-> show carry-over to the left of current digitIndex of input answer
                //-> move carry-over to the top
                //-> move carry-over to its designated space, hide

                //apply new carryover for next digit
                carryOverGroup.SetDigitNumber(nextDigitIndex, carryOverDigit);
                carryOverGroup.SetDigitVisible(nextDigitIndex, true);
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
            inputAnswerGroup.SetDigitInteractive(nextDigitIndex, true);
            inputAnswerGroup.SetDigitVisible(nextDigitIndex, true);
        }
        else {
            //we are finish
            highlightRoot.gameObject.SetActive(false);
        }

        mAnswerProcessRout = null;
    }

    private void StartError() {
        if(mAnswerProcessRout != null)
            StopCoroutine(mAnswerProcessRout);

        mAnswerProcessRout = StartCoroutine(DoError());
    }

    private void StartCorrect(int digitIndex, int digitAnswer) {
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