using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class InstallDiggableMound
{
    [MenuItem("Tools/Interactions/Install bakery soil mound")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        if(GameObject.Find("Земля над ямой"))throw new System.InvalidOperationException("The mound already exists.");
        var shovel=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(i=>i.displayName=="Лопата");
        var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/TerrainLayers/Pebbles_A_TerrainLayer.terrainlayer");
        var terrain=Terrain.activeTerrain;
        Vector3 center=new Vector3(19.73f,0,-29.5f);
        center.y=terrain.SampleHeight(center+Vector3.right*1.5f)+terrain.transform.position.y;
        const int segments=40,rings=8;
        var vertices=new System.Collections.Generic.List<Vector3>{new Vector3(0,.36f,0)};
        var uv=new System.Collections.Generic.List<Vector2>{UV(center,layer)};
        var triangles=new System.Collections.Generic.List<int>();
        for(int ring=1;ring<=rings;ring++)
        {
            float r=(float)ring/rings;
            for(int j=0;j<segments;j++)
            {
                float angle=j*Mathf.PI*2/segments;
                float edge=1+.025f*Mathf.Sin(angle*3)+.015f*Mathf.Cos(angle*7);
                float x=Mathf.Cos(angle)*1.12f*r*edge,z=Mathf.Sin(angle)*1.23f*r*edge;
                float y=.39f*Mathf.Pow(1-r*r,1.5f)-.03f;
                y+=(Mathf.PerlinNoise(x*3.5f+14,z*3.5f+19)-.5f)*.13f*r*(1-r);
                vertices.Add(new Vector3(x,y,z));uv.Add(UV(center+new Vector3(x,0,z),layer));
            }
        }
        for(int j=0;j<segments;j++)
        {
            int next=(j+1)%segments;
            triangles.AddRange(new[]{0,1+next,1+j});
            for(int ring=1;ring<rings;ring++)
            {
                int a=1+(ring-1)*segments+j,b=1+(ring-1)*segments+next,c=1+ring*segments+j,d=1+ring*segments+next;
                triangles.AddRange(new[]{a,b,c,b,d,c});
            }
        }
        int bottom=vertices.Count;vertices.Add(new Vector3(0,-.16f,0));uv.Add(UV(center,layer));
        for(int j=0;j<segments;j++)triangles.AddRange(new[]{bottom,1+(rings-1)*segments+j,1+(rings-1)*segments+(j+1)%segments});
        var mesh=new Mesh{name="Loose soil mound"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,"Assets/Interactions/Bakery soil mound.asset");
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Loose soil - Pebbles A"};
        material.SetTexture("_BaseMap",layer.diffuseTexture);material.SetColor("_BaseColor",new Color(.86f,.80f,.70f));
        material.SetTexture("_BumpMap",layer.normalMapTexture);material.SetFloat("_BumpScale",layer.normalScale);material.EnableKeyword("_NORMALMAP");material.SetFloat("_Smoothness",.08f);
        AssetDatabase.CreateAsset(material,"Assets/Interactions/Bakery soil mound.mat");
        var mound=new GameObject("Земля над ямой");Undo.RegisterCreatedObjectUndo(mound,"Add diggable soil mound");
        mound.transform.position=center;
        mound.AddComponent<MeshFilter>().sharedMesh=mesh;mound.AddComponent<MeshRenderer>().sharedMaterial=material;
        mound.AddComponent<MeshCollider>().sharedMesh=mesh;
        var target=mound.AddComponent<InteractionObject>();target.kind=InteractionKind.ToolAction;target.displayName="Земля";target.availableHoverText="раскопать";target.reachDistance=3;target.hoverTextSize=14;
        var action=mound.AddComponent<ToolActionTarget>();action.action=ToolActionKind.DigSoil;action.requiredTool=shovel;action.missingToolText="здесь что-то есть";
        Selection.activeGameObject=mound;EditorSceneManager.MarkSceneDirty(mound.scene);EditorSceneManager.SaveScene(mound.scene);AssetDatabase.SaveAssets();
    }

    static Vector2 UV(Vector3 p,TerrainLayer layer)=>new Vector2((p.x+layer.tileOffset.x)/layer.tileSize.x,(p.z+layer.tileOffset.y)/layer.tileSize.y);
}
