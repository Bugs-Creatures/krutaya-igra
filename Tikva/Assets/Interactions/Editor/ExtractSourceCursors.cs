using UnityEngine;
using UnityEditor;
using System.IO;
public static class ExtractSourceCursors
{
    static void Import(string path)
    {
        AssetDatabase.ImportAsset(path);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Default;i.npotScale=TextureImporterNPOTScale.None;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.Uncompressed;i.isReadable=true;i.alphaIsTransparency=true;i.mipmapEnabled=true;i.filterMode=FilterMode.Trilinear;i.SaveAndReimport();
    }
    static void Extract(Texture2D source,int start,int end,string name)
    {
        int minX=end,minY=source.height,maxX=0,maxY=0;
        for(int y=0;y<source.height;y++)for(int x=start;x<end;x++)if(source.GetPixel(x,y).a>.01f){minX=Mathf.Min(x,minX);minY=Mathf.Min(y,minY);maxX=Mathf.Max(x,maxX);maxY=Mathf.Max(y,maxY);}
        minX=Mathf.Max(start,minX-4);minY=Mathf.Max(0,minY-4);maxX=Mathf.Min(end-1,maxX+4);maxY=Mathf.Min(source.height-1,maxY+4);
        var output=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
        for(int y=0;y<output.height;y++)for(int x=0;x<output.width;x++)
        {
            Color c=source.GetPixel(minX+x,minY+y);
            // Copy original RGBA pixels, including black outline and antialiased alpha.
            output.SetPixel(x,y,c);
        }
        output.Apply();string path="Assets/Interactions/Cursors/"+name+".png";File.WriteAllBytes(path,output.EncodeToPNG());Object.DestroyImmediate(output);Import(path);
    }
    public static void Apply()
    {
        string path="Assets/Interactions/Cursors/CursorSource.png";Import(path);var source=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Extract(source,0,source.width/2,"SourceArrow");Extract(source,source.width/2,source.width,"SourceHand");
        InstallInteractionIcons.Apply();
        HardwareCursorSetup.Apply();
    }
    static Vector2 FindTip(Texture2D texture)
    {
        for(int y=texture.height-1;y>=0;y--)
        {
            float sum=0;int count=0;
            for(int x=0;x<texture.width;x++){var c=texture.GetPixel(x,y);if(c.a>.5f&&c.grayscale>.6f){sum+=x;count++;}}
            if(count>0)return new Vector2((sum/count+.5f)/texture.width,(texture.height-y-.5f)/texture.height)*24;
        }
        return Vector2.zero;
    }
}
