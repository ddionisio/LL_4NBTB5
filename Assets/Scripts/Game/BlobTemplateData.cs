using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "blobTemplateData", menuName = "Game/Blob Template Data")]
public class BlobTemplateData : ScriptableObject {
    [Header("Templates")]
    public GameObject[] templates;

    [Header("Spawn Info")]
    public float spawnPointCheckRadius;

    public GameObject template {
        get { return templates.Length > 0 ? templates[Random.Range(0, templates.Length)] : null; }
    }

    public void InitPool(M8.PoolController pool, int capacity) {
        for(int i = 0; i < templates.Length; i++)
            pool.AddType(templates[i], capacity, capacity);
    }
}
