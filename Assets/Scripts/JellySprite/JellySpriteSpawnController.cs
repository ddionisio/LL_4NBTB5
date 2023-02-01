using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellySpriteSpawnController : MonoBehaviour, M8.IPoolSpawn, M8.IPoolDespawn {
    public const string parmPosition = "position";
    public const string parmRotation = "rotation";
    public const string parmSprite = "sprite";
    public const string parmColor = "color";

    public UnityJellySprite jellySprite;
    public bool revertLayerOnDespawn;

    private bool mIsInit = false;
    private int mDefaultLayer;
    private Sprite mDefaultSprite;
    private Color mDefaultColor;

    void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
        Init();

        Sprite spr = mDefaultSprite;
        Color clr = mDefaultColor;

        Vector2 pos = Vector2.zero;
        float rot = 0f;

        if(parms != null) {
            if(parms.ContainsKey(parmPosition)) pos = parms.GetValue<Vector2>(parmPosition);
            if(parms.ContainsKey(parmRotation)) rot = parms.GetValue<float>(parmRotation);
            if(parms.ContainsKey(parmSprite)) spr = parms.GetValue<Sprite>(parmSprite);
            if(parms.ContainsKey(parmColor)) clr = parms.GetValue<Color>(parmColor);
        }

        bool isInit = jellySprite.CentralPoint != null;

        if(isInit) {
            //need to reinitialize mesh/material?
            bool isSpriteChanged = jellySprite.m_Sprite != spr;
            bool isColorChanged = jellySprite.m_Color != clr;

            jellySprite.m_Sprite = spr;
            jellySprite.m_Color = clr;

            if(isColorChanged || isSpriteChanged)
                jellySprite.RefreshMesh(); //just to ensure sprite uv's are properly applied
            //else if(isSpriteChanged)
                //jellySprite.ReInitMaterial();

            //reset and apply telemetry
            jellySprite.Reset(pos, new Vector3(0f, 0f, rot));
        }
        else {
            //directly apply telemetry
            var trans = jellySprite.transform;
            trans.position = pos;
            trans.eulerAngles = new Vector3(0f, 0f, rot);
        }
    }

    void M8.IPoolDespawn.OnDespawned() {
        if(revertLayerOnDespawn) {
            if(jellySprite.ReferencePoints != null) {
                for(int i = 0; i < jellySprite.ReferencePoints.Count; i++) {
                    var refPt = jellySprite.ReferencePoints[i];
                    if(refPt.GameObject)
                        refPt.GameObject.layer = mDefaultLayer;
                }
            }
        }
    }

    void Init() {
        if(mIsInit) return;

        if(!jellySprite)
            jellySprite = GetComponent<UnityJellySprite>();

        mDefaultLayer = jellySprite.gameObject.layer;
        mDefaultSprite = jellySprite.m_Sprite;
        mDefaultColor = jellySprite.m_Color;

        mIsInit = true;
    }
}
