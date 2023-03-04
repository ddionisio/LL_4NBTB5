using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Jelly sprite class. Attach to any Unity sprite, and at runtime the sprite will move and
/// distort under the influence of soft body physics.
/// </summary>
[AddComponentMenu("Jelly Sprite/Unity Jelly Sprite")]
public class UnityJellySprite : JellySprite
{
	public Sprite m_Sprite;

	//MODIFIED: allow custom material
	public Material m_Material;

	//MODIFIED: allow custom material
	struct MaterialCache {
		public Material materialRef;
		public Material materialInst;
	}

	static List<MaterialCache> s_MaterialList = new List<MaterialCache>();

	//MODIFIED: cache MeshRenderer
	public MeshRenderer meshRenderer {
		get {
			if(!mMeshRenderer)
				mMeshRenderer = GetComponent<MeshRenderer>();
			return mMeshRenderer;
		}
	}
	private MeshRenderer mMeshRenderer;

	/// <summary>
	/// Jelly sprites share materials wherever possible in order to ensure that dynamic batching is maintained when
	/// eg. slicing lots of sprites that share the same sprite sheet. If you want to clear out this list 
	/// (eg. on transitioning to a new scene) then simply call this function
	/// </summary>
	public static void ClearMaterials(bool immediate)
	{
		//MODIFIED: allow custom material
		for(int i = 0; i < s_MaterialList.Count; i++) {
			if(s_MaterialList[i].materialInst) {
				if(immediate)
					DestroyImmediate(s_MaterialList[i].materialInst);
				else
					Destroy(s_MaterialList[i].materialInst);
			}
		}

		s_MaterialList.Clear();
	}
	
	/// <summary>
	/// Get the bounds of the sprite
	/// </summary>
	protected override Bounds GetSpriteBounds()
	{
		return m_Sprite.bounds;
	}

	/// <summary>
	/// Check if the sprite is valid
	/// </summary>
	protected override bool IsSpriteValid()
	{
		return m_Sprite != null;
	}
		
	/// <summary>
	/// Check if the source sprite is rotated
	/// </summary>
	protected override bool IsSourceSpriteRotated()
	{
		return false;
	}
	
	protected override void GetMinMaxTextureRect(out Vector2 min, out Vector2 max)
	{
		//MODIFIED: fix when using atlas
		if(m_Sprite.textureRect.size == Vector2.zero) { //TODO: probably better way to check if using atlas
			if(m_Sprite.uv != null && m_Sprite.uv.Length > 0) {
				min = new Vector2(float.MaxValue, float.MaxValue);
				max = new Vector2(float.MinValue, float.MinValue);
				for(int i = 0; i < m_Sprite.uv.Length; i++) {
					var p = m_Sprite.uv[i];
					if(p.x < min.x) min.x = p.x;
					if(p.x > max.x) max.x = p.x;
					if(p.y < min.y) min.y = p.y;
					if(p.y > max.y) max.y = p.y;
				}
			}
			else {
				min = max = Vector2.zero;
			}
		}
		else {
			Rect textureRect = m_Sprite.textureRect;
			min = new Vector2(textureRect.xMin / (float)m_Sprite.texture.width, textureRect.yMin / (float)m_Sprite.texture.height);
			max = new Vector2(textureRect.xMax / (float)m_Sprite.texture.width, textureRect.yMax / (float)m_Sprite.texture.height);
		}
	}
	
	protected override void InitMaterial()
	{
		//MODIFIED: allow custom material
		//MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		Material material = null;

		// Grab a material from the cache, generate a new one if none exist
		for(int loop = 0; loop < s_MaterialList.Count; loop++) {
			//MODIFIED: allow custom material
			var matDat = s_MaterialList[loop];
			if(matDat.materialRef == m_Material && matDat.materialInst && matDat.materialInst.mainTexture.GetInstanceID() == m_Sprite.texture.GetInstanceID()) {
				material = matDat.materialInst;
				break;
			}
			/*if(s_MaterialList[loop] != null && s_MaterialList[loop].mainTexture.GetInstanceID() == m_Sprite.texture.GetInstanceID())
			{
				material = s_MaterialList[loop];
			}*/
		}

		if(material == null) {
			//MODIFIED: allow custom material
			material = m_Material ? new Material(m_Material) : new Material(Shader.Find("Sprites/Default"));
			material.mainTexture = m_Sprite.texture;
			material.name = m_Sprite.texture.name + "_Jelly";

			s_MaterialList.Add(new MaterialCache { materialRef = m_Material, materialInst = material });

			/*material = new Material(Shader.Find("Sprites/Default"));
			material.mainTexture = m_Sprite.texture;
			material.name = m_Sprite.texture.name + "_Jelly";
			s_MaterialList.Add(material);*/
		}

		meshRenderer.sharedMaterial = material;
	}

#if UNITY_EDITOR
	[MenuItem("GameObject/Create Other/Jelly Sprite/Unity Jelly Sprite", false, 12951)]
	static void DoCreateSpriteObject()
	{
		GameObject gameObject = new GameObject("JellySprite");
		gameObject.AddComponent<UnityJellySprite>();
		Selection.activeGameObject = gameObject;
		Undo.RegisterCreatedObjectUndo(gameObject, "Create Jelly Sprite");
	}
#endif
}