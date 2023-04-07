using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;
using TMPro;

public class ProgressController : GameModeController<ProgressController> {
    [System.Serializable]
    public class LevelData {
        public Transform root;
        public Transform blobsRoot;
        public AnimatorEnterExit shapesAnim;
        public GameObject constellationGO;

        public AnimatorEnterExit[] blobAnims { get; private set; }

        public Vector3 position { get { return root ? root.position : Vector3.zero; } }

        public bool active {
            get { return root ? root.gameObject.activeSelf : false; }
            set {
                if(root)
                    root.gameObject.SetActive(value);
            }
        }

        public bool constellationActive {
            get { return constellationGO ? constellationGO.activeSelf : false; }
            set {
                if(constellationGO)
                    constellationGO.SetActive(value);
            }
        }

        public void Init() {
            if(blobsRoot) {
                blobAnims = new AnimatorEnterExit[blobsRoot.childCount];
                for(int i = 0; i < blobsRoot.childCount; i++) {
                    var child = blobsRoot.GetChild(i);
                    blobAnims[i] = child.GetComponent<AnimatorEnterExit>();
                }

                blobsRoot.gameObject.SetActive(false);
            }
            else
                blobAnims = new AnimatorEnterExit[0];

            if(shapesAnim)
                shapesAnim.gameObject.SetActive(false);

            constellationActive = false;

            active = false;
        }

        /// <summary>
        /// Display as neutral
        /// </summary>
        public void ShowBlobsActive() {
            if(blobsRoot)
                blobsRoot.gameObject.SetActive(true);

            if(shapesAnim)
                shapesAnim.gameObject.SetActive(true);

            constellationActive = false;

            active = true;
        }

        /// <summary>
        /// Clear out blobs and shapes, show constellation
        /// </summary>
        public IEnumerator BlobsClear(string sfxBlobClear, string sfxConstellation, float blobClearDelay, float shapesFadeStartDelay, float constellationShowDelay) {
            var blobAnimNextWait = new WaitForSeconds(blobClearDelay);

            for(int i = 0; i < blobAnims.Length; i++) {
                var blobAnim = blobAnims[i];
                if(blobAnim) {
                    blobAnim.PlayExit();

                    if(!string.IsNullOrEmpty(sfxBlobClear))
                        M8.SoundPlaylist.instance.Play(sfxBlobClear, false);
                }

                yield return blobAnimNextWait;
            }

            yield return new WaitForSeconds(shapesFadeStartDelay);

            if(shapesAnim) {
                yield return shapesAnim.PlayExitWait();

                shapesAnim.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(constellationShowDelay);

            if(!string.IsNullOrEmpty(sfxConstellation))
                M8.SoundPlaylist.instance.Play(sfxConstellation, false);

            constellationActive = true;
        }
    }

    public M8.ColorPaletteCopy paletteOverride;

    [Header("Camera")]
    public Transform cameraRoot;
    public float cameraMoveDelay = 2f;
    public DG.Tweening.Ease cameraMoveEase = DG.Tweening.Ease.InOutSine;

    [Header("Level Label Display")]
    public GameObject levelLabelDisplayGO;
    public TMP_Text levelLabel;

    [Header("Levels")]
    public LevelData[] levels;

    [Header("Flow Control")]
    public float blobClearStartDelay = 1f;
    public float blobClearDelay = 0.3f;
    public float shapesFadeStartDelay = 0.5f;
    public float constellationShowDelay = 0.3f;
    public float levelNextStartDelay = 3f;
    public float showLevelLabelStartDelay = 1f;
    public float proceedDelay = 2.5f;

    [Header("Music")]
    [M8.MusicPlaylist]
    public string music;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxBlobClear;
    [M8.SoundPlaylist]
    public string sfxConstellation;
    [M8.SoundPlaylist]
    public string sfxLevelLabel;

    private int mLevelIndex;
    private bool mIsInit;

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        for(int i = 0; i < levels.Length; i++)
            levels[i].Init();

        //setup stuff
        if(LoLManager.isInstantiated && LoLManager.instance.isReady)
            Init();

        if(levelLabelDisplayGO)
            levelLabelDisplayGO.SetActive(false);
    }

    protected override IEnumerator Start() {
        while(!LoLManager.instance.isReady)
            yield return null;

        //setup stuff
        if(!mIsInit)
            Init();

        yield return base.Start();

        if(!string.IsNullOrEmpty(music))
            M8.MusicPlaylist.instance.Play(music, false, false);

        bool showLevelLabel = true;

        //determine if we want to end previous level
        if(mLevelIndex > 0) {
            int prevLevelIndex = mLevelIndex - 1;

            yield return new WaitForSeconds(blobClearStartDelay);

            yield return levels[prevLevelIndex].BlobsClear(sfxBlobClear, sfxConstellation, blobClearDelay, shapesFadeStartDelay, constellationShowDelay);

            //check if we are at last level, otherwise, we just proceed
            if(mLevelIndex < levels.Length) {
                yield return new WaitForSeconds(levelNextStartDelay);

                var newLevel = levels[mLevelIndex];

                //move camera
                if(cameraRoot) {
                    var startCamPos = cameraRoot.position;
                    var endCamPos = newLevel.position;

                    var cameraEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(cameraMoveEase);

                    var curTime = 0f;
                    while(curTime < cameraMoveDelay) {
                        yield return null;

                        curTime += Time.deltaTime;

                        var t = cameraEaseFunc(curTime, cameraMoveDelay, 0f, 0f);

                        cameraRoot.position = Vector3.Lerp(startCamPos, endCamPos, t);
                    }
                }
            }
            else
                showLevelLabel = false;
        }

        if(showLevelLabel) {
            yield return new WaitForSeconds(showLevelLabelStartDelay);

            if(!string.IsNullOrEmpty(sfxLevelLabel))
                M8.SoundPlaylist.instance.Play(sfxLevelLabel, false);

            //just show level label and proceed
            if(levelLabelDisplayGO)
                levelLabelDisplayGO.SetActive(true);
        }

        //proceed
        yield return new WaitForSeconds(proceedDelay);

        GameData.instance.ProceedNext();
    }

    private void Init() {
        mIsInit = true;

        int gameLevelIndex;

        if(GameData.instance.isProceed)
            gameLevelIndex = LoLManager.instance.curProgress;
        else
            gameLevelIndex = 0;

        var levelDat = GameData.instance.levels[gameLevelIndex];

        mLevelIndex = levelDat.index;

        if(paletteOverride && levelDat.palette) {
            paletteOverride.source = levelDat.palette;
            paletteOverride.Apply();
        }

        if(levelLabel && !string.IsNullOrEmpty(levelDat.titleRef))
            levelLabel.text = M8.Localize.Get(levelDat.titleRef);

        var showIndex = mLevelIndex > 0 ? mLevelIndex - 1 : mLevelIndex;

        if(cameraRoot)
            cameraRoot.position = levels[showIndex].position;

        levels[showIndex].ShowBlobsActive();

        if(mLevelIndex < levels.Length)
            levels[mLevelIndex].ShowBlobsActive();
    }
}
