using UnityEngine;
using TMPro;

// Shared runtime uGUI building blocks; all dimensions are in the 1280 x 720 reference canvas.
public static class GameHudUI
{
    static Sprite roundedSprite;
    static Sprite RoundedSprite()
    {
        if(roundedSprite)return roundedSprite;
        const int size=64,radius=16;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Rounded UI",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            Vector2 q=new Vector2(Mathf.Abs(x+.5f-size*.5f),Mathf.Abs(y+.5f-size*.5f))-Vector2.one*(size*.5f-radius);
            float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
            texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-distance)));
        }
        texture.Apply();roundedSprite=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));roundedSprite.hideFlags=HideFlags.HideAndDontSave;return roundedSprite;
    }
    public static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    public static UnityEngine.UI.Image Image(RectTransform r,Color color,bool rounded=false)
    {
        var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;
        if(rounded){image.sprite=RoundedSprite();image.type=UnityEngine.UI.Image.Type.Sliced;}
        return image;
    }
    public static TextMeshProUGUI Text(string name,Transform parent,TMP_FontAsset font,string value,float size,Color color,Vector2 dimensions,Vector2 position)
    {
        var r=Rect(name,parent,dimensions,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;return t;
    }
    public static bool Hit(RectTransform r,Vector2 p)=>r&&RectTransformUtility.RectangleContainsScreenPoint(r,p);
    public static Canvas Canvas(string name,int order)
    {
        var c=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler)).GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=order;
        var s=c.GetComponent<UnityEngine.UI.CanvasScaler>();s.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1280,720);s.matchWidthOrHeight=.5f;return c;
    }
}
