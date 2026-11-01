using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class InstallTownMonsterReveal
{
    [MenuItem("Tools/Interactions/Configure town monster reveal")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        var source=Object.FindFirstObjectByType<CrimsonMassDialogue>();
        var neighbour=Object.FindFirstObjectByType<DrunkDialogue>();
        var reveal=source.GetComponent<TownMonsterReveal>();if(!reveal)reveal=Undo.AddComponent<TownMonsterReveal>(source.gameObject);
        Undo.RecordObjects(new Object[]{reveal,source,neighbour,neighbour.GetComponent<InteractionObject>()},"Configure town reveal");
        reveal.sourceDialogue=source;
        reveal.monsterPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CrimsonMassIdle/CrimsonMass_Idle.prefab");
        reveal.neighbour=neighbour.GetComponent<InteractionObject>();
        reveal.townspeople=Object.FindObjectsByType<InteractionObject>(FindObjectsSortMode.None).Where(i=>i.kind==InteractionKind.Talk&&i!=reveal.neighbour&&i.gameObject!=source.gameObject).ToArray();
        source.townReveal=reveal;
        neighbour.speaker="Сосед";reveal.neighbour.displayName="Сосед";
        foreach(var target in new Object[]{reveal,source,neighbour,reveal.neighbour})EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(source.gameObject.scene);EditorSceneManager.SaveScene(source.gameObject.scene);
        Selection.activeGameObject=source.gameObject;
    }
}
