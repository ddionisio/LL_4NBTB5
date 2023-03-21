using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LoLExt;

using TMPro;

public class Lesson1Controller : GameModeController<Lesson1Controller> {
    [Header("Music")]
    [M8.MusicPlaylist]
    public string playMusic;

    [Header("Lesson Flow")]
    public AnimatorEnterExit blobNumberIllustrate;
    public ModalDialogFlow introDialog;
    public AnimatorEnterExit distributeIllustrate;
    public ModalDialogFlow distributeDialog;
    public AnimatorEnterExit areaIllustrate;
    public ModalDialogFlow areaDialog;
    public ModalDialogFlow tutorialConnectDialog;
    public ModalDialogFlow tutorialAttackDistributiveDialog;
    public ModalDialogFlow tutorialAttackEvalDialog;
    public ModalDialogFlow tutorialAttackSumsDialog;
    public ModalDialogFlow tutorialEndDialog;

    [Header("Board UI")]
    public GameObject boardHUDRootGO;
    public DragToGuideWidget boardDragGuide;
    public RectTransform clickInstructWidgetRoot;
    public Vector2 clickInstructOfs;

    [Header("Equation Display")]
    public M8.Animator.Animate equationOp1Anim; //play take index 0 when highlight
    public TMP_Text equationOp1Text;

    public M8.Animator.Animate equationOp2Anim; //play take index 0 when highlight
    public TMP_Text equationOp2Text;

    public M8.Animator.Animate equationAnsAnim; //play take index 0 when highlight
    public TMP_Text equationAnsText;

    public GameObject equationOpGO;
    public GameObject equationEqGO;

    [Header("Board")]
    public GameObject boardRootGO;

    [Header("Numbers")]    
    public int factor1 = 24;
    public int factor2 = 3;

    public const string blobAttackName = "attack";

    [Header("Blob Info")]
    public BlobTemplateData blobTemplate;
    public BlobTemplateData blobMediumTemplate;
    public BlobTemplateData blobAttackTemplate; //used for spawning an attack blob

    [Header("Controls")]
    public BlobConnectController connectControl;
    public BlobSpawner blobSpawner;

    [Header("Correct Info")]
    public float correctSpawnDelay = 0.5f;
    public float correctEvaluateDelay = 1f;

    [Header("Animation")]
    public M8.Animator.Animate boardAnimator;
    [M8.Animator.TakeSelector(animatorField = "boardAnimator")]
    public string takeBoardBegin;
    [M8.Animator.TakeSelector(animatorField = "boardAnimator")]
    public string takeBoardEnd;
    [M8.Animator.TakeSelector(animatorField = "boardAnimator")]
    public string takeBoardCorrect;

    [Header("Signal Listen")]
    public SignalBlob signalListenBlobDragBegin;
    public SignalBlob signalListenBlobDragEnd;

    public SignalAttackState signalListenAttackStateChanged;

    public M8.SignalBoolean signalListenAttackDistributive;
    public M8.SignalBoolean signalListenAttackEvaluate;
    public M8.SignalBoolean signalListenAttackSums;

    private int mBlobTemplateInd;
    private int mBlobLargeTemplateInd;
    private int mBlobAttackTemplateInd;

    private Coroutine mAttackRout;
    private Coroutine mDragGuideRout;
    private Coroutine mEquationRout;

    private AttackState mCurAttackState;

    private AreaOperation mAreaOp = new AreaOperation();

    private MistakeInfo mMistakeCurrent;

    private ModalAttackParams mModalAttackParms;

    private bool mIsAttackDistributiveExplained;
    private bool mIsAttackEvaluateExplained;
    private bool mIsAttackSumsExplained;

    private bool mIsAttackComplete;

