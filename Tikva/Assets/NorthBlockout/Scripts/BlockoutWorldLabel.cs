using UnityEngine;

namespace NorthBlockout
{
    [ExecuteAlways, RequireComponent(typeof(TextMesh))]
    public sealed class BlockoutWorldLabel : MonoBehaviour
    {
        Material instance;
        void OnEnable()
        {
            var shader=Shader.Find("NorthBlockout/WorldText");
            if(!shader)return;
            instance=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            GetComponent<MeshRenderer>().sharedMaterial=instance;
            Font.textureRebuilt+=Rebuilt;
            Rebuilt(GetComponent<TextMesh>().font);
        }
        void Rebuilt(Font font)
        {
            if(instance && font==GetComponent<TextMesh>().font)instance.mainTexture=font.material.mainTexture;
        }
        void OnDisable()
        {
            Font.textureRebuilt-=Rebuilt;
            if(instance) { if(Application.isPlaying)Destroy(instance);else DestroyImmediate(instance); }
        }
    }
}
