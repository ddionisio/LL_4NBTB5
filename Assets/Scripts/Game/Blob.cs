using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Blob number
/// </summary>
public class Blob : MonoBehaviour, M8.IPoolSpawn, M8.IPoolDespawn {
    public const string parmNumber = "number";

    public enum State {
        None,
        Normal,
        Spawning, //animate and set to normal
        Despawning, //animate and release
        Error, //error highlight for a bit
        Correct //animate and release
    }

    [Header("Jelly")]
    public UnityJellySprite jellySprite;
    public float radius; //estimate radius

    [Header("Face Display")]
    public SpriteRenderer[] eyeSpriteRenders;
    public SpriteRenderer mouthSpriteRender;

    [Header("Face States")]
    public Sprite eyeSpriteNormal;
    public Sprite eyeSpriteLarge;
    public Sprite eyeSpriteClose;

    public float eyeBlinkOpenDelayMin = 0.5f;
    public float eyeBlinkOpenDelayMax = 4f;
    public float eyeBlinkCloseDelay = 0.3f;

    public Sprite mouthSpriteNormal;
    public Sprite mouthSpriteDragging;
    public Sprite mouthSpriteConnected;
    public Sprite mouthSpriteError;
    public Sprite mouthSpriteCorrect;

    [Header("Highlight Materials")]
    public Material hoverDragMaterial;
    public Material errorMaterial;
    public Material correctMaterial;

    [Header("Error Settings")]
    public float errorDuration = 1f;

    [Header("Correct Settings")]
    public float correctStartDelay = 0.5f;

    [Header("Spawn Settings")]
    public M8.RangeFloat spawnCenterImpulse;
    public M8.RangeFloat spawnEdgeImpulse;

    [Header("Hint Settings")]
    public GameObject hintActiveGO;

    [Header("UI")]
    public GameObject highlightGO; //active during enter and dragging
    public Text numericText;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeSpawn;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeDespawn;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public string takeCorrect;

    [Header("Sfx")]
    [M8.SoundPlaylist]
    public string soundSpawn;

    [Header("Signal Listens")]
    public M8.Signal signalListenDespawn; //use to animate and then despawn

    [Header("Signal Invokes")]
    public SignalBlob signalInvokeDragBegin;
    public SignalBlob signalInvokeDragEnd;
    public SignalBlob signalInvokeDespawn;

    public int number {
        get { return mNumber; }
        set {
            if(mNumber != value) {
                mNumber = value;
                ApplyNumberDisplay();
            }
        }
    }

    public int dragRefPointIndex { get; private set; }
    public Vector2 dragPoint { get; private set; } //world
    public bool isDragging { get; private set; }
    public GameObject dragPointerGO { get; private set; } //current GameObject on pointer during drag
    public JellySpriteReferencePoint dragPointerJellySpriteRefPt { get; private set; } //current jelly sprite ref pt. on pointer during drag

    public M8.PoolDataController poolData {
        get {
            if(!mPoolDataCtrl)
                mPoolDataCtrl = GetComponent<M8.PoolDataController>();
            return mPoolDataCtrl;
        }
    }

    public bool inputLocked {
        get { return mInputLocked || mInputLockedInternal; }
        set {
            if(mInputLocked != value) {
                mInputLocked = value;
                ApplyInputLocked();
            }
        }
    }

    public State state {
        get { return mState; }
        set {
            if(mState != value) {
                mState = value;
                ApplyState();
            }
        }
    }

    public bool isConnected {
        get { return mIsConnected; }
        set {
            if(mIsConnected != value) {
                mIsConnected = value;

                RefreshMouthSprite();

                //highlight
            }
        }
    }

    public Color color {
        get { return jellySprite ? jellySprite.m_Color : Color.clear; }
        set {
            if(jellySprite)
                jellySprite.SetColor(value);
        }
    }

    public float colorAlpha {
        get { return jellySprite ? jellySprite.m_Color.a : 0f; }
        set {
            if(jellySprite) {
                var clr = jellySprite.m_Color;
                clr.a = value;
                jellySprite.SetColor(clr);
            }
        }
    }

