using UnityEngine;

[ExecuteAlways]
public sealed class SurveillanceMonitor : MonoBehaviour
{
    [Tooltip("Drag a 16:9 screenshot here. Every copied monitor has its own image.")]
    public Texture screenImage;
    public MeshRenderer screenRenderer;
    public GameObject placeholderLabel;
    public Light screenLight;
    [ColorUsage(false,true)] public Color screenTint=new Color(.86f,.92f,1);
    [Min(0)] public float emission=1.5f;
    [Min(0)] public float lightIntensity=5.5f;
    MaterialPropertyBlock properties;
    public void Refresh()
    {
        if(screenRenderer)
        {
            properties??=new MaterialPropertyBlock();screenRenderer.GetPropertyBlock(properties);
            properties.SetTexture("_BaseMap",screenImage?screenImage:Texture2D.whiteTexture);
            properties.SetFloat("_HasImage",screenImage?1:0);properties.SetColor("_BaseColor",screenTint);
            properties.SetFloat("_EmissionStrength",emission);screenRenderer.SetPropertyBlock(properties);
        }
        if(placeholderLabel)placeholderLabel.SetActive(!screenImage);
        if(screenLight){screenLight.color=screenTint;screenLight.intensity=lightIntensity;}
    }
    void OnEnable()=>Refresh();
    void OnValidate()=>Refresh();
}
