using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class InstallBroadcastCycle
{
    [MenuItem("Tools/Interactions/Configure broadcast breaks")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        const string path="Assets/Sounds/Broadcast/sibgame2026oct.mp3";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(AudioImporter)AssetImporter.GetAtPath(path);
        var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;
        importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=true;importer.SaveAndReimport();
        var player=Object.FindFirstObjectByType<SurvivalPlayerController>();
        var cycle=player.GetComponent<BroadcastCycle>();if(!cycle)cycle=Undo.AddComponent<BroadcastCycle>(player.gameObject);
        Undo.RecordObject(cycle,"Configure broadcast cycle");
        cycle.player=player;cycle.interaction=player.GetComponent<PlayerInteraction>();cycle.onAirMusic=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if(!cycle.onAirSource)cycle.onAirSource=Source(player.transform,"Broadcast — on air music",true);
        if(!cycle.advertisementSource)cycle.advertisementSource=Source(player.transform,"Broadcast — advertisement music",true);
        if(!cycle.announcerSource)cycle.announcerSource=Source(player.transform,"Broadcast — commentator",false);
        cycle.onAirSource.clip=cycle.onAirMusic;
        EditorUtility.SetDirty(cycle);EditorUtility.SetDirty(cycle.onAirSource);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();Selection.activeGameObject=player.gameObject;
    }
    static AudioSource Source(Transform parent,string name,bool loop)
    {
        var obj=new GameObject(name);Undo.RegisterCreatedObjectUndo(obj,"Add broadcast audio");obj.transform.SetParent(parent,false);
        var source=obj.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.volume=0;
        source.priority=loop?160:32;return source;
    }
}
