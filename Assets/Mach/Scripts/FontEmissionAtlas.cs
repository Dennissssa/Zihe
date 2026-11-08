using UnityEngine;
namespace Mach
{
    [ExecuteAlways]
    public sealed class FontEmissionAtlas : MonoBehaviour
    {
        public Font font;public Material[] materials;
        void OnEnable(){Font.textureRebuilt+=Rebuilt;Sync();}
        void OnDisable()=>Font.textureRebuilt-=Rebuilt;
        void Rebuilt(Font changed){if(changed==font)Sync();}
        void LateUpdate()=>Sync();
        void Sync(){if(!font||materials==null)return;foreach(var material in materials)if(material&&material.mainTexture!=font.material.mainTexture)material.mainTexture=font.material.mainTexture;}
    }
}
