using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobSpawner : MonoBehaviour {
    [System.Serializable]
    public class TemplateGroup {
        public string name;
        public GameObject[] templates;

        public GameObject template {
            get { return templates.Length > 0 ? templates[Random.Range(0, templates.Length)] : null; }
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
    public Transform spawnPointsRoot; //grab child for spawn point
    public bool spawnPointsShuffle = true;
    public LayerMask spawnPointCheckMask; //ensure spot is fine to spawn
    public float spawnPointCheckRadius;
    public float spawnDelay = 0.3f;

    public int spawnQueueCount { get { return mSpawnQueue.Count; } }

    public Queue<SpawnInfo> spawnQueue { get { return mSpawnQueue; } }

    public M8.CacheList<Blob> blobActives { get { return mBlobActives; } }

    private M8.PoolController mPool;

    private Vector2[] mSpawnPts;
    private int mCurSpawnPtInd;

    private Queue<SpawnInfo> mSpawnQueue = new Queue<SpawnInfo>();
    private Coroutine mSpawnRout;

    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    private M8.CacheList<Blob> mBlobActives;

    private System.Text.StringBuilder mBlobNameCache = new System.Text.StringBuilder();

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

    public void Spawn(string templateName, int number) {
        //grab template index
        int templateIndex = -1;
        for(int i = 0; i < templateGroups.Length; i++) {
            if(templateGroups[i].name == templateName) {
                templateIndex = i;
                break;
            }
        }

        if(templateIndex == -1) {
            Debug.LogWarning("No template for: " + templateName);
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

        int blobCapacity = GameData.instance.blobSpawnCount;

        mBlobActives = new M8.CacheList<Blob>(blobCapacity);

        //setup pool
        mPool = M8.PoolController.CreatePool(poolGroup);
        for(int i = 0; i < templateGroups.Length; i++) {
            var grp = templateGroups[i];
            for(int j = 0; j < grp.templates.Length; j++)
                mPool.AddType(grp.templates[j], blobCapacity, blobCapacity);
        }

        //generate spawn points
        mSpawnPts = new Vector2[spawnPointsRoot.childCount];
        for(int i = 0; i < spawnPointsRoot.childCount; i++)
            mSpawnPts[i] = spawnPointsRoot.GetChild(i).position;

        if(spawnPointsShuffle)
            M8.ArrayUtil.Shuffle(mSpawnPts);

        mCurSpawnPtInd = 0;
    }

    IEnumerator DoSpawnQueue() {
        var wait = new WaitForSeconds(spawnDelay);

        while(mSpawnQueue.Count > 0) {
            while(mBlobActives.IsFull) //wait for blobs to release
                yield return null;

            yield return wait;

            var spawnInfo = mSpawnQueue.Dequeue();

            //find valid spawn point
            Vector2 spawnPt = Vector2.zero;

            while(true) {
                var pt = mSpawnPts[mCurSpawnPtInd];
                mCurSpawnPtInd++;
                if(mCurSpawnPtInd == mSpawnPts.Length)
                    mCurSpawnPtInd = 0;

                //check if valid
                var coll = Physics2D.OverlapCircle(pt, spawnPointCheckRadius, spawnPointCheckMask);
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

            var template = templateGroups[spawnInfo.templateIndex].template;

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
