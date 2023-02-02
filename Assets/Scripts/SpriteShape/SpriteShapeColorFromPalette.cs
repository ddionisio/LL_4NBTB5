using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[ExecuteInEditMode]
public class SpriteShapeColorFromPalette : M8.ColorFromPaletteBase {
    public SpriteShapeController target;

    public override void ApplyColor() {
        target.spriteShapeRenderer.color = color;
    }

    void Awake() {
        if(!target)
            target = GetComponent<SpriteShapeController>();
    }
}
