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

    public const string blobAttackName = "attack";
    public const string blobBonusName = "bonus";

    [Header("Blob Info")]
    public BlobTemplateData blobAttackTemplate; //used for spawning an attack blob
    public int blobSpawnCount = 4;

    [Header("Numbers")]    
    public NumberGroup[] numberGroups;
    public bool numberCriteriaUnlocked; //if true, no blob connect restriction is made

    [Header("Bonus Number")]
    public NumberGroup numberBonusGroup; //set numbers to none to disable bonus
    public string numberBonusModal;
    public M8.GenericParamSerialized[] numberBonusModalParams; //extra parameters to be passed to modal

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

    [Header("Debug")]
    public bool debugAutoGenerateAttackBlob = false;

    public int curRoundIndex { get; private set; }
    public int roundCount { get { return mRoundCount; } }
    public OperatorType curRoundOp { get { return OperatorType.Multiply; } }
    public int comboCount { get; private set; }
    public bool comboIsActive { get { return comboCount > 1; } }
    public int curScore { get; private set; }
    public int blobClearedCount { get; private set; }

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

    private int mBonusCount;

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

        if(isBonusEnabled)
            numberBonusGroup.Init(blobSpawner);

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

        mBonusCount = 0;

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

        curRoundIndex = 0;
        while(curRoundIndex < mRoundCount) {
            connectControl.curOp = curRoundOp;

            //signal new round
            roundBeginCallback?.Invoke();

            var roundBeginLastTime = Time.time;

            //wait for correct answer
            mIsAnswerCorrectWait = true;
            while(mIsAnswerCorrectWait) {
                yield return null;
            }

            //determine rounds left based on blob clear count (from attack success)
            var newRoundIndex = blobClearedCount / 2;
            if(curRoundIndex < newRoundIndex) {
                var deltaCount = newRoundIndex - curRoundIndex;

                //update rounds display
                for(int i = 0; i < deltaCount; i++) {
                    var ind = curRoundIndex + i;
                    if(ind < mSpriteColorFromPalettes.Length) //fail-safe
                        mSpriteColorFromPalettes[ind].brightness = roundCompleteBrightness;
                }

                curRoundIndex = newRoundIndex;
            }

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

        //check for perfect
        if(mMistakeTotal.totalMistakeCount == 0)
            curScore += GameData.instance.perfectPoints;

        //show victory
        var parms = new M8.GenericParams();
        /*parms[ModalVictory.parmLevel] = levelIndex;
        parms[ModalVictory.parmScore] = curScore;
        parms[ModalVictory.parmBonusRoundScore] = mBonusRoundScore;
        parms[ModalVictory.parmTime] = playTotalTime;
        parms[ModalVictory.parmRoundsCount] = mRoundOps.Length;
        parms[ModalVictory.parmMistakeCount] = mistakeCount;
        parms[ModalVictory.parmNextScene] = nextScene;*/

        M8.ModalManager.main.Open(GameData.instance.modalVictory, parms);
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

            blobSpawner.Spawn(blobBonusName, numberBonusGroup.blobData, bonusNumber);
        }

        mSpawnRout = null;
    }

    IEnumerator DoAttack(BlobConnectController.Group grp) {
        var bonusBlob = blobSpawner.GetBlobActiveByName(blobBonusName);
        var bonusBlobIsConnected = bonusBlob ? grp.IsBlobInGroup(bonusBlob) : false;

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
        //mAreaOp.Setup(256, 4);

        mMistakeCurrent.Reset();

        string attackModal;

        //if(true) {
        if(bonusBlobIsConnected) {
            attackModal = numberBonusModal;

            M8.GenericParamSerialized.ApplyAll(mModalAttackParms, numberBonusModalParams);
        }
        else
            attackModal = GameData.instance.modalAttackDistributive;

        mModalAttackParms.SetAreaOperation(mAreaOp);
        mModalAttackParms.SetMistakeInfo(mMistakeCurrent);

        M8.ModalManager.main.Open(attackModal, mModalAttackParms);

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
                connectControl.ClearGroup(grp);

                //clear out bonus blob
                if(bonusBlob)
                    bonusBlob.state = Blob.State.Despawning;
                break;

            case AttackState.Fail:
                connectControl.GroupEvaluate(grp);

                //clear out bonus blob
                if(bonusBlob) {
                    if(bonusBlob.state != Blob.State.Despawning) {
                        //wait for it to return normal (if it was part of the group during error animation)
                        while(bonusBlob.state != Blob.State.Normal)
                            yield return null;

                        bonusBlob.state = Blob.State.Despawning;
                    }
                }
                break;

            case AttackState.Success:
                //generate attack blob and connect it to the group to be evaluated

                //do fancy animation on board

                /////////////////////
                blobSpawner.Spawn(blobAttackName, mBlobAttackTemplateInd, grp.blobOpLeft.number * grp.blobOpRight.number);

                while(blobSpawner.isSpawning)
                    yield return null;
                
                var attackBlob = blobSpawner.GetBlobActiveByName(blobAttackName);

                //wait for attack blob to spawn completely
                while(attackBlob.state == Blob.State.Spawning)
                    yield return null;

                //connect
                connectControl.SetGroupEqual(grp, !grp.isOpLeftGreaterThanRight, attackBlob);

                yield return new WaitForSeconds(1f);

                //evaluate
                connectControl.GroupEvaluate(grp);
                /////////////////////

                //check if bonus blob is part of the group, then clear out the other blobs on board
                if(bonusBlobIsConnected) {
                    //wait for bonus blob to release
                    while(bonusBlob.state != Blob.State.None)
                        yield return null;

                    //do fancy animation on board

                    for(int i = 0; i < blobSpawner.blobActives.Count; i++) {
                        var blob = blobSpawner.blobActives[i];
                        if(blob)
                            blob.state = Blob.State.Despawning;

                        blobClearedCount++;
                    }

                    blobSpawner.SpawnStop(); //fail-safe
                }
                else if(bonusBlob) //clear it out
                    bonusBlob.state = Blob.State.Despawning;

                blobClearedCount += 2;

                //go to next round
                mIsAnswerCorrectWait = false;
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

        var attackBlob = blobSpawner.GetBlobActiveByName(blobAttackName);

        //wait for attack blob to spawn completely
        while(attackBlob.state == Blob.State.Spawning)
            yield return null;

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
                if(!debugAutoGenerateAttackBlob)
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
        bool isOpComplete = grp.isComplete;

        if(isOpComplete) {
            switch(op) {
                case OperatorType.Multiply:
                    isCorrect = op1 * op2 == eq;
                    break;
                case OperatorType.Divide:
                    isCorrect = op1 / op2 == eq;
                    break;
            }
        }

        bool isBonus = grp.IsBlobInGroupByName(blobBonusName);

        Blob blobLeft = grp.blobOpLeft, blobRight = grp.blobOpRight, blobEq = grp.blobEq;
        BlobConnect connectOp = grp.connectOp, connectEq = grp.connectEq;

        if(isCorrect) {
            //do sparkly thing for blobs
            if(blobLeft) {
                blobLeft.state = Blob.State.Correct;
                blobSpawner.RemoveFromActive(blobLeft);
            }

            if(blobLeft) {
                blobRight.state = Blob.State.Correct;
                blobSpawner.RemoveFromActive(blobRight);
            }

            if(blobEq) {
                blobEq.state = Blob.State.Correct;
                blobSpawner.RemoveFromActive(blobEq);
            }

            //clean out op
            if(connectOp) connectOp.state = BlobConnect.State.Correct;
            if(connectEq) connectEq.state = BlobConnect.State.Correct;

            //add score
            curScore += GameData.instance.correctPoints * comboCount;

            //add bonus
            if(isBonus) {
                curScore += GameData.instance.bonusPoints;

                mBonusCount++;
            }

            comboCount++;
        }
        else {
            //do error thing for blobs
            if(blobLeft) blobLeft.state = Blob.State.Error;
            if(blobRight) blobRight.state = Blob.State.Error;
            if(blobEq) blobEq.state = Blob.State.Error;

            //clean out op
            if(connectOp) connectOp.state = BlobConnect.State.Error;
            if(connectEq) connectEq.state = BlobConnect.State.Error;

            //decrement combo count
            if(comboCount > 1)
                comboCount--;
        }

        //append error count, bonus not counted (special operations potentially not accounted-for)
        if(!isBonus)
            mMistakeTotal.Append(mMistakeCurrent);

        connectControl.ClearGroup(grp);

        //only call if the whole operation is valid
        if(isOpComplete)
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
