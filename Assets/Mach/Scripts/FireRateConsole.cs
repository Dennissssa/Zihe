using UnityEngine;

namespace Mach
{
    public sealed class FireRateConsole : MonoBehaviour
    {
        public string[] rateLabels={"LOW","MED","HIGH"};
        public TextMesh selectedText;
        public TextMesh statusText;
        public int SelectedIndex { get; private set; }
        public int ConfirmedIndex { get; private set; }
        public bool HasPendingChange => SelectedIndex!=ConfirmedIndex;
        public event System.Action<int> RateConfirmed;
        public BehaviorProgram program;
        public void Cycle()
        {
            if(rateLabels==null || rateLabels.Length==0)return;
            SelectedIndex=(SelectedIndex+1)%rateLabels.Length;RefreshDisplay();
        }
        public void Confirm()
        {
            ConfirmedIndex=SelectedIndex;RefreshDisplay();RateConfirmed?.Invoke(ConfirmedIndex);
            if(program)program.Add(CircuitKind.Rate,ConfirmedIndex.ToString());
        }
        public void RefreshDisplay()
        {
            if(rateLabels==null || rateLabels.Length==0)return;
            SelectedIndex=Mathf.Clamp(SelectedIndex,0,rateLabels.Length-1);
            ConfirmedIndex=Mathf.Clamp(ConfirmedIndex,0,rateLabels.Length-1);
            if(selectedText)selectedText.text=rateLabels[SelectedIndex];
            if(statusText)statusText.text=HasPendingChange ? "PENDING / PRESS CONFIRM" : "CONFIRMED";
        }
        void Start() => RefreshDisplay();
    }
}
