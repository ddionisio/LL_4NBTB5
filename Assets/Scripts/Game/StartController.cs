using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

public class StartController : GameModeController<StartController> {
    [Header("Screen")]
    public GameObject loadingGO;
    public GameObject readyGO;

    [Header("Play")]
    public AnimatorEnterExit readyAnim;
    public Button newButton;
    public Button continueButton;

    [Header("Intro")]
    public AnimatorEnterExit introScan;
    public GameObject introScanIdleGO;    
    public GameObject introScanDangerGO;
    public GameObject introAttackIllustrateGO;

    public M8.Animator.Animate introAnimator;
    [M8.Animator.TakeSelector(animatorField = "introAnimator")]
    public string introTakeLookUp;
    [M8.Animator.TakeSelector(animatorField = "introAnimator")]
    public string introTakeBlobShades;
    [M8.Animator.TakeSelector(animatorField = "introAnimator")]
    public string introTakePortalOpen;

    public float introIdleDelay = 0.5f;
    public float introBlobShadesDelay = 0.5f;
    public float introPortalOpenStartDelay = 0.5f;
    public float introPortalDelay = 1f;

    public ModalDialogFlow introDialog;
    public ModalDialogFlow introAttackDialog;
        
    [Header("Music")]
    [M8.MusicPlaylist]
    public string music;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxScanEnter;
    [M8.SoundPlaylist]
    public string sfxScanDanger;
    [M8.SoundPlaylist]
    public string sfxAnomalies;
    [M8.SoundPlaylist]
    public string sfxPortalOpen;

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(loadingGO) loadingGO.SetActive(true);
        if(readyGO) readyGO.SetActive(false);

        //Setup Play
        if(newButton) newButton.onClick.AddListener(OnPlayNew);
        if(continueButton) continueButton.onClick.AddListener(OnPlayContinue);

        //Setup Intro
        if(introScan) introScan.gameObject.SetActive(false);
        if(introScanIdleGO) introScanIdleGO.SetActive(false);
        if(introScanDangerGO) introScanDangerGO.SetActive(false);
        if(introAttackIllustrateGO) introAttackIllustrateGO.SetActive(false);
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        while(!LoLManager.instance.isReady)
            yield return null;

        //setup continue
        if(continueButton)
            continueButton.gameObject.SetActive(LoLManager.instance.curProgress > 0);

        //wait a bit
        yield return new WaitForSeconds(0.3f);

        if(loadingGO) loadingGO.SetActive(false);

        if(!string.IsNullOrEmpty(music))
            M8.MusicPlaylist.instance.Play(music, true, true);

        if(readyGO) readyGO.SetActive(true);

        //enter animation
        if(readyAnim)
            yield return readyAnim.PlayEnterWait();
    }

    IEnumerator DoIntro() {
        if(readyAnim)
            yield return readyAnim.PlayExitWait();

        //show scanner
        if(!string.IsNullOrEmpty(sfxScanEnter))
            M8.SoundPlaylist.instance.Play(sfxScanEnter, false);

        if(introScanIdleGO) introScanIdleGO.SetActive(true);

        if(introScan) {
            introScan.gameObject.SetActive(true);
            introScan.PlayEnter();
        }

        //look up
        if(introAnimator && !string.IsNullOrEmpty(introTakeLookUp))
            yield return introAnimator.PlayWait(introTakeLookUp);

        //wait a bit
        yield return new WaitForSeconds(introIdleDelay);

        //show shadow blobs
        if(!string.IsNullOrEmpty(sfxAnomalies))
            M8.SoundPlaylist.instance.Play(sfxAnomalies, false);

        if(introAnimator && !string.IsNullOrEmpty(introTakeBlobShades))
            yield return introAnimator.PlayWait(introTakeBlobShades);

        yield return new WaitForSeconds(introBlobShadesDelay);

        //danger!
        if(!string.IsNullOrEmpty(sfxScanDanger))
            M8.SoundPlaylist.instance.Play(sfxScanDanger, false);

        if(introScanIdleGO) introScanIdleGO.SetActive(false);
        if(introScanDangerGO) introScanDangerGO.SetActive(true);

        yield return new WaitForSeconds(introPortalOpenStartDelay);

        //open portal
        if(!string.IsNullOrEmpty(sfxPortalOpen))
            M8.SoundPlaylist.instance.Play(sfxPortalOpen, false);

        if(introAnimator && !string.IsNullOrEmpty(introTakePortalOpen))
            yield return introAnimator.PlayWait(introTakePortalOpen);

        yield return new WaitForSeconds(introPortalDelay);

        if(introScan) {
            yield return introScan.PlayExitWait();
            introScan.gameObject.SetActive(false);
        }

        //dialog
        yield return introDialog.Play();

        //attack blob
        if(introAttackIllustrateGO) introAttackIllustrateGO.SetActive(true);

        yield return introAttackDialog.Play();

        GameData.instance.ProceedFromCurrentProgress();
    }

    IEnumerator DoProceed() {
        if(readyAnim)
            yield return readyAnim.PlayExitWait();

        GameData.instance.ProceedFromCurrentProgress();
    }

    void OnPlayNew() {
        if(LoLManager.instance.curProgress > 0)
            GameData.instance.ResetProgress();

        //play intro
        StartCoroutine(DoIntro());
    }

    void OnPlayContinue() {
        if(LoLManager.instance.curProgress <= 0)
            StartCoroutine(DoIntro());
        else
            StartCoroutine(DoProceed());
    }
}