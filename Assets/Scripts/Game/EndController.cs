using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

public class EndController : GameModeController<EndController> {

    protected override void OnInstanceInit() {
        base.OnInstanceInit();
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        while(!LoLManager.instance.isReady)
            yield return null;
    }
}