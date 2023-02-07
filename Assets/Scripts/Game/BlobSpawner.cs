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
            mSpawnPoints = new Vector2[spawnPointsRoot.childCount];
            for(int i = 0; i < spawnPointsRoot.childCount; i++)
                mSpawnPoints[i] = spawnPointsRoot.GetChild(i).position;

            if(isShuffle)
                M8.ArrayUtil.Shuffle(mSpawnPoints);

            mCurSpawnPtInd = 0;
        }
    }

    public struct SpawnInfo {
        public int templateIndex;
        public int number;
    }

    [Header("Template")]
    public string poolGroup = "blobs";
    public TemplateGroup[] templateGroups;

    [Header("Spawn")]
    public int spawnActiveCount = 6;
    public bool spawnPointsShuffle = true;
    public LayerMask spawnPointCheckMask; //ensure spot is fine to spawn    
    public float spawnDelay = 0.3f;

    public int spawnQueueCount { get { return mSpawnQueue.Count; } }

    public Queue<SpawnInfo> spawnQueue { get { return mSpawnQueue; } }

    public M8.CacheList<Blob> blobActives { get { return mBlobActives; } }

    private M8.PoolController mPool;

    private Queue<SpawnInfo> mSpawnQueue = new Queue<SpawnInfo>();
    private Coroutine mSpawnRout;

    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    private M8.CacheList<Blob> mBlobActives;

    private System.Text.StringBuilder mBlobNameCache = new System.Text.StringBuilder();

    public void InitBlobTemplate(BlobTemplateData blobTemplateData) {
        if(!mPool)
            mPool = M8.PoolController.CreatePool(poolGroup);

        //setup template (pool init, spawn points)
        int templateInd = GetTemplateIndex(blobTemplateData);
        if(templateInd == -1) {
            Debug.LogError("Template Not Found: " + blobTemplateData.name);
            return;
        }

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

    public void Spawn(int templateIndex, int number) {        
        if(templateIndex < 0 || templateIndex >= templateGroups.Length) {
            Debug.LogWarning("Invalid template index: " + templateIndex);
            return;
        }

        mSpawnQueue.Enqueue(new SpawnInfo { templateIndex = templateIndex, number = number });
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

            //find valid spawn point
            Vector2 spawnPt = Vector2.zero;

            while(true) {
                var pt = templateGrp.spawnPoint;

                //check if valid
                var coll = Physics2D.OverlapCircle(pt, templateGrp.blobData.spawnPointCheckRadius, spawnPointCheckMask);
                if(!coll) {
                    spawnPt = pt;
                    break;
                }

                //invalid, check next
                yield return null;
            }

            //spawn
            mSpawnParms[JellySpriteSpawnController.parmPosition] = spawnPt;
            mSpawnParms[Blob.parmNumber] = spawnInfo.number;

            var template = templateGrp.blobData.template;

            mBlobNameCache.Clear();
            mBlobNameCache.Append(template.name);
            mBlobNameCache.Append(' ');
            mBlobNameCache.Append(spawnInfo.number);

            var blob = mPool.Spawn<Blob>(template.name, mBlobNameCache.ToString(), null, mSpawnParms);

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
