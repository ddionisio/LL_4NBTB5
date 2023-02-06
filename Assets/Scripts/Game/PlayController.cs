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

        public bool isFinish { get { return mCurNumIndex == numbers.Length; } }

        private int mCurNumIndex;

        public void Init(BlobSpawner blobSpawner) {
            M8.ArrayUtil.Shuffle(numbers);

            templateIndex = blobSpawner.GetTemplateIndex(blobData);

            mCurNumIndex = 0;
        }
    }

    [Header("Settings")]
    public int levelIndex;
    public string modalVictory = "victory";    
    public M8.SceneAssetPath nextScene; //after victory

    [Header("Numbers")]
    public NumberGroup[] numberGroups;
    public NumberGroup numberBonusGroup; //set numbers to none to disable bonus

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

    public bool isBonusEnabled { get { return numberBonusGroup.blobData == null || numberBonusGroup.numbers.Length == 0; } }

    //callbacks
    public event System.Action roundBeginCallback;
    public event System.Action roundEndCallback;
    public event System.Action<Operation, int, bool> groupEvalCallback; //params: equation, answer, isCorrect

    private int mRoundCount;

    private bool mIsAnswerCorrectWait;

    private float mPlayLastTime;

    private int mMistakeRoundCount;

    private M8.SpriteColorFromPalette[] mSpriteColorFromPalettes;

    private Coroutine mSpawnRout;

    protected override void OnInstanceDeinit() {
        if(connectControl)
            connectControl.evaluateCallback -= OnGroupEval;

        signalListenPlayStart.callback -= OnSignalPlayBegin;

        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        //setup numbers
        int numberCount = 0;

        for(int i = 0; i < numberGroups.Length; i++) {
            var grp = numberGroups[i];
            grp.Init(blobSpawner);

            numberCount += grp.numbers.Length;
        }

        mRoundCount = Mathf.FloorToInt(numberCount / 2.0f);

        //init rounds display
        int roundsDisplayCount = Mathf.Min(mRoundCount, roundsRoot.childCount);
        mSpriteColorFromPalettes = new M8.SpriteColorFromPalette[roundsDisplayCount];
        for(int i = 0; i < roundsDisplayCount; i++) {
            mSpriteColorFromPalettes[i] = roundsRoot.GetChild(i).GetComponent<M8.SpriteColorFromPalette>();
        }

        for(int i = roundsDisplayCount; i < roundsRoot.childCount; i++) //deactivate the rest
            roundsRoot.GetChild(i).gameObject.SetActive(false);

        connectControl.evaluateCallback += OnGroupEval;

        signalListenPlayStart.callback += OnSignalPlayBegin;
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

    IEnumerator DoRounds() {

        for(curRoundIndex = 0; curRoundIndex < mRoundCount; curRoundIndex++) {
            connectControl.curOp = curRoundOp;

            //signal new round
            roundBeginCallback?.Invoke();

            var roundBeginLastTime = Time.time;

            mMistakeRoundCount = 0;
                        
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
        var maxBlobCount = GameData.instance.blobSpawnCount;

        int curGroupNumberIndex = 0;

        bool isComplete = false;
        while(!isComplete) {
            //check if we have enough on the board
            while(blobSpawner.blobActives.Count + blobSpawner.spawnQueueCount < maxBlobCount) {
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

            comboCount++;

            //add score
            curScore += GameData.instance.correctPoints * comboCount;

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
            if(comboCount > 0)
                comboCount--;

            mistakeCount++;
            mMistakeRoundCount++;
        }

        connectControl.ClearGroup(grp);

        groupEvalCallback?.Invoke(new Operation { operand1=(int)op1, operand2= (int)op2, op=op }, (int)eq, isCorrect);
    }
}
