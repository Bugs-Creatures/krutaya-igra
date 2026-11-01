using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class InstallHeldItemRendering
{
    [MenuItem("Tools/Interactions/Install first person item rendering")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layer=tags.FindProperty("layers").GetArrayElementAtIndex(FirstPersonHeldItemRenderer.ItemLayer);
        if(!string.IsNullOrEmpty(layer.stringValue)&&layer.stringValue!=FirstPersonHeldItemRenderer.LayerName)
            throw new System.InvalidOperationException("Layer 31 is already in use.");
        layer.stringValue=FirstPersonHeldItemRenderer.LayerName;tags.ApplyModifiedPropertiesWithoutUndo();
        int worldMask=~(1<<FirstPersonHeldItemRenderer.ItemLayer);
        foreach(var path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"})
        {
            var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);if(!data)continue;
            data.opaqueLayerMask&=worldMask;data.transparentLayerMask&=worldMask;
            var feature=data.rendererFeatures.OfType<FirstPersonHeldItemFeature>().FirstOrDefault();
            if(!feature)
            {
                feature=ScriptableObject.CreateInstance<FirstPersonHeldItemFeature>();feature.name="First Person Held Item";
                AssetDatabase.AddObjectToAsset(feature,data);data.rendererFeatures.Add(feature);
            }
            feature.SetActive(true);
            var serialized=new SerializedObject(data);var map=serialized.FindProperty("m_RendererFeatureMap");
            map.arraySize=data.rendererFeatures.Count;
            for(int n=0;n<map.arraySize;n++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[n],out string guid,out long id);
                map.GetArrayElementAtIndex(n).longValue=id;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();data.SetDirty();EditorUtility.SetDirty(data);
        }
        var interaction=Object.FindAnyObjectByType<PlayerInteraction>();
        var view=interaction.GetComponent<FirstPersonHeldItemRenderer>();
        if(!view)view=interaction.gameObject.AddComponent<FirstPersonHeldItemRenderer>();
        view.interaction=interaction;EditorUtility.SetDirty(view);
        var camera=interaction.player.cameraRig.output;camera.cullingMask|=1<<FirstPersonHeldItemRenderer.ItemLayer;EditorUtility.SetDirty(camera);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
        EditorSceneManager.SaveScene(interaction.gameObject.scene);
    }
}
