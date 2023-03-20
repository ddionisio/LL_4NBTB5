using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobSpawner : MonoBehaviour {
    [System.Serializable]
    public class TemplateGroup {
        public BlobTemplateData blobData;
        public int poolCapacity = 3;

        [Header("Spawn Info")]
        public Transform spawnPointsRoot;

        private Vector2[] mSpawnPoints;
        private int mCurSpawnPtInd;

        public Vector2 spawnPoint {
            get {
                var pt = mSpawnPoints[mCurSpawnPtInd];

                mCurSpawnPtInd++;
                if(mCurSpawnPtInd == mSpawnPoints.Length)
                    mCurSpawnPtInd = 0;

                return pt;
            }
        }

        public void InitSpawnPoints(bool isShuffle) {
            var spawnList = new List<Vector2>(spawnPointsRoot.childCount);

            for(int i = 0; i < spawnPointsRoot.childCount; i++) {
                var t = spawnPointsRoot.GetChild(i);
                if(t.gameObject.activeSelf)
                    spawnList.Add(t.position);
            }

            mSpawnPoints = spawnList.ToArray();

            if(isShuffle)
                M8.ArrayUtil.Shuffle(mSpawnPoints);

            mCurSpawnPtInd = 0;
        }
    }

    public struct SpawnInfo {
        public string nameOverride; //ignored if null or empty
        public int templateIndex;
        public int number;
    }

    [Header("Template")]
    public string poolGroup = "blobs";
    public TemplateGroup[] templateGroups;

    [Header("Spawn")]
    public M8.ColorPalette spawnPalette;
    public int spawnActiveCount = 6;
    public bool spawnPointsShuffle = true;
    public LayerMask spawnPointCheckMask; //ensure spot is fine to spawn    
    public float spawnDelay = 0.3f;

    [Header("Spawn Clearout")]
    public float spawnClearoutForce = 5f;
    public float spawnClearoutDelay = 3f;

    public int spawnQueueCount { get { return mSpawnQueue.Count; } }

    public Queue<SpawnInfo> spawnQueue { get { return mSpawnQueue; } }

    public M8.CacheList<Blob> blobActives { get { return mBlobActives; } }

    public bool isSpawning { get { return mSpawnRout != null; } }

    private M8.PoolController mPool;

    private int[] mSpawnPaletteIndices;
    private int mSpawnPaletteCurrentInd;

    private Queue<SpawnInfo> mSpawnQueue = new Queue<SpawnInfo>();
    private Coroutine mSpawnRout;

    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    private M8.CacheList<Blob> mBlobActives;

    private System.Text.StringBuilder mBlobNameCache = new System.Text.StringBuilder();

    private Collider2D[] mColliderCache = new Collider2D[128];

    public int InitBlobTemplate(BlobTemplateData blobTemplateData) {        
        int templateInd = GetTemplateIndex(blobTemplateData);
        if(templateInd == -1) {
            Debug.LogError("Template Not Found: " + blobTemplateData.name);
            return -1;
        }

        InitBlobTemplate(templateInd);

        return templateInd;
    }

    public void InitBlobTemplate(int templateInd) {
        //setup template (pool init, spawn points)
        if(!mPool)
            mPool = M8.PoolController.CreatePool(poolGroup);

        var grp = templateGroups[templateInd];

        grp.blobData.InitPool(mPool, grp.poolCapacity);

        //generate spawn points
        grp.InitSpawnPoints(spawnPointsShuffle);
    }

    public bool CheckAnyBlobActiveState(params Blob.State[] states) {
        for(int i = 0; i < mBlobActives.Count; i++) {
            var blob = mBlobActives[i];
            if(blob) {
                for(int j = 0; j < states.Length; j++) {
                    if(blob.state == states[j])
                        return true;
                }
            }
        }

        return false;
    }

    public int GetBlobStateCount(params Blob.State[] states) {
        int count = 0;

        for(int i = 0; i < mBlobActives.Count; i++) {
            var blob = mBlobActives[i];
            if(blob) {
                for(int j = 0; j < states.Length; j++) {
                    if(blob.state == states[j]) {
                        count++;
                        break;
                    }
                }
            }
        }

        return count;
    }

    public Blob GetBlobActiveByName(string blobName) {
        if(mBlobActives == null)
            return null;

        for(int i = 0; i < mBlobActives.Count; i++) {
            var blob = mBlobActives[i];
            if(!blob)
                continue;

            if(blob.name == blobName)
                return blob;
        }

        return null;
    }

    public void DespawnAllBlobs() {
        if(mBlobActives == null)
            return;

        for(int i = 0; i < mBlobActives.Count; i++) {
            var blob = mBlobActives[i];
            if(!blob)
                continue;

            if(blob.poolData)
                blob.poolData.despawnCallback -= OnBlobRelease;

            blob.state = Blob.State.Despawning;
        }

        mBlobActives.Clear();
    }

    public void SpawnStop() {
        if(mSpawnRout != null) {
            StopCoroutine(mSpawnRout);
            mSpawnRout = null;
        }

        mSpawnQueue.Clear();
    }

    public int GetTemplateIndex(BlobTemplateData blobData) {
        int templateIndex = -1;
        for(int i = 0; i < templateGroups.Length; i++) {
            if(templateGroups[i].blobData == blobData) {
                templateIndex = i;
                break;
            }
        }

        return templateIndex;
    }

    public void Spawn(BlobTemplateData blobData, int number) {
        //grab template index
        int templateIndex = GetTemplateIndex(blobData);

        if(templateIndex == -1) {
            Debug.LogWarning("No template for: " + blobData.name);
            return;
        }

        Spawn(templateIndex, number);
    }

    public void Spawn(string nameOverride, BlobTemplateData blobData, int number) {
        //grab template index
        int templateIndex = GetTemplateIndex(blobData);

        if(templateIndex == -1) {
            Debug.LogWarning("No template for: " + blobData.name);
            return;
        }

        Spawn(nameOverride, templateIndex, number);
    }

    public void Spawn(int templateIndex, int number) {
        if(templateIndex < 0 || templateIndex >= templateGroups.Length) {
            Debug.LogWarning("Invalid template index: " + templateIndex);
            return;
        }

        mSpawnQueue.Enqueue(new SpawnInfo { templateIndex = templateIndex, number = number });
        if(mSpawnRout == null)
            mSpawnRout = StartCoroutine(DoSpawnQueue());
    }

    public void Spawn(string nameOverride, int templateIndex, int number) {
        if(templateIndex < 0 || templateIndex >= templateGroups.Length) {
            Debug.LogWarning("Invalid template index: " + templateIndex);
            return;
        }

        mSpawnQueue.Enqueue(new SpawnInfo { nameOverride = nameOverride, templateIndex = templateIndex, number = number });
        if(mSpawnRout == null)
            mSpawnRout = StartCoroutine(DoSpawnQueue());
    }

    public void RemoveFromActive(Blob blob) {
        if(mBlobActives.Remove(blob)) {
            blob.poolData.despawnCallback -= OnBlobRelease;
        }
    }

    void OnDisable() {
        SpawnStop();
    }

    void Awake() {
        mBlobActives = new M8.CacheList<Blob>(spawnActiveCount);
    }

    IEnumerator DoSpawnQueue() {
        var wait = new WaitForSeconds(spawnDelay);

        while(mSpawnQueue.Count > 0) {
            while(mBlobActives.IsFull) //wait for blobs to release
                yield return null;

            yield return wait;

            var spawnInfo = mSpawnQueue.Dequeue();

            var templateGrp = templateGroups[spawnInfo.templateIndex];

            //get spawn point, and clear out other blobs within spawn area
            Vector2 spawnPt = templateGrp.spawnPoint;
            var checkRadius = templateGrp.blobData.spawnPointCheckRadius;

            var curTime = 0f;
            while(curTime < spawnClearoutDelay) {
                var overlapCount = Physics2D.OverlapCircleNonAlloc(spawnPt, checkRadius, mColliderCache, spawnPointCheckMask);
                for(int i = 0; i < overlapCount; i++) {
                    var coll = mColliderCache[i];

                    Rigidbody2D overlapBody = null;

                    //grab central body of blob
                    var jellyRefPt = coll.GetComponent<JellySpriteReferencePoint>();
                    if(jellyRefPt) {
                        var jellySpr = jellyRefPt.ParentJellySprite;
                        if(jellySpr)
                            overlapBody = jellySpr.CentralPoint.Body2D;
                    }
                    else //some other object (connector)
                        overlapBody = coll.GetComponent<Rigidbody2D>();

                    if(overlapBody) {
                        var dir = (overlapBody.position - spawnPt).normalized;
                        overlapBody.AddForce(dir * spawnClearoutForce);
                    }
                }

                if(overlapCount == 0)
                    break;

                yield return null;

                curTime += Time.deltaTime;
            }

            //setup color
            if(spawnPalette && spawnPalette.count > 0) {
                Color spawnColor;

                if(mSpawnPaletteIndices == null) { //init
                    mSpawnPaletteIndices = new int[spawnPalette.count];
                    for(int i = 0; i < mSpawnPaletteIndices.Length; i++)
                        mSpawnPaletteIndices[i] = i;
                    M8.ArrayUtil.Shuffle(mSpawnPaletteIndices);
                }

                spawnColor = spawnPalette.GetColor(mSpawnPaletteIndices[mSpawnPaletteCurrentInd]);

                mSpawnPaletteCurrentInd++;
                if(mSpawnPaletteCurrentInd == mSpawnPaletteIndices.Length)
                    mSpawnPaletteCurrentInd = 0;

                mSpawnParms[JellySpriteSpawnController.parmColor] = spawnColor;
            }

            //spawn
            mSpawnParms[JellySpriteSpawnController.parmPosition] = spawnPt;
            mSpawnParms[Blob.parmNumber] = spawnInfo.number;

            var template = templateGrp.blobData.template;

            string blobName;

            if(string.IsNullOrEmpty(spawnInfo.nameOverride)) {
                mBlobNameCache.Clear();
                mBlobNameCache.Append(template.name);
                mBlobNameCache.Append(' ');
                mBlobNameCache.Append(spawnInfo.number);

                blobName = mBlobNameCache.ToString();
            }
            else
                blobName = spawnInfo.nameOverride;

            var blob = mPool.Spawn<Blob>(template.name, blobName, null, mSpawnParms);

            blob.poolData.despawnCallback += OnBlobRelease;

            mBlobActives.Add(blob);
        }

        mSpawnRout = null;
    }

    void OnBlobRelease(M8.PoolDataController pdc) {
        pdc.despawnCallback -= OnBlobRelease;

        for(int i = 0; i < mBlobActives.Count; i++) {
            var blob = mBlobActives[i];
            if(blob && blob.poolData == pdc) {
                mBlobActives.RemoveAt(i);
                break;
            }
        }
    }
}
