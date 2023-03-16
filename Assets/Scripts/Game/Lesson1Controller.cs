using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class Lesson1Controller : GameModeController<Lesson1Controller> {

    protected override void OnInstanceInit() {
        base.OnInstanceInit();
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        while(!LoLManager.instance.isReady)
            yield return null;
    }
}
