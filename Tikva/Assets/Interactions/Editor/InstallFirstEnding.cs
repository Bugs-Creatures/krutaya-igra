using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public static class InstallFirstEnding
{
    const string EffectPath="Assets/Interactions/NeighbourDeathEffect.prefab";
    const string EndingPath="Assets/Scenes/Ending01.unity";
    [MenuItem("Tools/Interactions/Configure first ending")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        var player=Object.FindFirstObjectByType<SurvivalPlayerController>();
        var neighbour=Object.FindFirstObjectByType<DrunkDialogue>();
        var axe=Object.FindFirstObjectByType<CrimsonMassDialogue>().axeReward;
        var gameScene=player.gameObject.scene;
        if(!axe.GetComponent<AxeWeapon>())Undo.AddComponent<AxeWeapon>(axe.gameObject);
        var root=GameObject.Find("Ending 01 runtime");
        if(!root){root=new GameObject("Ending 01 runtime");Undo.RegisterCreatedObjectUndo(root,"Add first ending");}
        var transition=root.GetComponent<EndingTransition>();if(!transition)transition=Undo.AddComponent<EndingTransition>(root);
        transition.player=player;transition.endingScene="Ending01";
        if(!transition.fade)
        {
            var canvas=Canvas("Ending fade",root.transform,32000);
            var group=canvas.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
            Fill(canvas.transform,"Black fade");transition.fade=group;
        }
        var victim=neighbour.GetComponent<NeighbourEndingTarget>();if(!victim)victim=Undo.AddComponent<NeighbourEndingTarget>(neighbour.gameObject);
        victim.ending=transition;
        var effectAsset=AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath);
        var effect=effectAsset?effectAsset.GetComponent<NeighbourDeathEffect>():null;
        if(!effect)effect=MakeEffect();victim.deathEffectPrefab=effect;
        var testAxe=GameObject.Find("Axe_Test_Bench");
        if(!testAxe)
        {
            testAxe=Object.Instantiate(axe.gameObject);testAxe.name="Axe_Test_Bench";
            Undo.RegisterCreatedObjectUndo(testAxe,"Place test axe on bench");
            var pickupGroup=GameObject.Find("Предметы на карте");if(pickupGroup)testAxe.transform.SetParent(pickupGroup.transform,true);
            testAxe.transform.SetPositionAndRotation(new Vector3(-1.55f,6.23745f,9f),Quaternion.Euler(0,8,0));
            var body=testAxe.GetComponent<Rigidbody>();if(body){body.isKinematic=false;body.useGravity=true;}
            testAxe.SetActive(true);
        }
        foreach(var target in new Object[]{transition,victim,axe})EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(gameScene);EditorSceneManager.SaveScene(gameScene);
        if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(EndingPath))
        {
            var ending=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(ending);
            var cameraObject=new GameObject("Main Camera");var camera=cameraObject.AddComponent<Camera>();
            cameraObject.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=0;
            cameraObject.AddComponent<AudioListener>();
            var canvas=Canvas("Ending Canvas",null,0);Fill(canvas.transform,"Black Background");
            EditorSceneManager.SaveScene(ending,EndingPath);EditorSceneManager.CloseScene(ending,true);SceneManager.SetActiveScene(gameScene);
        }
        var scenes=EditorBuildSettings.scenes.ToList();var entry=scenes.FirstOrDefault(s=>s.path==EndingPath);
        if(entry==null)scenes.Add(new EditorBuildSettingsScene(EndingPath,true));else entry.enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();Selection.activeGameObject=testAxe;
    }

    static Canvas Canvas(string name,Transform parent,int order)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        if(parent)obj.transform.SetParent(parent,false);
        var canvas=obj.GetComponent<UnityEngine.Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
        var scaler=obj.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;return canvas;
    }
    static void Fill(Transform parent,string name)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));obj.transform.SetParent(parent,false);
        var rect=(RectTransform)obj.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=obj.GetComponent<UnityEngine.UI.Image>();image.color=Color.black;image.raycastTarget=false;
    }
    static NeighbourDeathEffect MakeEffect()
    {
        var root=new GameObject("NeighbourDeathEffect");
        var effect=root.AddComponent<NeighbourDeathEffect>();var pieces=new List<Rigidbody>();var random=new System.Random(67);
        var flesh=FleshFragmentMaterial();
        Mesh sphere=null;
        for(int n=0;n<14;n++)
        {
            var chunk=GameObject.CreatePrimitive(PrimitiveType.Sphere);chunk.name="Flesh piece "+(n+1);chunk.layer=2;chunk.transform.SetParent(root.transform,false);
            chunk.transform.localPosition=new Vector3((float)(random.NextDouble()-.5)*.45f,.35f+n/14f*1.25f,(float)(random.NextDouble()-.5)*.3f);
            chunk.transform.localScale=new Vector3(.15f+(float)random.NextDouble()*.18f,.17f+(float)random.NextDouble()*.22f,.13f+(float)random.NextDouble()*.16f);
            chunk.GetComponent<Renderer>().sharedMaterial=flesh;sphere=chunk.GetComponent<MeshFilter>().sharedMesh;
            chunk.GetComponent<MeshFilter>().sharedMesh=FleshFragmentMesh(sphere);
            var body=chunk.AddComponent<Rigidbody>();body.mass=.12f;body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;pieces.Add(body);
        }
        effect.pieces=pieces.ToArray();
        var bloodObject=new GameObject("Blood burst");bloodObject.transform.SetParent(root.transform,false);bloodObject.transform.localPosition=Vector3.up;
        var blood=bloodObject.AddComponent<ParticleSystem>();blood.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=blood.main;main.loop=false;main.playOnAwake=false;main.duration=.25f;main.startLifetime=new ParticleSystem.MinMaxCurve(1.2f,2f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(1.8f,3.6f);main.startSize=new ParticleSystem.MinMaxCurve(.023f,.065f);main.gravityModifier=1;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=200;
        var emission=blood.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,150)});
        var shape=blood.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.22f;
        var colors=blood.colorOverLifetime;colors.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.65f,.015f,.025f),0),new GradientColorKey(new Color(.22f,0,0),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});colors.color=gradient;
        var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Blood droplets"};
        material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
        AssetDatabase.CreateAsset(material,"Assets/Interactions/Blood droplets.mat");
        var renderer=blood.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=sphere;renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        effect.blood=blood;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,EffectPath);Object.DestroyImmediate(root);return prefab.GetComponent<NeighbourDeathEffect>();
    }

    public static Material FleshFragmentMaterial()
    {
        const string path="Assets/Interactions/Flesh fragments.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
        material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimsonMassIdle/Materials/Crimson_Flesh.mat")){name="Flesh fragments"};
        material.SetColor("_BaseColor",new Color(.65f,.55f,.55f));
        material.SetTexture("_MetallicGlossMap",null);material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.55f);material.SetFloat("_BumpScale",.65f);
        material.SetFloat("_SpecularHighlights",1);material.SetFloat("_EnvironmentReflections",1);
        AssetDatabase.CreateAsset(material,path);return material;
    }

    public static Mesh FleshFragmentMesh(Mesh sphere)
    {
        const string path="Assets/Interactions/Flesh fragment sphere.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
        mesh=Object.Instantiate(sphere);mesh.name="Flesh fragment sphere";
        // Sample inside a large red flesh island. The atlas's black padding is for
        // the original monster UVs and must never wrap around the fragments.
        mesh.uv=mesh.uv.Select(uv=>new Vector2(.065f+uv.x*.16f,.125f+uv.y*.19f)).ToArray();
        mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
