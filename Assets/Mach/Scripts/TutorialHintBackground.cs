using UnityEngine;
using UnityEngine.UI;
namespace Mach
{
    [ExecuteAlways,DefaultExecutionOrder(32700)]
    public sealed class TutorialHintBackground : MonoBehaviour
    {
        public Text content;
        public Image background;
        public float padding=20;
        void LateUpdate()
        {
            if(!content||!background)return;
            background.enabled=content.gameObject.activeSelf;
            var rect=(RectTransform)transform;
            content.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,content.preferredHeight+4);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,content.preferredHeight+padding*2+4);
        }
    }
}
