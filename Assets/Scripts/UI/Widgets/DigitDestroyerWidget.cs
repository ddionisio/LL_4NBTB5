using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;
using TMPro;

public class DigitDestroyerWidget : MonoBehaviour {
    [Header("Display")]
    public GameObject overlayRootGO;    
    public TMP_Text countLabel;

    [Header("Interaction")]
    public GameObject interactRootGO;
    public Selectable interactWidget;

    [Header("FTUE")]
    public GameObject FTUERootGO;
    public ModalDialogFlow FTUEDialog;

    [Header("Animation")]
    public M8.Animator.Animate animatorInteract;
    [M8.Animator.TakeSelector(animatorField = "animatorInteract")]
    public string takeInteractEnter;
    [M8.Animator.TakeSelector(animatorField = "animatorInteract")]
    public string takeInteractExit;

    public M8.Animator.Animate animatorOverlay;
    [M8.Animator.TakeSelector(animatorField = "animatorOverlay")]
    public string takeOverlayEnter;
    [M8.Animator.TakeSelector(animatorField = "animatorOverlay")]
    public string takeOverlayExit;

    [Header("Signal Listen")]
    public M8.SignalBoolean signalListenPlayActionActive;

    public bool interactable {
        get { return interactWidget ? interactWidget.interactable : false; }
        set {
            if(interactWidget)
                interactWidget.interactable = value;
        }
    }

    public bool isShown {
        get { return mIsShown; }
        set {
            if(mIsShown != value) {
                mIsShown = value;

                if(mIsShown) {
                    if(interactRootGO) interactRootGO.SetActive(true);

                    if(animatorInteract && !string.IsNullOrEmpty(takeInteractEnter))
                        animatorInteract.Play(takeInteractEnter);
                }
                else {
                    if(animatorInteract && !string.IsNullOrEmpty(takeInteractExit))
                        animatorInteract.Play(takeInteractExit);
                }
            }
        }
    }

    public bool isOverlayShown {
        get { return mIsOverlayShown; }
        set {
            if(mIsOverlayShown != value) {
                mIsOverlayShown = value;

                if(mIsOverlayShown) {
                    if(overlayRootGO) overlayRootGO.SetActive(true);

                    if(animatorOverlay && !string.IsNullOrEmpty(takeOverlayEnter))
                        animatorOverlay.Play(takeOverlayEnter);
                }
                else {
                    if(animatorOverlay && !string.IsNullOrEmpty(takeOverlayExit))
                        animatorOverlay.Play(takeOverlayExit);
                }
            }
        }
    }

    public int count {
        get { return mCount; }
        set {
            if(mCount != value) {
                mCount = value;
                if(countLabel) countLabel.text = mCount.ToString();
            }
        }
    }

    private int mErrorCount;
    private int mCount;
    private bool mIsAction;

    private bool mIsShown;
    private bool mIsOverlayShown;

    public void Click() {
        if(count > 0) {
            isOverlayShown = true;

            PlayController.instance.StartDigitDestroyer();

            count--;
            if(count == 0) {
                mErrorCount = 0; //appear again later
                isShown = false;
            }
        }
    }

    void OnDisable() {
        if(signalListenPlayActionActive) signalListenPlayActionActive.callback -= OnSignalPlayActionActive;

        if(animatorInteract) animatorInteract.takeCompleteCallback -= OnTakeInteractComplete;
        if(animatorOverlay) animatorOverlay.takeCompleteCallback -= OnTakeOverlayComplete;

        if(PlayController.isInstantiated) PlayController.instance.groupEvalCallback -= OnGroupEvaluated;
    }

    void OnEnable() {
        if(signalListenPlayActionActive) signalListenPlayActionActive.callback += OnSignalPlayActionActive;

        if(animatorInteract) animatorInteract.takeCompleteCallback += OnTakeInteractComplete;
        if(animatorOverlay) animatorOverlay.takeCompleteCallback += OnTakeOverlayComplete;

        PlayController.instance.groupEvalCallback += OnGroupEvaluated;

        mErrorCount = 0;
        mIsShown = false;
        mIsOverlayShown = false;
        interactable = false;

        if(FTUERootGO) FTUERootGO.SetActive(false);
        if(overlayRootGO) overlayRootGO.SetActive(false);
        if(interactRootGO) interactRootGO.SetActive(false);
    }

    void Update() {
        if(isShown && !interactable && !mIsAction) {
            //check if there are any blobs with more than one non-zero digits
            var blobs = PlayController.instance.blobSpawner.blobActives;
            for(int i = 0; i < blobs.Count; i++) {
                var blob = blobs[i];
                if(blob.state != Blob.State.Normal)
                    continue;

                if(WholeNumber.NonZeroCount(blob.number) > 1) {
                    interactable = true;
                    break;
                }
            }
        }
    }

    void OnSignalPlayActionActive(bool isAction) {
        mIsAction = isAction;

        if(mIsAction) {
            interactable = false;
        }
        else {
            isOverlayShown = false;
        }
    }

    void OnGroupEvaluated(Operation op, int answer, bool isCorrect) {
        //show?
        if(!isCorrect) {
            if(!isShown) {
                mErrorCount++;
                if(mErrorCount >= GameData.instance.digitDestroyShowAfterIncorrect) {
                    count = GameData.instance.digitDestroyCount;

                    isShown = true;

                    //display dialog if first time
                    var isDialogDone = LoLManager.instance.userData.GetInt(GameData.userDataKeyFTUEDigitDestroyer, 0) > 0;
                    if(!isDialogDone) {
                        LoLManager.instance.userData.SetInt(GameData.userDataKeyFTUEDigitDestroyer, 1);
                        StartCoroutine(DoFTUE());
                    }
                }
            }
        }
    }

    void OnTakeInteractComplete(M8.Animator.Animate anim, M8.Animator.Take take) {
        if(take != null) {
            if(take.name == takeInteractExit) {
                if(interactRootGO)
                    interactRootGO.SetActive(false);
            }
        }
    }

    void OnTakeOverlayComplete(M8.Animator.Animate anim, M8.Animator.Take take) {
        if(take != null) {
            if(take.name == takeOverlayExit) {
                if(overlayRootGO)
                    overlayRootGO.SetActive(false);
            }
        }
    }

    IEnumerator DoFTUE() {
        if(FTUERootGO) FTUERootGO.SetActive(true);

        yield return FTUEDialog.Play();

        if(FTUERootGO) FTUERootGO.SetActive(false);
    }
}
