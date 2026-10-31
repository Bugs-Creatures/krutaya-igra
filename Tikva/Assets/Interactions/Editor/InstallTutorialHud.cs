using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InstallTutorialHud
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>().gameObject;
        if(!player.GetComponent<PlayerMapInventory>())player.AddComponent<PlayerMapInventory>();
        if(!player.GetComponent<PlayerTutorialHud>())player.AddComponent<PlayerTutorialHud>();
        if(!player.GetComponent<DroppedItemHighlight>())player.AddComponent<DroppedItemHighlight>();
        foreach(var item in Object.FindObjectsByType<MapItem>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {var target=item.GetComponent<InteractionObject>();target.hoverTextSize=14;target.availableHoverText="подобрать карту";EditorUtility.SetDirty(target);}
        EditorSceneManager.MarkSceneDirty(player.scene);EditorSceneManager.SaveScene(player.scene);
    }
}
