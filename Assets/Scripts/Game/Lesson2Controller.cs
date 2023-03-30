using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class Lesson2Controller : LessonBoardController {
    [Header("Lesson Flow")]
    public AnimatorEnterExit blobNumberIllustrate;
    public ModalDialogFlow introDialog;
    public AnimatorEnterExit doubleDigitMultIllustrate;
    public ModalDialogFlow playDialog;
    public ModalDialogFlow doubleDigitMultDialog;
    public ModalDialogFlow doubleDigitDistDialog;
    public ModalDialogFlow doubleDigitEvalDialog;
    public ModalDialogFlow endDialog;

    private bool mIsAttackDistributiveExplained;
    private bool mIsAttackEvaluateExplained;

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(blobNumberIllustrate) blobNumberIllustrate.Hide();
        if(doubleDigitMultIllustrate) doubleDigitMultIllustrate.Hide();
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

        if(doubleDigitMultIllustrate) {
            doubleDigitMultIllustrate.Show();
            yield return doubleDigitMultIllustrate.PlayEnterWait();
        }

        yield return doubleDigitMultDialog.Play();

        if(doubleDigitMultIllustrate) {
            yield return doubleDigitMultIllustrate.PlayExitWait();
            doubleDigitMultIllustrate.Hide();
        }

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

        SetEquationUpdateActive(true);

        yield return playDialog.Play();

        //wait for attack to finish
        mIsAttackComplete = false;
        while(!mIsAttackComplete)
            yield return null;

        SetEquationUpdateActive(false);

        //conclusion
        yield return endDialog.Play();

        if(boardAnimator && !string.IsNullOrEmpty(takeBoardEnd))
            yield return boardAnimator.PlayWait(takeBoardEnd);

        GameData.instance.ProceedNext();
    }

    IEnumerator DoAttackEval() {
        //wait for double digit eval.
        var areaEvalModal = M8.ModalManager.main.GetBehaviour<ModalAttackAreaEvaluate>(GameData.instance.modalAttackAreaEvaluate);
        if(areaEvalModal) {
            while(areaEvalModal.areaOpCellWidgetSelected == null || areaEvalModal.areaOpCellWidgetSelected.cellData.op.operand1 < 10 || areaEvalModal.areaOpCellWidgetSelected.cellData.op.operand2 < 10)
                yield return null;
        }

        yield return doubleDigitEvalDialog.Play();
    }

    protected override void OnSignalAttackDistributiveActive(bool aActive) {
        if(!mIsAttackDistributiveExplained && aActive) {
            StartCoroutine(DoDialog(doubleDigitDistDialog));
            mIsAttackDistributiveExplained = true;
        }
    }

    protected override void OnSignalAttackEvaluateActive(bool aActive) {
        if(!mIsAttackEvaluateExplained && aActive) {
            StartCoroutine(DoAttackEval());
            mIsAttackEvaluateExplained = true;
        }
    }
}