    protected override void OnInstanceDeinit() {
        if(connectControl) {
            connectControl.groupAddedCallback -= OnGroupAdded;
            connectControl.evaluateCallback -= OnGroupEval;
        }

        if(signalListenBlobDragBegin) signalListenBlobDragBegin.callback -= OnSignalBlobDragBegin;
        if(signalListenBlobDragEnd) signalListenBlobDragEnd.callback -= OnSignalBlobDragEnd;

        if(signalListenAttackDistributive) signalListenAttackDistributive.callback -= OnSignalAttackDistributiveActive;
        if(signalListenAttackEvaluate) signalListenAttackEvaluate.callback -= OnSignalAttackEvaluateActive;
        if(signalListenAttackSums) signalListenAttackSums.callback -= OnSignalAttackSumsActive;

        if(signalListenAttackStateChanged) signalListenAttackStateChanged.callback -= OnSignalAttackStateChanged;

        if(mAttackRout != null) {
            StopCoroutine(mAttackRout);
            mAttackRout = null;
        }

        if(mDragGuideRout != null) {
            StopCoroutine(mDragGuideRout);
            mDragGuideRout = null;
        }

        base.OnInstanceDeinit();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(blobNumberIllustrate) blobNumberIllustrate.Hide();
        if(distributeIllustrate) distributeIllustrate.Hide();
        if(areaIllustrate) areaIllustrate.Hide();

        //board stuff
        if(boardHUDRootGO) boardHUDRootGO.SetActive(false);
        if(boardRootGO) boardRootGO.SetActive(false);

        mBlobTemplateInd = blobSpawner.InitBlobTemplate(blobTemplate);
        mBlobLargeTemplateInd = blobSpawner.InitBlobTemplate(blobMediumTemplate);
        mBlobAttackTemplateInd = blobSpawner.InitBlobTemplate(blobAttackTemplate);

        mCurAttackState = AttackState.None;

        connectControl.curOp = OperatorType.Multiply;

        mMistakeCurrent = new MistakeInfo(GameData.instance.mistakeCount);

        mModalAttackParms = new ModalAttackParams();
        mModalAttackParms.SetAreaOperation(mAreaOp);
        mModalAttackParms.SetMistakeInfo(mMistakeCurrent);
        mModalAttackParms.SetShowTutorial(true);

        //ui stuff
        if(equationOp1Text) equationOp1Text.text = "";
        if(equationOp2Text) equationOp2Text.text = "";
        if(equationAnsText) equationAnsText.text = "";

        if(equationOpGO) equationOpGO.SetActive(false);
        if(equationEqGO) equationEqGO.SetActive(false);

        if(clickInstructWidgetRoot) clickInstructWidgetRoot.gameObject.SetActive(false);

        //signals
        connectControl.groupAddedCallback += OnGroupAdded;
        connectControl.evaluateCallback += OnGroupEval;

        signalListenBlobDragBegin.callback += OnSignalBlobDragBegin;
        signalListenBlobDragEnd.callback += OnSignalBlobDragEnd;

        signalListenAttackDistributive.callback += OnSignalAttackDistributiveActive;
        signalListenAttackEvaluate.callback += OnSignalAttackEvaluateActive;
        signalListenAttackSums.callback += OnSignalAttackSumsActive;

        signalListenAttackStateChanged.callback += OnSignalAttackStateChanged;
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        while(!LoLManager.instance.isReady)
            yield return null;

        if(!string.IsNullOrEmpty(playMusic))
            M8.MusicPlaylist.instance.Play(playMusic, true, false);

        //explanation

        if(blobNumberIllustrate) {
            blobNumberIllustrate.Show();
            yield return blobNumberIllustrate.PlayEnterWait();
        }

        yield return introDialog.Play();

        if(blobNumberIllustrate) {
            yield return blobNumberIllustrate.PlayExitWait();
            blobNumberIllustrate.Hide();
        }

        if(distributeIllustrate) {
            distributeIllustrate.Show();
            yield return distributeIllustrate.PlayEnterWait();
        }

        yield return distributeDialog.Play();

        if(distributeIllustrate) {
            yield return distributeIllustrate.PlayExitWait();
            distributeIllustrate.Hide();
        }

        if(areaIllustrate) {
            areaIllustrate.Show();
            yield return areaIllustrate.PlayEnterWait();
        }

        yield return areaDialog.Play();

        if(areaIllustrate) {            
            yield return areaIllustrate.PlayExitWait();
            areaIllustrate.Hide();
        }

        //tutorial

        //show board, spawn blobs
        if(boardHUDRootGO) boardHUDRootGO.SetActive(true);
        if(boardRootGO) boardRootGO.SetActive(true);

        if(boardAnimator && !string.IsNullOrEmpty(takeBoardBegin))
            yield return boardAnimator.PlayWait(takeBoardBegin);

        blobSpawner.Spawn(mBlobLargeTemplateInd, factor1);
        blobSpawner.Spawn(mBlobTemplateInd, factor2);

        while(blobSpawner.isSpawning || blobSpawner.CheckAnyBlobActiveState(Blob.State.Spawning))
            yield return null;

        //show drag instruction
        if(boardDragGuide)
            mDragGuideRout = StartCoroutine(DoDragGuide());

        yield return tutorialConnectDialog.Play();

        SetEquationUpdateActive(true);

        //wait for attack to finish
        mIsAttackComplete = false;
        while(!mIsAttackComplete)
            yield return null;

        SetEquationUpdateActive(false);

        //conclusion
        yield return tutorialEndDialog.Play();

        if(boardAnimator && !string.IsNullOrEmpty(takeBoardEnd))
            yield return boardAnimator.PlayWait(takeBoardEnd);

        GameData.instance.ProceedNext();
    }

