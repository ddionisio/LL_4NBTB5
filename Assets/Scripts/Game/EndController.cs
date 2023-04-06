using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

public class EndController : GameModeController<EndController> {
    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeAttackBlobEnter;
    [M8.Animator.TakeSelector]
    public string takeDaylight;
    [M8.Animator.TakeSelector]
    public string takeEnd;

    [Header("Flow")]
    public AnimatorEnterExit blobConsumeAnimate;
    public float completeDelay = 2f;

    [Header("Music")]
    [M8.MusicPlaylist]
    public string music;

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(blobConsumeAnimate) blobConsumeAnimate.gameObject.SetActive(false);
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        while(!LoLManager.instance.isReady)
            yield return null;

        if(!string.IsNullOrEmpty(music))
            M8.MusicPlaylist.instance.Play(music, false, false);

        //float lastTime = Time.time;

        if(animator && !string.IsNullOrEmpty(takeAttackBlobEnter))
            yield return animator.PlayWait(takeAttackBlobEnter);

        if(blobConsumeAnimate) {
            blobConsumeAnimate.gameObject.SetActive(true);

            yield return blobConsumeAnimate.PlayEnterWait();

            blobConsumeAnimate.gameObject.SetActive(false);
        }

        if(animator && !string.IsNullOrEmpty(takeDaylight))
            yield return animator.PlayWait(takeDaylight);

        if(animator && !string.IsNullOrEmpty(takeEnd))
            yield return animator.PlayWait(takeEnd);

        yield return new WaitForSeconds(completeDelay);

        LoLManager.instance.Complete();

        //Debug.Log("Time: " + (Time.time - lastTime));
    }
}