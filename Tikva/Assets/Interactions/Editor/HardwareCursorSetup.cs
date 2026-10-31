using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
public static class HardwareCursorSetup
{
    public static Vector2 Tip(Texture2D t)
    {
        for(int y=t.height-1;y>=0;y--){float sum=0;int n=0;for(int x=0;x<t.width;x++){var c=t.GetPixel(x,y);if(c.a>.5f&&c.grayscale>.6f){sum+=x;n++;}}if(n>0)return new Vector2(Mathf.Round(sum/n),t.height-y-1);}
        return Vector2.zero;
    }
    static Texture2D Prepare(string name)
    {
        // Read the full PNG, independently of the source's import preview size.
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(File.ReadAllBytes("Assets/Interactions/Cursors/Source"+name+".png"));
        const int canvas=32,extent=22,margin=2,samples=16;
        float scale=(float)extent/Mathf.Max(source.width,source.height);
        int width=Mathf.RoundToInt(source.width*scale),height=Mathf.RoundToInt(source.height*scale);
        var result=new Texture2D(canvas,canvas,TextureFormat.RGBA32,false);result.SetPixels(new Color[canvas*canvas]);
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            Vector3 rgb=Vector3.zero;float alpha=0;
            for(int sy=0;sy<samples;sy++)for(int sx=0;sx<samples;sx++)
            {
                var c=source.GetPixelBilinear((x+(sx+.5f)/samples)/width,(y+(sy+.5f)/samples)/height);
                rgb+=new Vector3(c.r,c.g,c.b)*c.a;alpha+=c.a;
            }
            Color pixel=alpha>0?new Color(rgb.x/alpha,rgb.y/alpha,rgb.z/alpha,alpha/(samples*samples)):Color.clear;
            result.SetPixel(margin+x,canvas-margin-height+y,pixel);
        }
        result.Apply();string path="Assets/Interactions/Cursors/Native"+name+".png";File.WriteAllBytes(path,result.EncodeToPNG());Object.DestroyImmediate(source);Object.DestroyImmediate(result);
        AssetDatabase.ImportAsset(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Cursor;i.maxTextureSize=32;i.npotScale=TextureImporterNPOTScale.None;i.isReadable=true;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=true;i.filterMode=FilterMode.Bilinear;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var hud=Object.FindAnyObjectByType<InteractionPrompt>();hud.defaultCursor=Prepare("Arrow");hud.linkCursor=Prepare("Hand");
        hud.defaultHotspot=Tip(hud.defaultCursor);hud.itemHotspot=Tip(hud.linkCursor);
        EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);
    }
}