    IEnumerator DoDragGuide() {
        var blob1 = blobSpawner.blobActives[0];
        var blob2 = blobSpawner.blobActives[1];

        while(!(blob1.isConnected && blob2.isConnected)) {
            var blobOp1Pos = blob1.jellySprite.CentralPoint.Body2D.position;
            var blobOp2Pos = blob2.jellySprite.CentralPoint.Body2D.position;

            var camCtrl = CameraController.main;
            var cam = camCtrl.cameraTarget;

            Vector2 blobOp1UIPos = cam.WorldToScreenPoint(blobOp1Pos);
            Vector2 blobOp2UIPos = cam.WorldToScreenPoint(blobOp2Pos);

            if(boardDragGuide.isActive)
                boardDragGuide.UpdatePositions(blobOp1UIPos, blobOp2UIPos);
            else
                boardDragGuide.Show(false, blobOp1UIPos, blobOp2UIPos);

            yield return null;
        }

        boardDragGuide.Hide();

        mDragGuideRout = null;
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

        string attackModal = GameData.instance.modalAttackDistributive;

        mModalAttackParms.SetAreaOperation(mAreaOp);
        mModalAttackParms.SetMistakeInfo(mMistakeCurrent);

        M8.ModalManager.main.Open(attackModal, mModalAttackParms);

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

                if(boardDragGuide)
                    mDragGuideRout = StartCoroutine(DoDragGuide());
                break;

            case AttackState.Fail:
                connectControl.GroupEvaluate(grp);

                if(boardDragGuide)
                    mDragGuideRout = StartCoroutine(DoDragGuide());
                break;

            case AttackState.Success:
                var camCtrl = CameraController.main;
                if(camCtrl)
                    camCtrl.raycastTarget = false; //disable blob input

                //do fancy animation on board
                if(boardAnimator && !string.IsNullOrEmpty(takeBoardCorrect))
                    boardAnimator.Play(takeBoardCorrect);

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
                
                if(camCtrl)
                    camCtrl.raycastTarget = true;

                mIsAttackComplete = true;
                break;
        }

