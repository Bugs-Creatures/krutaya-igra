using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public static class InstallEscapeEnding
{
    const string EffectPath="Assets/Interactions/TreeBreakEffect.prefab";
    const string EndingPath="Assets/Scenes/Ending02.unity";
    [MenuItem("Tools/Interactions/Configure tree escape ending")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        var tree=GameObject.Find("breakable tree");
        if(!tree)throw new System.InvalidOperationException("breakable tree is missing.");
        var roots=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Tree"||t.name.StartsWith("Tree (")||t==tree.transform).ToArray();
        foreach(var root in roots)foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if(!filter.sharedMesh||filter.sharedMesh.vertexCount==0||filter.GetComponent<Collider>())continue;
            var collider=Undo.AddComponent<MeshCollider>(filter.gameObject);collider.sharedMesh=filter.sharedMesh;
        }
        foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name=="Po2"||f.name.StartsWith("Po2 (")))
        {
            if(!filter.sharedMesh||filter.sharedMesh.vertexCount==0||filter.GetComponent<Collider>())continue;
            var collider=Undo.AddComponent<BoxCollider>(filter.gameObject);collider.center=filter.sharedMesh.bounds.center;collider.size=filter.sharedMesh.bounds.size;
        }
        var target=tree.GetComponent<InteractionObject>();if(!target)target=Undo.AddComponent<InteractionObject>(tree);
        target.kind=InteractionKind.ToolAction;target.displayName="Дерево";target.availableHoverText="разрубить";target.reachDistance=2.5f;target.hoverTextSize=14;target.suspicious=false;
        var breakable=tree.GetComponent<BreakableTree>();if(!breakable)breakable=Undo.AddComponent<BreakableTree>(tree);
        var effect=AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath);
        if(!effect)effect=MakeEffect(tree);
        breakable.effectPrefab=effect.GetComponent<TreeBreakEffect>();
        var action=tree.GetComponent<ToolActionTarget>();if(!action)action=Undo.AddComponent<ToolActionTarget>(tree);
        action.action=ToolActionKind.ChopTree;action.breakableTree=breakable;action.missingToolText="не перелезть";
        var ending=Object.FindFirstObjectByType<EndingTransition>();Undo.RecordObject(ending,"Earlier death fade");ending.effectHold=.15f;
        var gate=GameObject.Find("Escape ending trigger — behind tree");
        if(!gate){gate=new GameObject("Escape ending trigger — behind tree");Undo.RegisterCreatedObjectUndo(gate,"Add escape trigger");}
        gate.layer=2;gate.transform.SetPositionAndRotation(new Vector3(51.4f,7.0f,-5.14f),Quaternion.identity);
        var box=gate.GetComponent<BoxCollider>();if(!box)box=Undo.AddComponent<BoxCollider>(gate);
        box.isTrigger=true;box.size=new Vector3(1.2f,3.5f,2.05f);
        var rb=gate.GetComponent<Rigidbody>();if(!rb)rb=Undo.AddComponent<Rigidbody>(gate);rb.isKinematic=true;rb.useGravity=false;
        var exit=gate.GetComponent<EscapeEndingTrigger>();if(!exit)exit=Undo.AddComponent<EscapeEndingTrigger>(gate);
        exit.tree=breakable;exit.ending=ending;exit.endingScene="Ending02";
        foreach(var o in new Object[]{target,breakable,action,ending,box,rb,exit})EditorUtility.SetDirty(o);
        var scene=tree.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(EndingPath))
        {
            var other=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(other);
            var camera=new GameObject("Main Camera");camera.tag="MainCamera";
            var view=camera.AddComponent<Camera>();view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.black;view.cullingMask=0;camera.AddComponent<AudioListener>();
            var canvasObject=new GameObject("Escape Ending Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var background=new GameObject("Black Background",typeof(RectTransform),typeof(UnityEngine.UI.Image));background.transform.SetParent(canvasObject.transform,false);
            var rect=(RectTransform)background.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=background.GetComponent<UnityEngine.UI.Image>();image.color=Color.black;image.raycastTarget=false;
            EditorSceneManager.SaveScene(other,EndingPath);EditorSceneManager.CloseScene(other,true);SceneManager.SetActiveScene(scene);
        }
        var scenes=EditorBuildSettings.scenes.ToList();var entry=scenes.FirstOrDefault(s=>s.path==EndingPath);
        if(entry==null)scenes.Add(new EditorBuildSettingsScene(EndingPath,true));else entry.enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();Selection.activeGameObject=tree;
    }

    static GameObject MakeEffect(GameObject tree)
    {
        var source=tree.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh&&f.sharedMesh.vertexCount>0);
        var root=new GameObject("TreeBreakEffect");var effect=root.AddComponent<TreeBreakEffect>();var bodies=new List<Rigidbody>();
        for(int n=0;n<2;n++)
        {
            Vector3 center;var mesh=HalfMesh(source,tree.transform,n,out center);AssetDatabase.CreateAsset(mesh,$"Assets/Interactions/Tree half {n+1}.asset");
            var half=new GameObject("Tree half "+(n+1));half.layer=2;half.transform.SetParent(root.transform,false);half.transform.localPosition=center;
            half.AddComponent<MeshFilter>().sharedMesh=mesh;half.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;
            // Brief visual debris does not block the newly opened exit.
            var body=half.AddComponent<Rigidbody>();body.isKinematic=true;body.mass=10;bodies.Add(body);
        }
        effect.halves=bodies.ToArray();
        var chipsObject=new GameObject("Wood chips");chipsObject.transform.SetParent(root.transform,false);
        var blocker=tree.GetComponentInChildren<BoxCollider>();chipsObject.transform.localPosition=tree.transform.InverseTransformPoint(blocker.bounds.center-Vector3.up*.3f);
        var particles=chipsObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.playOnAwake=false;main.loop=false;main.duration=.2f;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.9f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.4f,3);
        main.startSize3D=true;main.startSizeX=new ParticleSystem.MinMaxCurve(.015f,.04f);main.startSizeY=new ParticleSystem.MinMaxCurve(.01f,.03f);main.startSizeZ=new ParticleSystem.MinMaxCurve(.05f,.11f);
        main.startRotation3D=true;main.startRotationX=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationY=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationZ=new ParticleSystem.MinMaxCurve(0,6.28f);
        main.gravityModifier=1;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=32;
        var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,24)});
        var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Wood chips"};material.SetColor("_BaseColor",new Color(.3f,.18f,.08f));material.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(material,"Assets/Interactions/Wood chips.mat");
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=cube.GetComponent<MeshFilter>().sharedMesh;renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;Object.DestroyImmediate(cube);
        effect.chips=particles;var prefab=PrefabUtility.SaveAsPrefabAsset(root,EffectPath);Object.DestroyImmediate(root);return prefab;
    }

    static Mesh HalfMesh(MeshFilter filter,Transform root,int side,out Vector3 center)
    {
        var source=filter.sharedMesh;var positions=source.vertices.Select(v=>root.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();
        var normals=source.normals.Select(v=>root.InverseTransformDirection(filter.transform.TransformDirection(v)).normalized).ToArray();var uv=source.uv;
        var lo=positions.Aggregate(Vector3.Min);var hi=positions.Aggregate(Vector3.Max);var size=hi-lo;int axis=size.x>size.y?(size.x>size.z?0:2):(size.y>size.z?1:2);float split=(lo[axis]+hi[axis])*.5f;
        var map=new Dictionary<int,int>();var vertices=new List<Vector3>();var ns=new List<Vector3>();var uvs=new List<Vector2>();var submeshes=new List<int[]>();
        int Add(int index)
        {
            if(map.TryGetValue(index,out int found))return found;
            int next=vertices.Count;map.Add(index,next);vertices.Add(positions[index]);ns.Add(normals[index]);uvs.Add(uv[index]);return next;
        }
        for(int s=0;s<source.subMeshCount;s++)
        {
            var original=source.GetTriangles(s);var triangles=new List<int>();
            for(int i=0;i<original.Length;i+=3)
            {
                float middle=(positions[original[i]][axis]+positions[original[i+1]][axis]+positions[original[i+2]][axis])/3;
                if((middle<split?0:1)!=side)continue;
                triangles.Add(Add(original[i]));triangles.Add(Add(original[i+1]));triangles.Add(Add(original[i+2]));
            }
            submeshes.Add(triangles.ToArray());
        }
        center=(vertices.Aggregate(Vector3.Min)+vertices.Aggregate(Vector3.Max))*.5f;
        var pivot=center;
        var mesh=new Mesh{name="Tree broken half "+(side+1)};mesh.SetVertices(vertices.Select(v=>v-pivot).ToList());mesh.SetNormals(ns);mesh.SetUVs(0,uvs);mesh.subMeshCount=submeshes.Count;
        for(int s=0;s<submeshes.Count;s++)mesh.SetTriangles(submeshes[s],s);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
}
