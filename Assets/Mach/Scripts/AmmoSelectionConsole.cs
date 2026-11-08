using UnityEngine;

namespace Mach
{
    public sealed class AmmoSelectionConsole : MonoBehaviour
    {
        public string[] ammunition={"JHP","AP","BURST","ACID","THUNDER"};
        public TextMesh selectedText, statusText, indexText;
        public TextMesh[] optionLabels;
        public Color inactiveDisplayColor=new Color(.22f,.32f,.35f);
        public Color displayColor=new Color(.35f,.78f,.85f);
        public int PreviewIndex { get; private set; }
        public int SelectedIndex { get; private set; }
        public string SelectedAmmo => ammunition!=null && ammunition.Length>0 ? ammunition[SelectedIndex] : "";
        public bool HasPendingChange => PreviewIndex!=SelectedIndex;
        public event System.Action<string> AmmoSelected;
        public BehaviorProgram program;
        float confirmationPulse;
        public void Browse(int direction)
        {
            if(ammunition==null || ammunition.Length==0)return;
            PreviewIndex=(PreviewIndex+direction%ammunition.Length+ammunition.Length)%ammunition.Length;
            confirmationPulse=0;RefreshDisplay();
        }
        public void Select()
        {
            if(ammunition==null || ammunition.Length==0)return;
            SelectedIndex=PreviewIndex;confirmationPulse=.45f;RefreshDisplay();AmmoSelected?.Invoke(SelectedAmmo);
            if(program)program.Add(CircuitKind.Ammo,SelectedAmmo);
        }
        public void RefreshDisplay()
        {
            if(ammunition==null || ammunition.Length==0)return;
            PreviewIndex=Mathf.Clamp(PreviewIndex,0,ammunition.Length-1);SelectedIndex=Mathf.Clamp(SelectedIndex,0,ammunition.Length-1);
            if(selectedText)selectedText.text=ammunition[PreviewIndex];
            if(indexText)indexText.text=(PreviewIndex+1).ToString("D2")+" / "+ammunition.Length.ToString("D2");
            if(statusText)statusText.text=HasPendingChange ? "PREVIEW  /  SELECT TO CONFIRM" : "SELECTED  /  "+SelectedAmmo;
            if(optionLabels!=null)for(int i=0;i<optionLabels.Length;i++)if(optionLabels[i])optionLabels[i].color=i==PreviewIndex?displayColor:inactiveDisplayColor;
        }
        void Start()=>RefreshDisplay();
        void Update()
        {
            confirmationPulse=Mathf.Max(0,confirmationPulse-Time.deltaTime);
            if(selectedText)selectedText.color=Color.Lerp(displayColor,Color.white,confirmationPulse/.45f);
        }
    }
}

