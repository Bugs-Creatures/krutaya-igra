using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
public static class StyleSuspiciousReference
{
    static TutorialCardView view;static Color rule=new Color(.35f,.35f,.33f,.8f);
    static void Place(RectTransform r,Vector2 size,Vector2 pos)
    {r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=pos;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
    static RectTransform Line(string name,Transform parent,Vector2 size,Vector2 pos,Color color)
    {
        var old=parent.Find(name);var r=old?(RectTransform)old:GameHudUI.Rect(name,parent,size,pos);Place(r,size,pos);
        var image=r.GetComponent<UnityEngine.UI.Image>();if(!image)image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return r;
    }
    static Sprite OutlineSprite()
    {
        string path="Assets/Interactions/UI/ThinRoundedOutline.png";
        if(!File.Exists(path))
        {
            const int size=64,radius=16;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var q=new Vector2(Mathf.Abs(x+.5f-size*.5f),Mathf.Abs(y+.5f-size*.5f))-Vector2.one*(size*.5f-radius);
                float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)-Mathf.Clamp01(.5f-d-8)));
            }
            texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        }
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spriteBorder=Vector4.one*16;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Border(string name,RectTransform parent,Sprite sprite)
    {
        var border=Line(name,parent,parent.sizeDelta,Vector2.zero,new Color(.6f,.6f,.56f,.8f));
        var image=border.GetComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=8;border.SetAsFirstSibling();
    }
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var hud=Object.FindAnyObjectByType<PlayerTutorialHud>();view=hud.suspiciousCard;var root=(RectTransform)view.transform;
        Undo.RegisterFullObjectHierarchyUndo(view.gameObject,"Style Suspicious tutorial");
        Place(root,new Vector2(900,460),Vector2.zero);var bg=root.GetComponent<UnityEngine.UI.Image>();bg.color=new Color(.035f,.035f,.032f,1);bg.pixelsPerUnitMultiplier=8;
        var outline=OutlineSprite();Border("Reference border",root,outline);
        var header=root.Find("Reference header");TextMeshProUGUI headerText;
        if(!header)headerText=GameHudUI.Text("Reference header",root,view.body.font,"ПАМЯТКА",14,new Color(.6f,.6f,.57f),new Vector2(150,30),new Vector2(-345,195));else headerText=header.GetComponent<TextMeshProUGUI>();
        headerText.alignment=TextAlignmentOptions.MidlineLeft;headerText.characterSpacing=4;
        Place(view.counter.rectTransform,new Vector2(65,30),new Vector2(340,195));view.counter.color=new Color(.7f,.7f,.66f);view.counter.fontSize=15;view.counter.text="1/2";
        Place(view.closeButton,new Vector2(38,38),new Vector2(411,195));view.closeButton.GetComponent<TextMeshProUGUI>().fontSize=30;
        Line("Reference close divider",root,new Vector2(1,22),new Vector2(385,195),rule);
        Line("Reference header rule",root,new Vector2(840,1),new Vector2(0,172),rule);
        Line("Reference column rule",root,new Vector2(1,300),new Vector2(-175,0),rule);
        Line("Reference footer rule",root,new Vector2(840,1),new Vector2(0,-166),rule);
        Line("Reference red square",root,new Vector2(14,14),new Vector2(-140,105),new Color(.65f,.16f,.15f));
        Place(view.title.rectTransform,new Vector2(515,70),new Vector2(150,105));view.title.fontSize=24;view.title.fontStyle=FontStyles.Bold;view.title.alignment=TextAlignmentOptions.MidlineLeft;view.title.text="ПОДОЗРИТЕЛЬНЫЕ ПРЕДМЕТЫ";view.title.color=new Color(.91f,.91f,.87f);
        Place(view.body.rectTransform,new Vector2(500,180),new Vector2(155,-30));view.body.fontSize=21;view.body.alignment=TextAlignmentOptions.TopLeft;view.body.lineSpacing=7;view.body.color=new Color(.88f,.88f,.85f);
        view.body.text="<color=#B44943>Подозрительные</color> предметы можно подбирать и носить только вне камер. Если с таким предметом всё-таки попасть на камеру, то персонаж его принудительно уронит.";
        var frame=(RectTransform)view.placeholder.transform.parent;Place(frame,new Vector2(220,270),new Vector2(-300,0));frame.GetComponent<UnityEngine.UI.Image>().color=new Color(.065f,.065f,.06f,1);
        var bounds=(RectTransform)view.picture.transform.parent;Place(bounds,new Vector2(210,260),Vector2.zero);view.picture.texture=null;view.picture.enabled=false;hud.suspiciousImage=null;
        Place(view.placeholder.rectTransform,new Vector2(210,35),Vector2.zero);view.placeholder.fontSize=13;view.placeholder.characterSpacing=2;view.placeholder.color=new Color(.4f,.4f,.38f);view.placeholder.gameObject.SetActive(true);
        Place(view.actionButton,new Vector2(160,36),new Vector2(337,-202));var button=view.actionButton.GetComponent<UnityEngine.UI.Image>();button.color=new Color(.04f,.04f,.036f,1);button.pixelsPerUnitMultiplier=8;Border("Reference button border",view.actionButton,outline);
        Place(view.actionText.rectTransform,new Vector2(154,34),Vector2.zero);view.actionText.fontSize=18;view.actionText.fontStyle=FontStyles.Normal;view.actionText.color=new Color(.9f,.9f,.86f);
        foreach(var component in root.GetComponentsInChildren<Component>(true))if(component)EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);AssetDatabase.SaveAssets();
    }
}

