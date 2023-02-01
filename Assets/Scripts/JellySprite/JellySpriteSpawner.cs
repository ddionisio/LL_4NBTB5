using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellySpriteSpawner : MonoBehaviour {
    [System.Serializable]
    public class TemplateInfo {
        public string name;
        public GameObject template;
        public int capacity;
    }

    public string poolGroup = "jelly";
    public TemplateInfo[] templates;

    private M8.PoolController mPool;
    private M8.GenericParams mSpawnParms = new M8.GenericParams();

    public GameObject Spawn(string templateName, Vector2 position, float rotate, M8.GenericParams parms) {
        GameObject spawnGO = null;

        TemplateInfo template = null;
        for(int i = 0; i < templates.Length; i++) {
            if(templates[i].name == templateName) {
                template = templates[i];
                break;
            }
        }

        if(template != null) {
            mSpawnParms.Clear();

            mSpawnParms[JellySpriteSpawnController.parmPosition] = position;
            mSpawnParms[JellySpriteSpawnController.parmRotation] = rotate;

            mSpawnParms.Merge(parms, true);

            var spawn = mPool.Spawn(templateName, templateName, null, mSpawnParms);
            spawnGO = spawn.gameObject;
        }
        else
            Debug.LogWarning("Template does not exists: " + templateName);

        return spawnGO;
    }

    public void DespawnAll() {
        for(int i = 0; i < templates.Length; i++)
            mPool.ReleaseAllByType(templates[i].name);
    }

    void Awake() {
        mPool = M8.PoolController.CreatePool(poolGroup);
        
        for(int i = 0; i < templates.Length; i++) {
            var template = templates[i];
            mPool.AddType(template.name, template.template, template.capacity, template.capacity);
        }
    }
}