    public bool hintActive {
        get { return hintActiveGO && hintActiveGO.activeSelf; }
        set {
            if(hintActiveGO)
                hintActiveGO.SetActive(value);
        }
    }

    public bool isHighlighted { get; private set; }

    private int mNumber;

    private M8.PoolDataController mPoolDataCtrl;

    private Camera mDragCamera;

    private Coroutine mRout;
    private Coroutine mEyeBlinkRout;

    private State mState = State.None;

    private RaycastHit2D[] mHitCache = new RaycastHit2D[16];

    private bool mInputLocked;
    private bool mInputLockedInternal;

    private bool mIsConnected;

    /// <summary>
    /// Get an approximate edge towards given point, relies on reference points to provide edge.
    /// </summary>
    public bool GetEdge(Vector2 toPos, out Vector2 refPtPos, out int refPtIndex) {
        Vector2 sPos = jellySprite.CentralPoint.Body2D.position;
        Vector2 dpos = sPos - toPos;
        float dist = dpos.magnitude;

        if(dist <= 0f) {
            refPtPos = sPos;
            refPtIndex = 0;
            return false;
        }

        Vector2 dir = dpos / dist;

        var centralPointParent = jellySprite.CentralPoint.GameObject.transform.parent;

        var hitCount = Physics2D.RaycastNonAlloc(toPos, dir, mHitCache, dist, (1<<gameObject.layer));
        if(hitCount == 0) {
            refPtPos = sPos;
            refPtIndex = 0;
            return false;
        }

        //Collider2D edgeColl = null;
        Vector2 edgePt = sPos;
        int edgeInd = 0;

        for(int i = 0; i < hitCount; i++) {
            var hit = mHitCache[i];
            var coll = hit.collider;

            if(!coll)
                continue;

            //only consider hits from own reference pts.
            if(coll.transform.parent != centralPointParent)
                continue;

            edgePt = hit.point;
            edgeInd = coll.transform.GetSiblingIndex();
            break;
        }

        refPtPos = edgePt;
        refPtIndex = edgeInd;
        return true;
    }

    public void ApplyJellySpriteMaterial(Material mat) {
        if(jellySprite.m_Material != mat) {
            jellySprite.m_Material = mat;
            jellySprite.ReInitMaterial();
        }
    }

    void OnApplicationFocus(bool isActive) {
        if(!isActive) {
            if(isDragging)
                DragInvalidate();
        }
    }

    /*void Awake() {
        //apply children to jelly attach
        Transform[] attaches = new Transform[transform.childCount];
        for(int i = 0; i < transform.childCount; i++)
            attaches[i] = transform.GetChild(i);

        if(jellySprite.m_AttachPoints == null)
            jellySprite.m_AttachPoints = attaches;
        else {
            int prevAttachPointLen = jellySprite.m_AttachPoints.Length;
            System.Array.Resize(ref jellySprite.m_AttachPoints, prevAttachPointLen + attaches.Length);
            System.Array.Copy(attaches, 0, jellySprite.m_AttachPoints, prevAttachPointLen, attaches.Length);
        }

        jellySprite.m_NumAttachPoints = jellySprite.m_AttachPoints.Length;
    }*/

    void M8.IPoolDespawn.OnDespawned() {        
        state = State.None;
                
        if(signalInvokeDespawn)
            signalInvokeDespawn.Invoke(this);
    }

    void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
        jellySprite.Init();

        mNumber = 0;
        mInputLocked = false;
        mInputLockedInternal = false;

        if(parms != null) {
            if(parms.ContainsKey(parmNumber))
                mNumber = parms.GetValue<int>(parmNumber);
        }

        ApplyNumberDisplay();

