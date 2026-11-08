using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Mach
{
    [Serializable]public class CircuitSlot
    {
        public RectTransform rect,input,output,data,parameter;public Text title;public Image background;
    }
    public sealed class CircuitBoardView : MonoBehaviour
    {
        public BehaviorProgram program;public Canvas canvas;public RectTransform surface,workspace,checkButton,deleteButton,unlinkButton;
        public Text status,help;public CircuitSlot[] slots;public CircuitWireGraphic wires;
        public RectTransform clearButton,resetViewButton;public Text boardInfo;
        int selected=-1,wireSource=-1,dragging=-1;bool wireData,wireInput,panning;Vector2 last;
        public event Action<bool,string> Checked;
        public bool CheckCircuit(){bool valid=program.Validate(out var message);GameAudio.Play(valid?SoundCue.Connect:SoundCue.Error,transform.position,true);program.Say(message);Checked?.Invoke(valid,message);return valid;}
        public int Selected=>selected;
        public bool DraggingBlock=>dragging>=0;
        public bool PendingWire=>program && program.Editing && program.Valid(wireSource);
        public bool PendingData=>wireData;
        public RectTransform PendingPort=>!PendingWire?null:wireData?(wireInput?slots[wireSource].data:slots[wireSource].parameter):(wireInput?slots[wireSource].input:slots[wireSource].output);
        public Vector2 PendingDirection=>wireData?(wireInput?Vector2.down:Vector2.up):(wireInput?Vector2.left:Vector2.right);
        public static Color BlockColor(CircuitKind kind)=>kind==CircuitKind.Point?new Color(.86f,.48f,.22f)
            :kind==CircuitKind.Duration?new Color(.90f,.55f,.29f)
            :BehaviorProgram.IsParameter(kind)?new Color(.76f,.36f,.10f)
            :kind==CircuitKind.Fire?new Color(.08f,.21f,.39f):new Color(.13f,.38f,.62f);
        public static readonly Color ExecutionPortColor=new Color(.28f,.65f,.92f);
        public static readonly Color ParameterPortColor=new Color(.96f,.53f,.18f);
        public void Refresh()
        {
            for(int i=0;i<slots.Length;i++)
            {
                var slot=slots[i];bool active=program.Valid(i);slot.rect.gameObject.SetActive(active);if(!active)continue;var node=program.nodes[i];slot.rect.anchoredPosition=node.position;
                string value=node.value;if(node.kind==CircuitKind.Rate&&int.TryParse(value,out int rate))value=rate==0?"LOW":rate==1?"MID":"HIGH";
                slot.title.text=node.kind.ToString().ToUpper()+(string.IsNullOrEmpty(value)?"":"\n"+value);
                if(node.kind==CircuitKind.Point)slot.title.text="POINT "+value+"\nTARGET DATA";
                Color c=BlockColor(node.kind);
                slot.background.color=i==selected?Color.Lerp(c,Color.white,.3f):c;
                slot.input.GetComponent<Image>().color=slot.output.GetComponent<Image>().color=ExecutionPortColor;
                slot.data.GetComponent<Image>().color=slot.parameter.GetComponent<Image>().color=ParameterPortColor;
                bool parameterOnly=BehaviorProgram.IsParameter(node.kind);
                slot.input.gameObject.SetActive(node.kind!=CircuitKind.Start&&!parameterOnly);slot.output.gameObject.SetActive(!parameterOnly);
                slot.data.gameObject.SetActive(parameterOnly);
                slot.parameter.gameObject.SetActive(node.kind==CircuitKind.Facing||node.kind==CircuitKind.DriveTo||node.kind==CircuitKind.Fire||node.kind==CircuitKind.Wait);
            }
            status.text=program.Status;wires.RefreshRoutes();
            if(boardInfo){int count=0;foreach(var n in program.nodes)if(n.active)count++;boardInfo.text=count+" / "+slots.Length+" BLOCKS   |   "+Mathf.RoundToInt(workspace.localScale.x*100)+"%";}
        }
        void OnEnable(){if(program)program.Changed+=Refresh;}
        void OnDisable(){if(program)program.Changed-=Refresh;wireSource=-1;dragging=-1;panning=false;}
        public bool ClickPort(int node,bool input,bool data)
        {
            if(!program.Valid(node))return false;
            dragging=-1;panning=false;
            if(wireSource<0||!program.Valid(wireSource))
            {GameAudio.Play(SoundCue.Button,transform.position,true);wireSource=node;wireInput=input;wireData=data;program.Say("Choose the other port.");return false;}
            if(wireSource==node&&wireInput==input&&wireData==data){GameAudio.Play(SoundCue.Cancel,transform.position,true);wireSource=-1;program.Say("Wire cancelled.");return false;}
            if(wireData!=data||wireInput==input){GameAudio.Play(SoundCue.Error,transform.position,true);program.Say("Connect an output to a matching input.");return false;}
            int source=wireInput?node:wireSource,target=wireInput?wireSource:node;
            if(!program.Connect(data?target:source,data?source:target,data)){GameAudio.Play(SoundCue.Error,transform.position,true);return false;}
            wireSource=-1;return true;
        }
        bool Hit(RectTransform rect,Vector2 mouse)=>rect&&rect.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(rect,mouse,canvas.worldCamera);
        bool HitPort(RectTransform rect,Vector2 mouse)
        {
            if(!rect||!rect.gameObject.activeInHierarchy)return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,mouse,canvas.worldCamera,out var p);
            var bounds=rect.rect;bounds.min-=Vector2.one*3;bounds.max+=Vector2.one*3;
            return bounds.Contains(p);
        }
        void Press(RectTransform rect){var button=rect.GetComponentInParent<TabletPhysicalButton>();if(button)button.Press();}
        public void ResetView(){workspace.localScale=Vector3.one;workspace.anchoredPosition=Vector2.zero;dragging=-1;panning=false;}
        public void ClearBoard(){if(program.Docked)return;selected=wireSource=dragging=-1;panning=false;ResetView();program.ClearCircuit();}
        public void ZoomAt(Vector2 screen,float wheel)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(surface,screen,canvas.worldCamera,out var anchor);
            float old=workspace.localScale.x;float next=Mathf.Clamp(old*Mathf.Exp(Mathf.Clamp(wheel,-1,1)*.14f),.4f,2.5f);
            workspace.anchoredPosition=anchor-(anchor-workspace.anchoredPosition)*(next/old);workspace.localScale=Vector3.one*next;
        }
        void LateUpdate()
        {
            Refresh();if(!program.Editing)return;var mouse=Mouse.current;var keyboard=Keyboard.current;if(mouse==null)return;var screen=mouse.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(workspace,screen,canvas.worldCamera,out var local);
            if(keyboard!=null&&keyboard.deleteKey.wasPressedThisFrame){program.Remove(selected);selected=-1;}
            if(mouse.rightButton.wasPressedThisFrame)
            {
                if(wireSource>=0){GameAudio.Play(SoundCue.Cancel,transform.position,true);wireSource=-1;program.Say("Wire cancelled.");}
                else if(Hit(surface,screen))wires.DeleteAt(screen);
            }
            if(mouse.scroll.ReadValue().y!=0&&Hit(surface,screen)){ZoomAt(screen,mouse.scroll.ReadValue().y);RectTransformUtility.ScreenPointToLocalPointInRectangle(workspace,screen,canvas.worldCamera,out local);dragging=-1;panning=false;}
            if(mouse.leftButton.wasPressedThisFrame)
            {
                if(Hit(checkButton,screen)){Press(checkButton);CheckCircuit();return;}
                if(Hit(deleteButton,screen)){Press(deleteButton);program.Remove(selected);selected=-1;return;}
                if(Hit(clearButton,screen)){Press(clearButton);ClearBoard();return;}
                if(Hit(resetViewButton,screen)){ResetView();return;}
                if(!Hit(surface,screen))return;
                for(int i=slots.Length-1;i>=0;i--)if(program.Valid(i))
                {
                    var slot=slots[i];
                    if(HitPort(slot.output,screen)){ClickPort(i,false,false);return;}
                    if(HitPort(slot.data,screen)){ClickPort(i,true,true);return;}
                    if(HitPort(slot.input,screen)){ClickPort(i,true,false);return;}
                    if(HitPort(slot.parameter,screen)){ClickPort(i,false,true);return;}
                }
                for(int i=slots.Length-1;i>=0;i--)if(program.Valid(i))
                {
                    var slot=slots[i];
                    if(Hit(slot.rect,screen)){selected=i;dragging=i;last=local;return;}
                }
                if(Hit(surface,screen)){panning=true;last=screen;}
            }
            if(mouse.leftButton.isPressed && dragging>=0){program.nodes[dragging].position+=local-last;last=local;}
            else if(mouse.leftButton.isPressed&&panning){RectTransformUtility.ScreenPointToLocalPointInRectangle(surface,screen,canvas.worldCamera,out var a);RectTransformUtility.ScreenPointToLocalPointInRectangle(surface,last,canvas.worldCamera,out var b);workspace.anchoredPosition+=a-b;last=screen;}
            if(mouse.leftButton.wasReleasedThisFrame){if(dragging>=0)program.Save();dragging=-1;panning=false;}
        }
    }
}




