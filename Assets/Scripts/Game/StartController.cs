using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using LoLExt;

public class StartController : GameModeController<StartController> {
    [Header("Screen")]
    public GameObject loadingGO;
    public GameObject readyGO;

    [Header("Title")]
    [M8.Localize]
    public string titleRef;
    public TMP_Text titleText;

    [Header("Play")]
    public Button newButton;
    public Button continueButton;

    [Header("Music")]
    [M8.MusicPlaylist]
    public string music;

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(loadingGO) loadingGO.SetActive(true);
        if(readyGO) readyGO.SetActive(false);

        //Setup Play
        if(newButton) newButton.onClick.AddListener(OnPlayNew);

        if(continueButton) {
            continueButton.onClick.AddListener(OnPlayContinue);
            continueButton.gameObject.SetActive(LoLManager.instance.curProgress > 0);
        }
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        while(!LoLManager.instance.isReady)
            yield return null;

        if(loadingGO) loadingGO.SetActive(false);

        //Title/Ready

        if(!string.IsNullOrEmpty(music))
            M8.MusicPlaylist.instance.Play(music, true, true);

        //Setup Title
        if(titleText) titleText.text = M8.Localize.Get(titleRef);

        if(readyGO) readyGO.SetActive(true);

        //enter animation
    }

    void OnPlayNew() {
        if(LoLManager.instance.curProgress > 0)
            GameData.instance.ResetProgress();

        //play intro

        GameData.instance.ProceedFromCurrentProgress();
    }

    void OnPlayContinue() {
        if(LoLManager.instance.curProgress <= 0) {
            //play intro
        }
        else
            GameData.instance.ProceedFromCurrentProgress();
    }
}