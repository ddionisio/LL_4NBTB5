using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class PlayController : GameModeController<PlayController> {
    [System.Serializable]
    public class NumberGroup {
        public BlobTemplateData blobData;
        public int[] numbers;

        public int templateIndex { get; private set; }

        public int number {
            get {
                if(mCurNumIndex < numbers.Length) {
                    var num = numbers[mCurNumIndex];

                    mCurNumIndex++;

                    return num;
                }
                else
                    return numbers[numbers.Length - 1];
            }
        }

        public bool isFinish { get { return mCurNumIndex >= numbers.Length; } }

        private int mCurNumIndex;

        public void Init(BlobSpawner blobSpawner) {
            M8.ArrayUtil.Shuffle(numbers);

            templateIndex = blobSpawner.GetTemplateIndex(blobData);

            blobSpawner.InitBlobTemplate(templateIndex);

            mCurNumIndex = 0;
        }
    }

    public const string blobAttackName = "attackBlob";

    [Header("Settings")]
    public int levelIndex;
    public string modalVictory = "victory";    
    public M8.SceneAssetPath nextScene; //after victory

    [Header("Numbers")]    
    public NumberGroup[] numberGroups;
    public NumberGroup numberBonusGroup; //set numbers to none to disable bonus
    public BlobTemplateData blobAttackTemplate; //used for spawning an attack blob
    public bool numberCriteriaUnlocked; //if true, no blob connect restriction is made
    public int blobSpawnCount = 4;

    [Header("Controls")]
    public BlobConnectController connectControl;
    public BlobSpawner blobSpawner;

    [Header("Rounds")]
    public Transform roundsRoot; //grab SpriteColorFromPalette for each child
    public float roundCompleteBrightness = 0.3f;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeBegin;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeEnd;

    [Header("Music")]
    [M8.MusicPlaylist]
    public string playMusic;

    [Header("Signal Listen")]
    public M8.Signal signalListenPlayStart;
    public SignalBlob signalListenBlobDragBegin;
    public SignalBlob signalListenBlobDragEnd;

    public SignalAttackState signalListenAttackStateChanged;

    [Header("Signal Invoke")]
    public M8.Signal signalInvokePlayEnd;

    public int curRoundIndex { get; private set; }
    public int roundCount { get { return mRoundCount; } }
    public OperatorType curRoundOp { get { return OperatorType.Multiply; } }
    public int comboCount { get; private set; }
    public bool comboIsActive { get { return comboCount > 1; } }
    public int curScore { get; private set; }
    public int curNumberIndex { get; private set; }
    public int mistakeCount { get; private set; }

    public float curPlayTime { get { return Time.time - mPlayLastTime; } }

    public bool isBonusEnabled { get { return numberBonusGroup.blobData != null && numberBonusGroup.numbers.Length > 0; } }

    //callbacks
    public event System.Action roundBeginCallback;
    public event System.Action roundEndCallback;
    public event System.Action<Operation, int, bool> groupEvalCallback; //params: equation, answer, isCorrect

    private int mBlobAttackTemplateInd;

    private int mRoundCount;

    private bool mIsAnswerCorrectWait;

    private float mPlayLastTime;

    private M8.SpriteColorFromPalette[] mSpriteColorFromPalettes;

    private Coroutine mSpawnRout;
    private Coroutine mAttackRout;

    private AttackState mCurAttackState;

    private AreaOperation mAreaOp = new AreaOperation();

    private MistakeInfo mMistakeCurrent;
    private MistakeInfo mMistakeTotal;

    private ModalAttackParams mModalAttackParms = new ModalAttackParams();

    protected override void OnInstanceDeinit() {
        if(connectControl) {
            connectControl.groupAddedCallback -= OnGroupAdded;
            connectControl.evaluateCallback -= OnGroupEval;
        }

        signalListenPlayStart.callback -= OnSignalPlayBegin;
        signalListenBlobDragBegin.callback -= OnSignalBlobDragBegin;
        signalListenBlobDragEnd.callback -= OnSignalBlobDragEnd;

        signalListenAttackStateChanged.callback -= OnSignalAttackStateChanged;

        if(mSpawnRout != null) {
            StopCoroutine(mSpawnRout);
            mSpawnRout = null;
        }

        if(mAttackRout != null) {
            StopCoroutine(mAttackRout);
            mAttackRout = null;
        }

        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        //setup template and numbers
        int numberCount = 0;

        for(int i = 0; i < numberGroups.Length; i++) {
            var grp = numberGroups[i];

            grp.Init(blobSpawner);

            numberCount += grp.numbers.Length;
        }

        mBlobAttackTemplateInd = blobSpawner.GetTemplateIndex(blobAttackTemplate);
        if(mBlobAttackTemplateInd != -1)
            blobSpawner.InitBlobTemplate(mBlobAttackTemplateInd);

        mRoundCount = Mathf.FloorToInt(numberCount / 2.0f);

        //init rounds display
        int roundsDisplayCount = Mathf.Min(mRoundCount, roundsRoot.childCount);
        mSpriteColorFromPalettes = new M8.SpriteColorFromPalette[roundsDisplayCount];
        for(int i = 0; i < roundsDisplayCount; i++) {
            mSpriteColorFromPalettes[i] = roundsRoot.GetChild(i).GetComponent<M8.SpriteColorFromPalette>();
        }

        for(int i = roundsDisplayCount; i < roundsRoot.childCount; i++) //deactivate the rest
            roundsRoot.GetChild(i).gameObject.SetActive(false);

        comboCount = 1;

        mCurAttackState = AttackState.None;

        mMistakeCurrent = new MistakeInfo(GameData.instance.mistakeCount);
        mMistakeTotal = new MistakeInfo(GameData.instance.mistakeCount);

        connectControl.groupAddedCallback += OnGroupAdded;
        connectControl.evaluateCallback += OnGroupEval;

        signalListenPlayStart.callback += OnSignalPlayBegin;
        signalListenBlobDragBegin.callback += OnSignalBlobDragBegin;
        signalListenBlobDragEnd.callback += OnSignalBlobDragEnd;

        signalListenAttackStateChanged.callback += OnSignalAttackStateChanged;
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        //music
        M8.MusicPlaylist.instance.Play(playMusic, true, false);

        //play enter if available
        if(animator && !string.IsNullOrEmpty(takeBegin))
            yield return animator.PlayWait(takeBegin);
    }

    void OnSignalPlayBegin() {
        //start spawning
        StartCoroutine(DoRounds());

        mSpawnRout = StartCoroutine(DoBlobSpawn());

        mPlayLastTime = Time.time;
    }

    void OnSignalBlobDragBegin(Blob blob) {
        var blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];
            if(blobActive == blob)
                continue;

            if(numberCriteriaUnlocked || CheckBlobConnectCriteria(blob, blobActive)) {
                blobActive.highlightLock = true;
            }
            else {
                blobActive.inputLocked = true;
            }
        }
    }

    void OnSignalBlobDragEnd(Blob blob) {
        var blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];
            if(blobActive == blob)
                continue;

            if(numberCriteriaUnlocked || CheckBlobConnectCriteria(blob, blobActive)) {
                blobActive.highlightLock = false;
            }
            else {
                blobActive.inputLocked = false;
            }
        }
    }

    void OnSignalAttackStateChanged(AttackState toState) {
        mCurAttackState = toState;
    }

    IEnumerator DoRounds() {

        for(curRoundIndex = 0; curRoundIndex < mRoundCount; curRoundIndex++) {
            connectControl.curOp = curRoundOp;

            //signal new round
            roundBeginCallback?.Invoke();

            var roundBeginLastTime = Time.time;

            //wait for correct answer
            mIsAnswerCorrectWait = true;
            while(mIsAnswerCorrectWait) {
                yield return null;
            }

            mSpriteColorFromPalettes[curRoundIndex].brightness = roundCompleteBrightness;

            //signal complete round
            roundEndCallback?.Invoke();
        }

        //var playTotalTime = curPlayTime;

        //wait for blobs to clear out
        while(blobSpawner.blobActives.Count > 0)
            yield return null;

        //play finish
        signalInvokePlayEnd.Invoke();

        //play end animation if available
        if(animator && !string.IsNullOrEmpty(takeEnd))
            yield return animator.PlayWait(takeEnd);

        //show victory
        var parms = new M8.GenericParams();
        /*parms[ModalVictory.parmLevel] = levelIndex;
        parms[ModalVictory.parmScore] = curScore;
        parms[ModalVictory.parmBonusRoundScore] = mBonusRoundScore;
        parms[ModalVictory.parmTime] = playTotalTime;
        parms[ModalVictory.parmRoundsCount] = mRoundOps.Length;
        parms[ModalVictory.parmMistakeCount] = mistakeCount;
        parms[ModalVictory.parmNextScene] = nextScene;*/

        M8.ModalManager.main.Open(modalVictory, parms);
    }

    IEnumerator DoBlobSpawn() {
        int curGroupNumberIndex = 0;

        bool isComplete = false;
        while(!isComplete) {
            while(blobSpawner.isSpawning)
                yield return null;

            //check if we have enough on the board
            while(blobSpawner.blobActives.Count + blobSpawner.spawnQueueCount < blobSpawnCount) {
                var numGrp = numberGroups[curGroupNumberIndex];
                if(!numGrp.isFinish) {
                    var newNumber = numGrp.number;

                    blobSpawner.Spawn(numGrp.templateIndex, newNumber);
                }

                curGroupNumberIndex++;
                if(curGroupNumberIndex == numberGroups.Length) {
                    curGroupNumberIndex = 0;

                    //check if we are finish with all the groups
                    int finishCount = 0;
                    for(int i = 0; i < numberGroups.Length; i++) {
                        if(numberGroups[i].isFinish)
                            finishCount++;
                    }

                    if(finishCount == numberGroups.Length) {
                        isComplete = true;
                        break;
                    }
                }
            }

            yield return null;
        }

        //spawn bonus blob as last blob
        if(isBonusEnabled) {
            var bonusNumber = numberBonusGroup.number;

            blobSpawner.Spawn(numberBonusGroup.blobData, bonusNumber);
        }

        mSpawnRout = null;
    }

    IEnumerator DoAttack(BlobConnectController.Group grp) {
        //setup area operation
        int factorLeft, factorRight;

        if(grp.blobOpLeft.number > grp.blobOpRight.number) {
            factorLeft = grp.blobOpLeft.number;
            factorRight = grp.blobOpRight.number;
        }
        else {
            factorLeft = grp.blobOpRight.number;
            factorRight = grp.blobOpLeft.number;
        }

        mAreaOp.Setup(factorLeft, factorRight);

        mMistakeCurrent.Reset();

        mModalAttackParms.SetAreaOperation(mAreaOp);
        mModalAttackParms.SetMistakeInfo(mMistakeCurrent);

        M8.ModalManager.main.Open(GameData.instance.modalAttackDistributive, mModalAttackParms);

        //TODO: background animation

        //wait for state change
        mCurAttackState = AttackState.Attacking;
        while(mCurAttackState == AttackState.Attacking)
            yield return null;

        //wait for modals to close
        while(M8.ModalManager.main.isBusy)
            yield return null;

        //determine course of action based on attack state
        switch(mCurAttackState) {
            case AttackState.Cancel:
                break;

            case AttackState.Fail:
                break;

            case AttackState.Success:
                break;
        }

        mAttackRout = null;
    }

    IEnumerator DoAttackAutoGenerateAttackDebug(BlobConnectController.Group grp) {
        //yield return new WaitForSeconds(1f);
        //connectControl.GroupError(grp);
        //yield return null;

        

        //mModalAttackParms
        //var areaOp = new AreaOperation();
        //areaOp.Init(factorLeft, factorRight);

        //generate attack blob
        blobSpawner.Spawn(blobAttackName, mBlobAttackTemplateInd, grp.blobOpLeft.number * grp.blobOpRight.number);

        while(blobSpawner.isSpawning)
            yield return null;

        yield return new WaitForSeconds(1f);

        var attackBlob = blobSpawner.GetBlobActiveByName(blobAttackName);

        //connect
        connectControl.SetGroupEqual(grp, !grp.isOpLeftGreaterThanRight, attackBlob);

        yield return new WaitForSeconds(1f);

        //evaluate
        connectControl.GroupEvaluate(grp);

        mAttackRout = null;
    }

    void OnGroupAdded(BlobConnectController.Group grp) {
        if(grp.isOpFilled) {
            //Debug.Log("Launch attack: " + grp.blobOpLeft.number + " x " + grp.blobOpRight.number);
            if(mAttackRout == null) {
                if(!GameData.instance.debugAutoGenerateAttackBlob)
                    mAttackRout = StartCoroutine(DoAttack(grp));
                else
                    mAttackRout = StartCoroutine(DoAttackAutoGenerateAttackDebug(grp));
            }
        }
        //else {
            //Debug.Log("Can't attack yet!");
        //}
    }

    void OnGroupEval(BlobConnectController.Group grp) {
        float op1, op2, eq;
        grp.GetNumbers(out op1, out op2, out eq);

        var op = grp.connectOp.op;

        bool isCorrect = false;

        switch(op) {
            case OperatorType.Multiply:
                isCorrect = op1 * op2 == eq;
                break;
            case OperatorType.Divide:
                isCorrect = op1 / op2 == eq;
                break;
        }

        Blob blobLeft = grp.blobOpLeft, blobRight = grp.blobOpRight, blobEq = grp.blobEq;
        BlobConnect connectOp = grp.connectOp, connectEq = grp.connectEq;

        if(isCorrect) {
            //do sparkly thing for blobs
            blobLeft.state = Blob.State.Correct;
            blobRight.state = Blob.State.Correct;
            blobEq.state = Blob.State.Correct;

            blobSpawner.RemoveFromActive(blobLeft);
            blobSpawner.RemoveFromActive(blobRight);
            blobSpawner.RemoveFromActive(blobEq);

            //clean out op
            connectOp.state = BlobConnect.State.Correct;
            connectEq.state = BlobConnect.State.Correct;

            //add score
            curScore += GameData.instance.correctPoints * comboCount;

            comboCount++;

            //go to next round
            mIsAnswerCorrectWait = false;
        }
        else {
            //do error thing for blobs
            blobLeft.state = Blob.State.Error;
            blobRight.state = Blob.State.Error;
            blobEq.state = Blob.State.Error;

            //clean out op
            connectOp.state = BlobConnect.State.Error;
            connectEq.state = BlobConnect.State.Error;

            //decrement combo count
            if(comboCount > 1)
                comboCount--;

            mistakeCount++;
        }

        connectControl.ClearGroup(grp);

        groupEvalCallback?.Invoke(new Operation { operand1=(int)op1, operand2= (int)op2, op=op }, (int)eq, isCorrect);
    }

    private bool CheckBlobConnectCriteria(Blob blobSource, Blob blobTarget) {
        var blobSrcVal = blobSource.number;
        var blobTgtVal = blobTarget.number;

        if(blobSrcVal > 9) {
            //can only connect to single digit values
            return blobTgtVal < 10;
        }
        else {
            //can only connect to two or more digit values
            return blobTgtVal > 9;
        }
    }
}
