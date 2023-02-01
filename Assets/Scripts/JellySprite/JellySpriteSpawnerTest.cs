using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellySpriteSpawnerTest : MonoBehaviour {    
    public JellySpriteSpawner spawner;

    [Header("Test Data")]
    public Transform spawnRoot; //grab children, and use those to spawn (name is the templateName)

    public void Spawn() {
        for(int i = 0; i < spawnRoot.childCount; i++) {
            var spawn = spawnRoot.GetChild(i);
            spawner.Spawn(spawn.name, spawn.position, spawn.eulerAngles.z, null);
        }
    }

    public void Despawn() {
        spawner.DespawnAll();
    }
}
