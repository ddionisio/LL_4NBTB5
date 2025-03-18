using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class PlayController : GameModeController<PlayController> {
    [System.Serializable]
    public class NumberGroup {
        public int[] leftNumbers;
        public int[] rightNumbers;

        public int leftNumber {
            get {
                var num = leftNumbers[mCurLeftIndex];

                mCurLeftIndex++;
                if(mCurLeftIndex == leftNumbers.Length)
                    mCurLeftIndex = 0;

                return num;
            }
        }

        public int rightNumber {
            get {
                var num = rightNumbers[mCurRightIndex];

                mCurRightIndex++;
                if(mCurRightIndex == rightNumbers.Length)
                    mCurRightIndex = 0;

                return num;
            }
        }

        private int mCurLeftIndex;
        private int mCurRightIndex;

        public void Init() {
            M8.ArrayUtil.Shuffle(leftNumbers);
            M8.ArrayUtil.Shuffle(rightNumbers);
        }
    }

    public const string blobAttackName = "attack";
    public const string blobBonusName = "bonus";

    [Header("Blob Info")]
    public BlobTemplateData blobAttackTemplate; //used for spawning an attack blob
    public int blobSpawnLimit = 4;

    [Header("Numbers")]
    public BlobTemplateData blobLeftTemplate;
    public BlobTemplateData blobRightTemplate;
    public NumberGroup[] numberGroups; //determines rounds
    public bool numberCriteriaUnlocked; //if true, no blob connect restriction is made

    [Header("Bonus Number")]
    public BlobTemplateData blobBonusTemplate;
    public int[] numberBonuses; //set numbers to none to disable bonus
    public string numberBonusModal;
    public M8.GenericParamSerialized[] numberBonusModalParams; //extra parameters to be passed to modal

    [Header("Rounds")]
    public Transform roundsRoot; //grab SpriteColorFromPalette for each child
    public float roundCompleteBrightness = 0.3f;

    [Header("Controls")]
    public BlobConnectController connectControl;
    public BlobSpawner blobSpawner;

    [Header("Correct Info")]
    public float correctSpawnDelay = 0.5f;
    public float correctEvaluateDelay = 1f;

    [Header("Bonus Info")]
    public float bonusClearStartDelay = 0.4f;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeBegin;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeEnd;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeCorrect;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeBonus;

    [Header("Music")]
    [M8.MusicPlaylist]
    public string playMusic;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxBonusClear;
    [M8.SoundPlaylist]
    public string sfxBonusClearBlob;

    [Header("Signal Listen")]
    public M8.Signal signalListenPlayStart;
    public SignalBlob signalListenBlobClick;
    public SignalBlob signalListenBlobDragBegin;
    public SignalBlob signalListenBlobDragEnd;

    public SignalAttackState signalListenAttackStateChanged;

    [Header("Signal Invoke")]
    public M8.SignalInteger signalInvokeScoreUpdate;
    public M8.SignalBoolean signalInvokeActionActive;
    public M8.Signal signalInvokePlayEnd;

    public int roundCount { get { return numberGroups.Length; } }
    public int curRoundIndex { get; private set; }
    public OperatorType curRoundOp { get { return OperatorType.Multiply; } }
    public int comboCount { get; private set; }
    public bool comboIsActive { get { return comboCount > 1; } }
    public int curScore { 
        get { return mCurScore; } 

        private set {
            if(mCurScore != value) {
                mCurScore = value;
                signalInvokeScoreUpdate?.Invoke(value);
            }
        }
    }

    public int blobClearedCount { get; private set; }

    public float curPlayTime { get { return Time.time - mPlayLastTime; } }

    public bool isBonusEnabled { get { return numberBonuses != null && numberBonuses.Length > 0 && mBlobBonusTemplateInd != -1; } }

    public MistakeInfo mistakeCurrent { get { return mMistakeCurrent; } }
    public MistakeInfo mistakeTotal { get { return mMistakeTotal; } }

    public bool isActionActive { get { return mActionRout != null; } }

    //callbacks
    public event System.Action roundBeginCallback;
    public event System.Action roundEndCallback;
    public event System.Action<Operation, int, bool> groupEvalCallback; //params: equation, answer, isCorrect

    private int mBlobAttackTemplateInd;
    private int mBlobLeftTemplateInd;
    private int mBlobRightTemplateInd;
    private int mBlobBonusTemplateInd;

    private bool mIsRoundWait;

    private float mPlayLastTime;

    private RoundWidget[] mRoundWidgets;

    private Coroutine mSpawnRout;
    private Coroutine mActionRout;

    private AttackState mCurAttackState;

    private AreaOperation mAreaOp = new AreaOperation();

    private MistakeInfo mMistakeCurrent;
    private MistakeInfo mMistakeTotal;

    private int mBonusCount;

    private ModalAttackParams mModalAttackParms;
    private M8.GenericParams mModalDigitRemoverParms = new M8.GenericParams();

    private Blob mBlobClicked;

    private int mCurScore = 0;

    public void StartDigitDestroyer() {
        if(mActionRout != null)
            StopCoroutine(mActionRout);

        mActionRout = StartCoroutine(DoDigitDestroyer());
    }

    public void DebugClearActiveBlobs() {
        for(int i = blobSpawner.blobActives.Count - 1; i >= 0; i--) {
            var blob = blobSpawner.blobActives[i];
            if(blob && blob.state == Blob.State.Normal)
                blob.state = Blob.State.Despawning;

            blobClearedCount++;
        }

        blobClearedCount += blobSpawner.spawnQueueCount;
        blobSpawner.SpawnStop();

        mIsRoundWait = false;
    }

    public void DebugAutoConnect() {
        if(mActionRout != null)
            StopCoroutine(mActionRout);

        mActionRout = StartCoroutine(DoAutoConnect());
    }

    public void DebugComboSubtract() {
        if(comboCount > 1)
            comboCount--;
    }

    protected override void OnInstanceDeinit() {
        mBlobClicked = null;

        if(connectControl) {
            connectControl.groupAddedCallback -= OnGroupAdded;
            connectControl.evaluateCallback -= OnGroupEval;
        }

        if(signalListenPlayStart) signalListenPlayStart.callback -= OnSignalPlayBegin;
        if(signalListenBlobClick) signalListenBlobClick.callback -= OnSignalBlobClick;
        if(signalListenBlobDragBegin) signalListenBlobDragBegin.callback -= OnSignalBlobDragBegin;
        if(signalListenBlobDragEnd) signalListenBlobDragEnd.callback -= OnSignalBlobDragEnd;

        if(signalListenAttackStateChanged) signalListenAttackStateChanged.callback -= OnSignalAttackStateChanged;

        if(mSpawnRout != null) {
            StopCoroutine(mSpawnRout);
            mSpawnRout = null;
        }

        if(mActionRout != null) {
            StopCoroutine(mActionRout);
            mActionRout = null;
        }

        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        //setup template and numbers
        mBlobLeftTemplateInd = blobLeftTemplate ? blobSpawner.InitBlobTemplate(blobLeftTemplate) : -1;
        mBlobRightTemplateInd = blobRightTemplate ? blobSpawner.InitBlobTemplate(blobRightTemplate) : -1;

        for(int i = 0; i < numberGroups.Length; i++) {
            var grp = numberGroups[i];
            grp.Init();
        }

        mBlobBonusTemplateInd = blobBonusTemplate ? blobSpawner.InitBlobTemplate(blobBonusTemplate) : -1;

        if(isBonusEnabled)
            M8.ArrayUtil.Shuffle(numberBonuses);

        mBlobAttackTemplateInd = blobAttackTemplate ? blobSpawner.InitBlobTemplate(blobAttackTemplate) : -1;

        //init rounds display
        mRoundWidgets = roundsRoot.GetComponentsInChildren<RoundWidget>();
        int roundsDisplayCount = Mathf.Min(roundCount, mRoundWidgets.Length);

        for(int i = 0; i < roundsDisplayCount; i++)
            mRoundWidgets[i].Setup(true);

        for(int i = roundsDisplayCount; i < mRoundWidgets.Length; i++) //deactivate the rest
            mRoundWidgets[i].gameObject.SetActive(false);

        comboCount = 1;

        mCurAttackState = AttackState.None;

        mMistakeCurrent = new MistakeInfo(GameData.instance.mistakeCount);
        mMistakeTotal = new MistakeInfo(GameData.instance.mistakeCount);

        mBonusCount = 0;

        mModalAttackParms = new ModalAttackParams();
        mModalAttackParms.SetAreaOperation(mAreaOp);
        mModalAttackParms.SetMistakeInfo(mMistakeCurrent);

        BlobConnectController.checkBlobConnectCriteriaDisabled = numberCriteriaUnlocked;

		connectControl.groupAddedCallback += OnGroupAdded;
        connectControl.evaluateCallback += OnGroupEval;

        if(signalListenPlayStart) signalListenPlayStart.callback += OnSignalPlayBegin;
        if(signalListenBlobClick) signalListenBlobClick.callback += OnSignalBlobClick;
        if(signalListenBlobDragBegin) signalListenBlobDragBegin.callback += OnSignalBlobDragBegin;
        if(signalListenBlobDragEnd) signalListenBlobDragEnd.callback += OnSignalBlobDragEnd;

        if(signalListenAttackStateChanged) signalListenAttackStateChanged.callback += OnSignalAttackStateChanged;
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        //music
        if(!string.IsNullOrEmpty(playMusic))
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

    void OnSignalBlobClick(Blob blob) {
        mBlobClicked = blob;
    }

    void OnSignalBlobDragBegin(Blob blob) {
        if(connectControl.isDragDisabled)
            return;

        var blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];
            if(blobActive == blob)
                continue;

            if(numberCriteriaUnlocked || BlobConnectController.CheckBlobConnectCriteria(blob, blobActive)) {
                blobActive.highlightLock = true;
            }
            else {
                blobActive.inputLocked = true;
            }
        }
    }

    void OnSignalBlobDragEnd(Blob blob) {
        if(connectControl.isDragDisabled)
            return;

        var blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];

            blobActive.highlightLock = false;
            blobActive.inputLocked = false;
        }
    }

    void OnSignalAttackStateChanged(AttackState toState) {
        mCurAttackState = toState;
    }

    IEnumerator DoRounds() {

        bool isRoundsContinue = true;
        curRoundIndex = 0;

        while(isRoundsContinue) {
            connectControl.curOp = curRoundOp;

            //signal new round
            roundBeginCallback?.Invoke();

            var roundBeginLastTime = Time.time;

            //wait for correct answer
            mIsRoundWait = true;
            while(mIsRoundWait) {
                yield return null;
            }

            //determine rounds left based on blob clear count (from attack success)
            var newRoundIndex = blobClearedCount / 2;
            if(curRoundIndex < newRoundIndex) {
                var deltaCount = newRoundIndex - curRoundIndex;

                //update rounds display
                for(int i = 0; i < deltaCount; i++) {
                    var ind = curRoundIndex + i;
                    if(ind < mRoundWidgets.Length) //fail-safe
                        mRoundWidgets[ind].Clear();
                }

                curRoundIndex = newRoundIndex;
            }

            isRoundsContinue = curRoundIndex < roundCount;

            //all rounds finish, check for perfect
            if(!isRoundsContinue && mMistakeTotal.totalMistakeCount == 0)
                curScore += GameData.instance.perfectPoints;

            //signal complete round
            roundEndCallback?.Invoke();

            mBlobClicked = null;
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

        parms[ModalVictory.parmMistakeInfo] = mMistakeTotal;
        parms[ModalVictory.parmScore] = curScore;
        parms[ModalVictory.parmComboCount] = comboCount;
        parms[ModalVictory.parmBonusCount] = mBonusCount;
        parms[ModalVictory.parmRoundCount] = roundCount;

        M8.ModalManager.main.Open(GameData.instance.modalVictory, parms);
    }

    IEnumerator DoBlobSpawn() {
        int blobSpawnCount = 0;
        int blobSpawnMax = roundCount * 2;

        int curGroupNumberIndex = 0;

        bool isLeft = true;

        bool isComplete = false;
        while(!isComplete) {
            while(blobSpawner.isSpawning)
                yield return null;

            //check if we have enough on the board
            while(blobSpawner.blobActives.Count + blobSpawner.spawnQueueCount < blobSpawnLimit) {
                var numGrp = numberGroups[curGroupNumberIndex];

                var templateInd = isLeft ? mBlobLeftTemplateInd : mBlobRightTemplateInd;

                var newNumber = isLeft ? numGrp.leftNumber : numGrp.rightNumber;

                blobSpawner.Spawn(templateInd, newNumber);
                blobSpawnCount++;

                //check if we are finish
                if(blobSpawnCount >= blobSpawnMax) {
                    isComplete = true;
                    break;
                }

                //go to next number or group
                if(isLeft)
                    isLeft = false;
                else {
                    isLeft = true;

                    curGroupNumberIndex++;
                    if(curGroupNumberIndex == numberGroups.Length)
                        curGroupNumberIndex = 0;
                }
            }

            yield return null;
        }

        //spawn bonus blob as last blob
        if(isBonusEnabled) {
            var bonusNumber = numberBonuses[0];

            blobSpawner.Spawn(blobBonusName, mBlobBonusTemplateInd, bonusNumber);

            //show first time explanation
            var isBonusBlobDialogDone = LoLManager.instance.userData.GetInt(GameData.userDataKeyFTUEBonusBlob, 0) > 0;
            if(!isBonusBlobDialogDone) {
                //wait for bonus blob to appear
                while(blobSpawner.isSpawning)
                    yield return null;

                //make sure there's no connections made yet
                if(connectControl.activeGroup == null) {
                    LoLManager.instance.userData.SetInt(GameData.userDataKeyFTUEBonusBlob, 1);

                    //clear out dragging
                    connectControl.ReleaseDragging();

                    var camCtrl = CameraController.main;
                    if(camCtrl)
                        camCtrl.raycastTarget = false; //disable blob input

                    var bonusBlob = blobSpawner.GetBlobActiveByName(blobBonusName);

                    while(bonusBlob.state != Blob.State.Normal)
                        yield return null;

                    //select bonus blob
                    bonusBlob.highlightLock = true;

                    //dialog
                    yield return GameData.instance.bonusBlobDialog.Play();

                    bonusBlob.highlightLock = false;

                    if(camCtrl)
                        camCtrl.raycastTarget = true; //resume input
                }
            }
        }

        mSpawnRout = null;
    }

    IEnumerator DoDigitDestroyer() {
        signalInvokeActionActive?.Invoke(true);

        connectControl.isDragDisabled = true;

        //wait for blobs to spawn
        while(blobSpawner.isSpawning)
            yield return null;

        //highlight potential targets
        var blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];
            if(!(blobActive.state == Blob.State.Normal || blobActive.state == Blob.State.Spawning)) //don't include things that are despawning, etc.
                continue;

            while(blobActive.state == Blob.State.Spawning)
                yield return null;

            if(blobActive.name != blobBonusName && WholeNumber.NonZeroCount(blobActive.number) > 1) {
                blobActive.highlightLock = true;
                blobActive.inputLocked = false;
            }
            else {
                blobActive.inputLocked = true;
            }
        }

        //wait for a valid blob click
        mBlobClicked = null;
        while(!mBlobClicked)
            yield return null;

        //open modal for digit removal
        mModalDigitRemoverParms[ModalDigitRemover.parmBlob] = mBlobClicked;

        M8.ModalManager.main.Open(GameData.instance.modalDigitRemover, mModalDigitRemoverParms);

        //wait for process to finish
        while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(GameData.instance.modalDigitRemover))
            yield return null;

        //clear out highlights
        blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];

            blobActive.highlightLock = false;
            blobActive.inputLocked = false;
        }

        connectControl.isDragDisabled = false;

        //add penalty counter for later
        mBlobClicked.penaltyCounter++;

        mBlobClicked = null;

        mActionRout = null;

        signalInvokeActionActive?.Invoke(false);
    }

    IEnumerator DoAutoConnect() {
        signalInvokeActionActive?.Invoke(true);

        //wait for blobs to spawn
        while(blobSpawner.isSpawning)
            yield return null;

        //grab a blob
        Blob blob = null;

        for(int i = blobSpawner.blobActives.Count - 1; i >= 0; i--) {
            var blobCheck = blobSpawner.blobActives[i];
            if(blobCheck && blobCheck.state == Blob.State.Normal) {
                blob = blobCheck;
                break;
            }
        }

        if(!blob)
            yield break;

        //get another blob
        Blob blobTarget = null;
        for(int i = 0; i < blobSpawner.blobActives.Count; i++) {
            var blobCheck = blobSpawner.blobActives[i];
            if(blobCheck && blobCheck != blob && blobCheck.state == Blob.State.Normal 
                && (numberCriteriaUnlocked || BlobConnectController.CheckBlobConnectCriteria(blob, blobCheck))) {
                blobTarget = blobCheck;
                break;
            }
        }

        if(!blobTarget)
            yield break;

        var grp = connectControl.GenerateConnect(blob, blobTarget, curRoundOp);
        if(grp == null)
            yield break;

        mMistakeCurrent.Reset();

        yield return DoAttackSuccess(grp);

        mActionRout = null;

        signalInvokeActionActive?.Invoke(false);
    }

    IEnumerator DoAttack(BlobConnectController.Group grp) {
        signalInvokeActionActive?.Invoke(true);

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
        //mAreaOp.Setup(428, 4);

        mMistakeCurrent.Reset();

        string attackModal;

        //if(true) {
        if(bonusBlobIsConnected) {
            attackModal = numberBonusModal;

            M8.GenericParamSerialized.ApplyAll(mModalAttackParms, numberBonusModalParams);
        }
        else
            attackModal = GameData.instance.modalAttackDistributive;

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

                //decrement combo count
                if(comboCount > 1)
                    comboCount--;
                break;

            case AttackState.Success:
                yield return DoAttackSuccess(grp);
                break;
        }

        mActionRout = null;

        signalInvokeActionActive?.Invoke(false);
    }

    IEnumerator DoAttackSuccess(BlobConnectController.Group grp) {
        var bonusBlob = blobSpawner.GetBlobActiveByName(blobBonusName);
        var bonusBlobIsConnected = bonusBlob ? grp.IsBlobInGroup(bonusBlob) : false;

        var camCtrl = CameraController.main;
        if(camCtrl)
            camCtrl.raycastTarget = false; //disable blob input

        //generate attack blob and connect it to the group to be evaluated

        //do fancy animation on board
        if(animator && !string.IsNullOrEmpty(takeCorrect))
            animator.Play(takeCorrect);

        yield return new WaitForSeconds(correctSpawnDelay);

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

        yield return new WaitForSeconds(correctEvaluateDelay);

        //evaluate
        connectControl.GroupEvaluate(grp);

        //wait for attack blob to release
        while(attackBlob.state != Blob.State.None)
            yield return null;
        /////////////////////

        blobClearedCount += 2;

        //increase combo if we still have rounds left
        if(blobClearedCount / 2 < roundCount)
            comboCount++;

        //check if bonus blob is part of the group, then clear out the other blobs on board
        if(bonusBlobIsConnected) {
            //ensure blobs are no longer being spawned
            var bonusClearCount = blobSpawner.spawnQueueCount;
            blobSpawner.SpawnStop();

            if(!string.IsNullOrEmpty(sfxBonusClear))
                M8.SoundPlaylist.instance.Play(sfxBonusClear, false);

            //do fancy animation on board
            if(animator && !string.IsNullOrEmpty(takeBonus))
                animator.Play(takeBonus);

            yield return new WaitForSeconds(bonusClearStartDelay);

            for(int i = blobSpawner.blobActives.Count - 1; i >= 0; i--) {
                var blob = blobSpawner.blobActives[i];
                if(blob) {
                    //wait for it to spawn
                    while(blob.state == Blob.State.Spawning)
                        yield return null;

                    if(!string.IsNullOrEmpty(sfxBonusClearBlob))
                        M8.SoundPlaylist.instance.Play(sfxBonusClearBlob, false);

                    blob.state = Blob.State.Correct;

                    while(blob.state != Blob.State.None)
                        yield return null;

                    bonusClearCount++;
                }
            }

            //add up score for blobs cleared
            int bonusRoundCount = bonusClearCount / 2;

            for(int i = 0; i < bonusRoundCount; i++) {
                curScore += GameData.instance.correctPoints * comboCount;

                if(i < bonusRoundCount - 1)
                    comboCount++;
            }
            //

            blobClearedCount += bonusClearCount;
        }
        else if(bonusBlob) //clear it out
            bonusBlob.state = Blob.State.Despawning;

        //go to next round
        mIsRoundWait = false;

        if(camCtrl)
            camCtrl.raycastTarget = true;
    }

    void OnGroupAdded(BlobConnectController.Group grp) {
        if(grp.isOpFilled) {
            //Debug.Log("Launch attack: " + grp.blobOpLeft.number + " x " + grp.blobOpRight.number);
            if(mActionRout == null)
                mActionRout = StartCoroutine(DoAttack(grp));
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

        if(grp.isComplete) {
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
            int penaltyCounter = 0;

            //do sparkly thing for blobs
            if(blobLeft) {
                blobLeft.state = Blob.State.Correct;
                blobSpawner.RemoveFromActive(blobLeft);

                penaltyCounter += blobLeft.penaltyCounter;
            }

            if(blobRight) {
                blobRight.state = Blob.State.Correct;
                blobSpawner.RemoveFromActive(blobRight);

                penaltyCounter += blobRight.penaltyCounter;
            }

            if(blobEq) {
                blobEq.state = Blob.State.Correct;
                blobSpawner.RemoveFromActive(blobEq);
            }

            //clean out op
            if(connectOp) connectOp.state = BlobConnect.State.Correct;
            if(connectEq) connectEq.state = BlobConnect.State.Correct;

            int correctPoints = GameData.instance.correctPoints;

            //subtract mistake penalties
            if(mMistakeCurrent.totalMistakeCount > 0)
                correctPoints -= GameData.instance.mistakePenaltyPoints * mMistakeCurrent.totalMistakeCount;

            //subtract penalties
            if(penaltyCounter > 0)
                correctPoints -= penaltyCounter * GameData.instance.digitDestroyPenalityPoints;

            if(correctPoints < 0)
                correctPoints = 0;

            //add score
            curScore += correctPoints * comboCount;

            //add bonus
            if(isBonus) {
                curScore += GameData.instance.bonusPoints;

                mBonusCount++;
            }
        }
        else {
            //do error thing for blobs
            if(blobLeft) blobLeft.state = Blob.State.Error;
            if(blobRight) blobRight.state = Blob.State.Error;
            if(blobEq) blobEq.state = Blob.State.Error;

            //clean out op
            if(connectOp) connectOp.state = BlobConnect.State.Error;
            if(connectEq) connectEq.state = BlobConnect.State.Error;
        }

        //append error count, bonus not counted (special operations potentially not accounted-for)
        if(!isBonus)
            mMistakeTotal.Append(mMistakeCurrent);

        connectControl.ClearGroup(grp);

        groupEvalCallback?.Invoke(new Operation { operand1=(int)op1, operand2= (int)op2, op=op }, (int)eq, isCorrect);
    }
}
