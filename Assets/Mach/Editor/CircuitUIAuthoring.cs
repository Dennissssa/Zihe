using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        [MenuItem("Mach/Polish Circuit UI")]
        public static void PolishCircuitUI()
        {
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var board=Object.FindAnyObjectByType<BehaviorProgram>().board;var root=board.transform;
            board.program.tablet.readingDistance=.48f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(board.program.tablet);
            circuitFont=ProjectTypography.Font;
            var heading=root.Find("Heading").GetComponent<Text>();heading.text="CIRCUIT / EDITOR";heading.fontSize=30;heading.alignment=TextAnchor.MiddleLeft;heading.rectTransform.anchoredPosition=new Vector2(-320,360);heading.rectTransform.sizeDelta=new Vector2(490,44);
            if(!board.boardInfo)board.boardInfo=UILabel(root,"Board count and zoom","",new Vector2(365,360),new Vector2(390,40),23,new Color(.48f,.77f,.86f));
            board.checkButton.anchoredPosition=new Vector2(-465,305);board.unlinkButton.anchoredPosition=new Vector2(-235,305);board.deleteButton.anchoredPosition=new Vector2(-5,305);
            foreach(var rect in new[]{board.checkButton,board.unlinkButton,board.deleteButton}){rect.sizeDelta=new Vector2(210,42);rect.GetComponentInChildren<Text>().rectTransform.sizeDelta=rect.sizeDelta;}
            if(!board.resetViewButton)board.resetViewButton=UIButton(root,"Reset view","RESET VIEW",new Vector2(225,305),new Vector2(210,42),new Color(.16f,.27f,.33f),21);
            if(!board.clearButton)board.clearButton=UIButton(root,"Clear entire circuit","CLEAR ALL",new Vector2(455,305),new Vector2(210,42),new Color(.53f,.20f,.17f),21);
            board.surface.sizeDelta=new Vector2(1155,540);board.surface.anchoredPosition=new Vector2(0,-5);
            if(!board.surface.Find("Graph dot grid")){var grid=UIRect(board.surface,"Graph dot grid",Vector2.zero,board.surface.sizeDelta);grid.gameObject.AddComponent<CircuitGridGraphic>().raycastTarget=false;grid.SetAsFirstSibling();}
            foreach(var slot in board.slots)
            {
                var outline=slot.rect.GetComponent<Outline>();if(!outline)outline=slot.rect.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.34f,.56f,.66f,.65f);outline.effectDistance=new Vector2(1,-1);
                slot.title.fontSize=22;
            }
            board.status.rectTransform.anchoredPosition=new Vector2(0,-310);board.status.fontSize=24;
            board.help.text="BLUE  EXEC > IN     /     GOLD  TARGET / AMMO / RATE > PARAM\nWheel: zoom at pointer   |   Drag empty space: pan   |   Tab: close";board.help.fontSize=20;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.gameObject.layer;
            board.Refresh();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            VerifyCircuitSystem();TabletAuthoring.VerifyAndBuild();
        }
    }
}

