using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
public static class InstallInteractionIcons
{
    static bool Inside(Vector2 p,Vector2[] polygon)
    {
        bool inside=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)if((polygon[i].y>p.y)!=(polygon[j].y>p.y)&&p.x<(polygon[j].x-polygon[i].x)*(p.y-polygon[i].y)/(polygon[j].y-polygon[i].y)+polygon[i].x)inside=!inside;return inside;
    }
    static Texture2D Make(string name,Vector2[] polygon,int size,bool dots=false)
    {
        var t=new Texture2D(size,size,TextureFormat.RGBA32,false);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            Color sum=Color.clear;
            for(int sy=0;sy<4;sy++)for(int sx=0;sx<4;sx++)
            {
                var p=new Vector2((x+(sx+.5f)/4)/size,(y+(sy+.5f)/4)/size);
                bool fill=Inside(p,polygon),edge=false;
                if(!fill)for(int d=0;d<16;d++){float a=d*Mathf.PI/8;if(Inside(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a)) *(dots?.7f/size:1.6f/24f),polygon)){edge=true;break;}}
                bool dot=dots&&(Vector2.Distance(p,new Vector2(.32f,.43f))<.045f||Vector2.Distance(p,new Vector2(.5f,.43f))<.045f||Vector2.Distance(p,new Vector2(.68f,.43f))<.045f);
                sum+=fill?(dot?new Color(.08f,.08f,.08f,1):Color.white):edge?new Color(.08f,.08f,.08f,1):Color.clear;
            }
            var value=sum/16;if(value.a>0){value.r/=value.a;value.g/=value.a;value.b/=value.a;}t.SetPixel(x,size-1-y,value);
        }
        t.Apply();string path="Assets/Interactions/Cursors/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.mipmapEnabled=true;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var arrow=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Interactions/Cursors/SourceArrow.png");
        var hand=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Interactions/Cursors/SourceHand.png");
        var bubble=Make("TalkBubble",new[]{new Vector2(.18f,.13f),new Vector2(.82f,.13f),new Vector2(.91f,.22f),new Vector2(.91f,.65f),new Vector2(.82f,.74f),new Vector2(.43f,.74f),new Vector2(.23f,.92f),new Vector2(.26f,.74f),new Vector2(.18f,.74f),new Vector2(.09f,.65f),new Vector2(.09f,.22f)},64,true);
        var hud=Object.FindAnyObjectByType<InteractionPrompt>();Undo.RecordObject(hud,"Set cursors");hud.defaultCursor=arrow;hud.linkCursor=hand;hud.defaultHotspot=new Vector2(2,2);hud.itemHotspot=new Vector2(8,1);EditorUtility.SetDirty(hud);
        var seller=GameObject.Find("salesman");var indicator=seller.GetComponent<TalkIndicator>();if(!indicator)indicator=Undo.AddComponent<TalkIndicator>(seller);indicator.icon=bubble;EditorUtility.SetDirty(indicator);
        EditorSceneManager.MarkSceneDirty(seller.scene);EditorSceneManager.SaveScene(seller.scene);AssetDatabase.SaveAssets();
    }
}



