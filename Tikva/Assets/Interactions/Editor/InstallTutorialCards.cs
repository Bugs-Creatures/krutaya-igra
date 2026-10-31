using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using System.Linq;
public static class InstallTutorialCards
{
    static TMP_FontAsset font;static Sprite rounded;
    static RectTransform R(string n,Transform p,Vector2 s,Vector2 a)=>GameHudUI.Rect(n,p,s,a);
    static TextMeshProUGUI T(string n,Transform p,string value,float size,Color color,Vector2 s,Vector2 a)=>GameHudUI.Text(n,p,font,value,size,color,s,a);
    static void Background(RectTransform r,Color c)
    {var image=GameHudUI.Image(r,c);image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;}
    static TutorialCardView Make(string name,Transform parent,string heading,string content)
    {
        var card=R(name,parent,new Vector2(800,590),Vector2.zero);Background(card,new Color(.07f,.07f,.07f,1));
        var view=card.gameObject.AddComponent<TutorialCardView>();
        view.title=T("Title",card,heading,22,Color.white,new Vector2(580,36),new Vector2(0,250));view.title.fontStyle=FontStyles.Bold;
        view.counter=T("Page counter",card,"1/1",16,new Color(.7f,.7f,.7f),new Vector2(90,35),new Vector2(-325,250));
        view.closeButton=T("Close tutorials",card,"×",32,Color.white,new Vector2(45,45),new Vector2(360,250)).rectTransform;
        var frame=R("Picture frame",card,new Vector2(736,250),new Vector2(0,78));Background(frame,new Color(.16f,.16f,.16f));
        var bounds=R("Picture bounds",frame,new Vector2(720,234),Vector2.zero);
        view.picture=R("Tutorial image",bounds,new Vector2(720,234),Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();view.picture.raycastTarget=false;
        view.picture.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        view.picture.enabled=false;
        view.placeholder=T("Picture placeholder",frame,"ИЛЛЮСТРАЦИЯ",19,new Color(.6f,.6f,.6f),new Vector2(650,45),Vector2.zero);view.placeholder.characterSpacing=5;
        view.body=T("Tutorial text",card,content,23,new Color(.95f,.95f,.95f),new Vector2(730,155),new Vector2(0,-140));
        view.actionButton=R("Continue",card,new Vector2(210,46),new Vector2(0,-250));Background(view.actionButton,new Color(.32f,.32f,.32f));
        view.actionText=T("Continue label",view.actionButton,"Понятно",20,Color.white,new Vector2(200,45),Vector2.zero);
        card.gameObject.SetActive(false);return view;
    }
    static Sprite RoundedAsset()
    {
        string folder="Assets/Interactions/UI";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Interactions","UI");
        string path=folder+"/RoundedPanel.png";
        if(!File.Exists(path))
        {
            var temp=new GameObject("Rounded shape source",typeof(RectTransform));var image=GameHudUI.Image(temp.GetComponent<RectTransform>(),Color.white,true);
            File.WriteAllBytes(path,image.sprite.texture.EncodeToPNG());Object.DestroyImmediate(temp);AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=Vector4.one*16;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var hud=Object.FindAnyObjectByType<PlayerTutorialHud>();font=Object.FindAnyObjectByType<InteractionPrompt>().font;rounded=RoundedAsset();
        var root=GameObject.Find("Tutorial Cards Canvas");
        if(!root)
        {
            root=GameHudUI.Canvas("Tutorial Cards Canvas",250).gameObject;
            var overlay=R("Tutorial overlay",root.transform,Vector2.zero,Vector2.zero);overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;GameHudUI.Image(overlay,new Color(0,0,0,.65f));
            hud.tutorialOverlay=overlay.gameObject.AddComponent<CanvasGroup>();hud.tutorialOverlay.blocksRaycasts=false;
            hud.mapCard=Make("Map",overlay,"КАРТА","Чтобы открыть карту нажмите \"M\".");
            hud.suspiciousCard=Make("Suspicious",overlay,"ПОДОЗРИТЕЛЬНЫЕ ПРЕДМЕТЫ","<color=#FF5B5B>Подозрительные</color> предметы можно подбирать и носить только вне камер. Если с таким предметом всё-таки попасть на камеру, то персонаж его принудительно уронит.");
            hud.droppedCard=Make("DroppedItems",overlay,"ПОИСК ПРЕДМЕТОВ","Чтобы подсветить предметы, которые вы уронили, удерживайте \"ПКМ\".");
            overlay.gameObject.SetActive(false);
        }
        else
        {
            hud.tutorialOverlay=root.GetComponentInChildren<CanvasGroup>(true);
            var cards=root.GetComponentsInChildren<TutorialCardView>(true);hud.mapCard=cards.First(c=>c.name=="Map");hud.suspiciousCard=cards.First(c=>c.name=="Suspicious");hud.droppedCard=cards.First(c=>c.name=="DroppedItems");
        }
        EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
    }
}

