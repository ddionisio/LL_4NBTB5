using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(UnityJellySprite))]
class UnityJellySpriteEditor : JellySpriteEditor
{
	public SerializedProperty m_Sprite;
	public Object m_InitialSprite;

	//MODIFIED: allow custom material
	public SerializedProperty m_Material;
	public Object m_InitialMaterial;

	protected override void OnEnable() 
	{
		base.OnEnable();
		m_Sprite = serializedObject.FindProperty("m_Sprite");

		//MODIFIED: allow custom material
		m_Material = serializedObject.FindProperty("m_Material");
	}
	
	protected override void DisplayInspectorGUI()
	{
		EditorGUILayout.PropertyField(m_Sprite, new GUIContent("Sprite"));

		//MODIFIED: allow custom material
		EditorGUILayout.PropertyField(m_Material, new GUIContent("Material"));

		base.DisplayInspectorGUI();

		if(GUILayout.Button("Refresh Render")) {
			var jellySpr = target as JellySprite;
			UnityJellySprite.ClearMaterials(true);
			jellySpr.RefreshMesh();
		}
	}

	protected override void StoreInitialValues()
	{
		m_InitialSprite = m_Sprite.objectReferenceValue;

		//MODIFIED: allow custom material
		m_InitialMaterial = m_Material.objectReferenceValue;

		base.StoreInitialValues();
	}

    protected override void CheckForObjectChanges()
    {
        base.CheckForObjectChanges();
        JellySprite targetObject = this.target as JellySprite;

        if (m_InitialSprite != m_Sprite.objectReferenceValue)
        {
            Sprite sprite = m_Sprite.objectReferenceValue as Sprite;
			if(sprite) {
				Bounds bounds = sprite.bounds;
				float pivotX = -bounds.center.x / bounds.extents.x;
				float pivotY = -bounds.center.y / bounds.extents.y;
				targetObject.m_CentralBodyOffset = targetObject.m_SoftBodyOffset = new Vector3(pivotX * bounds.extents.x * targetObject.m_SpriteScale.x, pivotY * bounds.extents.y * targetObject.m_SpriteScale.y, 0.0f);
				targetObject.RefreshMesh();
			}
        }
		//MODIFIED: allow custom material
		else if(m_InitialMaterial != m_Material.objectReferenceValue) {
			targetObject.ReInitMaterial();
		}
	}    

	void OnSceneGUI ()
	{
		UpdateHandles();
	}
}