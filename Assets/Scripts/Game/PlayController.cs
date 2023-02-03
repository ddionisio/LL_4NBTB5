using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class PlayController : GameModeController<PlayController> {
    [System.Serializable]
    public class NumberGroup {
        public string name;
        public int templateIndex;
        public int[] numbers;
        public bool isBonus;

        public int number { 
            get {
                var num = numbers[mCurNumIndex];

                mCurNumIndex++;
                if(mCurNumIndex == numbers.Length)
                    mCurNumIndex = 0;

                return num;
            } 
        }

        private int mCurNumIndex;

        public void Init() {
            M8.ArrayUtil.Shuffle(numbers);

            mCurNumIndex = 0;
        }
    }

    [Header("Settings")]
    public int levelIndex;
    public string modalVictory = "victory";    
    public M8.SceneAssetPath nextScene; //after victory

    [Header("Numbers")]
    public NumberGroup[] numberGroups;
    public int[] numberIndexLookups; //index to number group

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
        for(int i = 0; i < numberGroups.Length; i++) {
            var grp = numberGroups[i];
            grp.Init();
        }

        mRoundCount = Mathf.FloorToInt(numberIndexLookups.Length / 2.0f);

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
        curNumberIndex = 0;

        while(curNumberIndex < numberIndexLookups.Length) {
            var maxBlobCount = GameData.instance.blobSpawnCount;

            var numGrp = numberGroups[numberIndexLookups[curNumberIndex]];

            //check if we have enough on the board, or the current one is a bonus
            while(blobSpawner.blobActives.Count + blobSpawner.spawnQueueCount < maxBlobCount || numGrp.isBonus) {
                var newNumber = numGrp.number;

                blobSpawner.Spawn(numGrp.templateIndex, newNumber);

                curNumberIndex++;
                if(curNumberIndex == numberIndexLookups.Length)
                    break;
            }

            yield return null;
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