        state = State.Spawning;
    }

    public void OnPointerEnter(JellySprite jellySprite, int index, PointerEventData eventData) {
        if(inputLocked)
            return;

        isHighlighted = true;

        if(state == State.Normal) {
            //highlight on
            ApplyJellySpriteMaterial(hoverDragMaterial);

            if(highlightGO) highlightGO.SetActive(true);
        }
    }

    public void OnPointerExit(JellySprite jellySprite, int index, PointerEventData eventData) {
        if(inputLocked)
            return;

        isHighlighted = false;

        //highlight off
        if(state == State.Normal) {
            if(!isDragging)
                ApplyJellySpriteMaterial(null);

            if(highlightGO) highlightGO.SetActive(false);
        }
    }

    public void OnDragBegin(JellySprite jellySprite, int index, PointerEventData eventData) {
        if(inputLocked)
            return;

        DragStart();

        DragUpdate(eventData, index);

        if(signalInvokeDragBegin)
            signalInvokeDragBegin.Invoke(this);
    }

    public void OnDrag(JellySprite jellySprite, int index, PointerEventData eventData) {
        if(!isDragging)
            return;

        DragUpdate(eventData, index);
    }

    public void OnDragEnd(JellySprite jellySprite, int index, PointerEventData eventData) {
        if(!isDragging)
            return;

        isDragging = false;
        
        //signal
        if(signalInvokeDragEnd)
            signalInvokeDragEnd.Invoke(this);

        DragEnd();
    }

    IEnumerator DoSpawn() {
        M8.SoundPlaylist.instance.Play(soundSpawn, false);

        if(animator && !string.IsNullOrEmpty(takeSpawn))
            yield return animator.PlayWait(takeSpawn);
        else
            yield return null;

        mRout = null;

        state = State.Normal;

        //impulse center
        var centerDir = M8.MathUtil.RotateAngle(Vector2.up, Random.Range(0f, 360f));
        jellySprite.CentralPoint.Body2D.AddForce(centerDir * spawnCenterImpulse.random, ForceMode2D.Impulse);

        //impulse edges
        if(jellySprite.ReferencePoints.Count > 1) {
            var edgeDir = Vector2.right;
            var rotAmt = 360f / (jellySprite.ReferencePoints.Count - 1);
            for(int i = 1; i < jellySprite.ReferencePoints.Count; i++) {
                var refPt = jellySprite.ReferencePoints[i];
                refPt.Body2D.AddForce(edgeDir * spawnEdgeImpulse.random, ForceMode2D.Impulse);
                edgeDir = M8.MathUtil.RotateAngle(edgeDir, rotAmt);
            }
        }
    }

    IEnumerator DoDespawn() {
        if(animator && !string.IsNullOrEmpty(takeDespawn))
            yield return animator.PlayWait(takeDespawn);
        else
            yield return null;

        mRout = null;

        poolData.Release();
    }

    IEnumerator DoCorrect() {
        ApplyJellySpriteMaterial(correctMaterial);

        if(correctStartDelay > 0f)
            yield return new WaitForSeconds(correctStartDelay);
        else
            yield return null;

        ApplyJellySpriteMaterial(null);

        //something fancy
        if(animator && !string.IsNullOrEmpty(takeCorrect))
            yield return animator.PlayWait(takeCorrect);

        mRout = null;

        poolData.Release();
    }

    IEnumerator DoError() {
        //error highlight
        ApplyJellySpriteMaterial(errorMaterial);

        yield return new WaitForSeconds(errorDuration);

        ApplyJellySpriteMaterial(null);

        mRout = null;

        state = State.Normal;
    }

    IEnumerator DoEyeBlinking() {
        var blinkCloseWait = new WaitForSeconds(eyeBlinkCloseDelay);

        while(true) {
            SetEyesSprite(eyeSpriteNormal);

            yield return new WaitForSeconds(Random.Range(eyeBlinkOpenDelayMin, eyeBlinkOpenDelayMax));

            SetEyesSprite(eyeSpriteClose);

            yield return blinkCloseWait;
        }
    }
        
    private void RefreshMouthSprite() {
        if(!mouthSpriteRender)
            return;

        Sprite spr;
                
        switch(mState) {
            case State.Correct:
                spr = mouthSpriteCorrect;
                break;

            case State.Error:
                spr = mouthSpriteError;
                break;

            default:
                if(isDragging)
                    spr = mouthSpriteDragging;
                else if(isConnected)
                    spr = mouthSpriteConnected;
                else
                    spr = mouthSpriteNormal;
                break;
        }

        mouthSpriteRender.sprite = spr;
    }

    private void SetEyeBlinking(bool active) {
        if(active) {
            if(mEyeBlinkRout == null)
                mEyeBlinkRout = StartCoroutine(DoEyeBlinking());
        }
        else {
            if(mEyeBlinkRout != null) {
                StopCoroutine(mEyeBlinkRout);
                mEyeBlinkRout = null;

                SetEyesSprite(eyeSpriteNormal);
            }
        }
    }

    private void SetEyesSprite(Sprite spr) {
        for(int i = 0; i < eyeSpriteRenders.Length; i++) {
            if(eyeSpriteRenders[i])
                eyeSpriteRenders[i].sprite = spr;
        }
    }

    private Vector2 GetWorldPoint(Vector2 screenPos) {
        if(!mDragCamera)
            mDragCamera = Camera.main;

        if(mDragCamera)
            return mDragCamera.ScreenToWorldPoint(screenPos);

        return Vector2.zero;
    }

    private void DragStart() {
        isDragging = true;

        //display stuff, sound, etc.
        RefreshMouthSprite();

        ApplyJellySpriteMaterial(hoverDragMaterial);
    }

    private void DragUpdate(PointerEventData eventData, int index) {
        dragRefPointIndex = index;

        var prevDragPointerGO = dragPointerGO;
        dragPointerGO = eventData.pointerCurrentRaycast.gameObject;

        if(dragPointerGO) {
            //update ref.
            if(dragPointerGO != prevDragPointerGO) {
                dragPointerJellySpriteRefPt = dragPointerGO.GetComponent<JellySpriteReferencePoint>();
            }

            dragPoint = eventData.pointerCurrentRaycast.worldPosition;
        }
        else {
            dragPointerJellySpriteRefPt = null;

            //grab point from main camera
            dragPoint = GetWorldPoint(eventData.position);
        }
    }

    private void DragInvalidate() {
        if(isDragging) {
            //signal with no dragPointer
            dragPointerGO = null;
            dragPointerJellySpriteRefPt = null;

            if(signalInvokeDragEnd)
                signalInvokeDragEnd.Invoke(this);
        }

        DragEnd();
    }

    private void DragEnd() {
        dragRefPointIndex = -1;
        isDragging = false;
        dragPointerGO = null;
        dragPointerJellySpriteRefPt = null;

        //hide display, etc.
        RefreshMouthSprite();

        if(state == State.Normal)
            ApplyJellySpriteMaterial(null);
    }

    private void ClearRout() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }
        
    private void ApplyNumberDisplay() {
        if(numericText)
            numericText.text = mNumber.ToString();
    }

    private void ApplyInputLocked() {
        if(inputLocked) {
            //clear pointer highlight
            //update highlight
            if(highlightGO) highlightGO.SetActive(false);
            isHighlighted = false;

            if(isDragging)
                DragInvalidate();
        }
    }
        
    private void ApplyState() {
        ClearRout();

        ApplyJellySpriteMaterial(null);

        bool isDragInvalid = false;
                
        switch(mState) {
            case State.Normal:
                SetEyeBlinking(true);
                break;

            case State.Spawning:
                SetEyeBlinking(false);
                hintActive = false;
                //animate, and then set state to normal
                mRout = StartCoroutine(DoSpawn());
                break;

            case State.Despawning:
                SetEyeBlinking(false);
                hintActive = false;
                mInputLockedInternal = true;
                ApplyInputLocked();

                //animate and then release
                mRout = StartCoroutine(DoDespawn());
                break;

            case State.Correct:
                SetEyeBlinking(false);
                hintActive = false;
                mInputLockedInternal = true;
                ApplyInputLocked();

                mRout = StartCoroutine(DoCorrect());
                break;

            case State.Error:
                SetEyeBlinking(false);
                mRout = StartCoroutine(DoError());
                break;

            default:
                SetEyeBlinking(false);
                hintActive = false;
                isDragInvalid = true;
                break;
        }

        if(isDragInvalid)
            DragInvalidate();

        mIsConnected = false;

        RefreshMouthSprite();

        if(highlightGO) highlightGO.SetActive(false);
        isHighlighted = false;
    }
}
