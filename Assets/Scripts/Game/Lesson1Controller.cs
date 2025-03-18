using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

using TMPro;

public class Lesson1Controller : LessonBoardController {
    [Header("Lesson Flow")]
    public AnimatorEnterExit blobNumberIllustrate;
    public ModalDialogFlow introDialog;
    public AnimatorEnterExit distributeIllustrate;
    public ModalDialogFlow distributeDialog;
    public AnimatorEnterExit areaIllustrate;
    public ModalDialogFlow areaDialog;
    public GameObject areaSubGO;
    public ModalDialogFlow areaSubDialog;
    public GameObject areaSumsGO;
    public ModalDialogFlow areaSumsDialog;
    public ModalDialogFlow tutorialConnectDialog;
    public ModalDialogFlow tutorialAttackDistributiveDialog;
    public ModalDialogFlow tutorialAttackEvalDialog;
    public ModalDialogFlow tutorialAttackEvalLastDigitDialog;
    public ModalDialogFlow tutorialAttackSumsDialog;
    public ModalDialogFlow tutorialEndDialog;

    private bool mIsAttackDistributiveExplained;
    private bool mIsAttackEvaluateExplained;
    private bool mIsAttackSumsExplained;

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

		BlobConnectController.checkBlobConnectCriteriaDisabled = false;

		if(blobNumberIllustrate) blobNumberIllustrate.Hide();
        if(distributeIllustrate) distributeIllustrate.Hide();
        if(areaIllustrate) areaIllustrate.Hide();
        if(areaSubGO) areaSubGO.SetActive(false);
        if(areaSumsGO) areaSumsGO.SetActive(false);
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

        if(areaSubGO) areaSubGO.SetActive(true);

        yield return areaSubDialog.Play();

        if(areaSumsGO) areaSumsGO.SetActive(true);

        yield return areaSumsDialog.Play();

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

    IEnumerator DoAttackEvalTutorial() {
        do {
            yield return null;
        } while(M8.ModalManager.main.isBusy);

        yield return tutorialAttackEvalDialog.Play();

        //wait for double digit eval.
        var areaEvalModal = M8.ModalManager.main.GetBehaviour<ModalAttackAreaEvaluate>(GameData.instance.modalAttackAreaEvaluate);
        if(areaEvalModal) {
            while(areaEvalModal.areaOpCellWidgetSelected == null || areaEvalModal.areaOpCellWidgetSelected.cellData.op.operand1 < 10)
                yield return null;
        }

        yield return tutorialAttackEvalLastDigitDialog.Play();
    }
    
    protected override void OnSignalAttackDistributiveActive(bool aActive) {
        if(!mIsAttackDistributiveExplained && aActive) {
            StartCoroutine(DoAttackDistributiveTutorial());
            mIsAttackDistributiveExplained = true;
        }
    }

    protected override void OnSignalAttackEvaluateActive(bool aActive) {
        if(!mIsAttackEvaluateExplained && aActive) {
            StartCoroutine(DoAttackEvalTutorial());
            mIsAttackEvaluateExplained = true;
        }
    }

    protected override void OnSignalAttackSumsActive(bool aActive) {
        if(!mIsAttackSumsExplained && aActive) {
            StartCoroutine(DoDialog(tutorialAttackSumsDialog));
            mIsAttackSumsExplained = true;
        }
    }
}