        mAttackRout = null;
    }

    IEnumerator DoEquationUpdate() {
        var wait = new WaitForSeconds(0.1f);

        //TODO: assumes simple equation: num op num = answer
        while(true) {
            bool isOpActive = false;
            bool isEqActive = false;

            //grab blob that is dragging, and one that is highlighted
            Blob blobDragging = connectControl.curBlobDragging;
            Blob blobHighlight = null;

            var blobs = blobSpawner.blobActives;
            for(int i = 0; i < blobs.Count; i++) {
                var blob = blobs[i];
                if(blob.isHighlighted) {
                    if(blob != blobDragging)
                        blobHighlight = blob;
                }
            }

            //check for group
            var grp = connectControl.activeGroup;
            if(grp == null) {
                if(connectControl.curGroupDragging != null)
                    grp = connectControl.curGroupDragging;
            }

            if(grp != null) {
                if(equationOp1Anim) equationOp1Anim.Stop();
                if(equationOp2Anim) equationOp2Anim.Stop();

                isOpActive = true;

                string op1Text, op2Text;

                if(grp.blobOpLeft && grp.blobOpRight) {
                    if(grp.blobOpRight.number > grp.blobOpLeft.number) {
                        op1Text = grp.blobOpRight.number.ToString();
                        op2Text = grp.blobOpLeft.number.ToString();
                    }
                    else {
                        op1Text = grp.blobOpLeft.number.ToString();
                        op2Text = grp.blobOpRight.number.ToString();
                    }
                }
                else if(grp.blobOpLeft) {
                    op1Text = grp.blobOpLeft.number.ToString();
                    op2Text = "";
                }
                else if(grp.blobOpRight) {
                    op1Text = grp.blobOpRight.number.ToString();
                    op2Text = "";
                }
                else {
                    op1Text = "";
                    op2Text = "";
                }

                //check answer blob
                if(grp.blobEq) {
                    isEqActive = true;

                    if(equationAnsAnim) equationAnsAnim.Stop();

                    if(equationAnsText) equationAnsText.text = grp.blobEq.number.ToString();
                }
                else {
                    if(equationAnsAnim) equationAnsAnim.Play(0);

                    //update answer text
                    if(equationAnsText) equationAnsText.text = "";

                    isEqActive = grp.blobOpLeft && grp.blobOpRight;
                }

                if(equationOp1Text) equationOp1Text.text = op1Text;
                if(equationOp2Text) equationOp2Text.text = op2Text;
            }
            else {
                if(equationOp1Anim) equationOp1Anim.Play(0);
                if(equationOp2Anim) equationOp2Anim.Play(0);
                if(equationAnsAnim) equationAnsAnim.Stop();

                if(blobDragging) {
                    if(blobHighlight) {
                        if(blobHighlight.number > blobDragging.number) {
                            if(equationOp1Text) equationOp1Text.text = blobHighlight.number.ToString();
                            if(equationOp2Text) equationOp2Text.text = blobDragging.number.ToString();
                        }
                        else {
                            if(equationOp1Text) equationOp1Text.text = blobDragging.number.ToString();
                            if(equationOp2Text) equationOp2Text.text = blobHighlight.number.ToString();
                        }
                    }
                    else {
                        if(equationOp1Text) equationOp1Text.text = blobDragging.number.ToString();
                        if(equationOp2Text) equationOp2Text.text = "";
                    }

                    isOpActive = true;
                }
                else if(blobHighlight) {
                    if(equationOp1Text) equationOp1Text.text = blobHighlight.number.ToString();
                    if(equationOp2Text) equationOp2Text.text = "";
                }
                else {
                    if(equationOp1Text) equationOp1Text.text = "";
                    if(equationOp2Text) equationOp2Text.text = "";
                }

                if(equationAnsText) equationAnsText.text = "";
            }

            if(equationOpGO) equationOpGO.SetActive(isOpActive);
            if(equationEqGO) equationEqGO.SetActive(isEqActive);

            yield return wait;
        }
    }

    IEnumerator DoAttackDistributiveTutorial() {
        do {
            yield return null;
        } while(M8.ModalManager.main.isBusy);

        var modalAttackDist = M8.ModalManager.main.GetBehaviour<ModalAttackDistributive>(GameData.instance.modalAttackDistributive);
        var digitGrpWidget = modalAttackDist.digitGroupTop;

        //grab interact digit
        DigitWidget digitInteractWidget = null;

        for(int i = 0; i < digitGrpWidget.digitCount; i++) {
            var digitWidget = digitGrpWidget.GetDigitWidget(i);
            if(digitWidget && digitWidget.interactable) {
                digitInteractWidget = digitWidget;
                break;
            }
        }

        Transform clickLastParent = null;

        //show click
        if(digitInteractWidget) {
            if(clickInstructWidgetRoot) {
                clickLastParent = clickInstructWidgetRoot.parent;

                clickInstructWidgetRoot.SetParent(DragHolder.instance.dragRoot, false);

                var pos = digitInteractWidget.rectTransform.position;
                pos.x += clickInstructOfs.x;
                pos.y += clickInstructOfs.y;

                clickInstructWidgetRoot.position = pos;

                clickInstructWidgetRoot.gameObject.SetActive(true);
            }
        }

        yield return tutorialAttackDistributiveDialog.Play();

        if(digitInteractWidget) {
            //wait for it to be distributed
            while(digitInteractWidget.interactable)
                yield return null;

            if(clickInstructWidgetRoot) {
                clickInstructWidgetRoot.SetParent(clickLastParent);
                clickInstructWidgetRoot.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator DoDialog(ModalDialogFlow dlg) {
        do {
            yield return null;
        } while(M8.ModalManager.main.isBusy);

        yield return dlg.Play();
    }

    void OnSignalBlobDragBegin(Blob blob) {
        var blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];
            if(blobActive == blob)
                continue;

            blobActive.highlightLock = true;
        }
    }

    void OnSignalBlobDragEnd(Blob blob) {
        var blobActives = blobSpawner.blobActives;
        for(int i = 0; i < blobActives.Count; i++) {
            var blobActive = blobActives[i];
            if(blobActive == blob)
                continue;

            blobActive.highlightLock = false;
        }
    }
    
    void OnSignalAttackDistributiveActive(bool aActive) {
        if(!mIsAttackDistributiveExplained && aActive) {
            StartCoroutine(DoAttackDistributiveTutorial());
            mIsAttackDistributiveExplained = true;
        }
    }

    void OnSignalAttackEvaluateActive(bool aActive) {
        if(!mIsAttackEvaluateExplained && aActive) {
            StartCoroutine(DoDialog(tutorialAttackEvalDialog));
            mIsAttackEvaluateExplained = true;
        }
    }

    void OnSignalAttackSumsActive(bool aActive) {
        if(!mIsAttackSumsExplained && aActive) {
            StartCoroutine(DoDialog(tutorialAttackSumsDialog));
            mIsAttackSumsExplained = true;
        }
    }

    void OnSignalAttackStateChanged(AttackState toState) {
        mCurAttackState = toState;
    }

    void OnGroupAdded(BlobConnectController.Group grp) {
        if(grp.isOpFilled) {
            //Debug.Log("Launch attack: " + grp.blobOpLeft.number + " x " + grp.blobOpRight.number);
            if(mAttackRout == null)
                mAttackRout = StartCoroutine(DoAttack(grp));
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

        connectControl.ClearGroup(grp);
    }

    private void SetEquationUpdateActive(bool isActive) {
        if(isActive) {
            if(mEquationRout != null) StopCoroutine(mEquationRout);
            mEquationRout = StartCoroutine(DoEquationUpdate());
        }
        else {
            if(mEquationRout != null) {
                StopCoroutine(mEquationRout);
                mEquationRout = null;
            }

            if(equationOp1Anim) equationOp1Anim.Stop();
            if(equationOp2Anim) equationOp2Anim.Stop();
            if(equationAnsAnim) equationAnsAnim.Stop();

            if(equationOp1Text) equationOp1Text.text = "";
            if(equationOp2Text) equationOp2Text.text = "";
            if(equationAnsText) equationAnsText.text = "";

            if(equationOpGO) equationOpGO.SetActive(false);
            if(equationEqGO) equationEqGO.SetActive(false);
        }
    }
}
