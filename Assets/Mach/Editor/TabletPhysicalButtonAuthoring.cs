using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        [MenuItem("Mach/Author Physical Tablet Buttons")]
        public static void AuthorPhysicalTabletButtons()
        {
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var program=Object.FindAnyObjectByType<BehaviorProgram>();var board=program.board;var model=program.tabletVisual;
            meshFolder=CircuitFolder+"/Meshes";meshPrefix="TabletButton_";meshIndex=0;
            string[] names={"CHECK","UNLINK","CLEAR","CLEAR ALL"};
            RectTransform[] hits={board.checkButton,board.unlinkButton,board.deleteButton,board.clearButton};
            for(int i=0;i<4;i++)
            {
                if(hits[i].GetComponentInParent<TabletPhysicalButton>())continue;
                Vector3 position=new Vector3(i<2?-.287f:.287f,i%2==0?.075f:-.035f,-.045f);
                var cap=i==0?model.Find("Red power button"):new GameObject(names[i]+" physical key").transform;if(i!=0)cap.SetParent(model,false);cap.localPosition=position;cap.gameObject.AddComponent<TabletPhysicalButton>();
                if(i==0)
                {
                    cap.localScale=new Vector3(.046f/.022f,.056f/.047f,1);PrefabUtility.RecordPrefabInstancePropertyModifications(cap);
                    var socket=model.Find("Power button socket");socket.localScale=new Vector3(.054f/.032f,.07f/.061f,1);PrefabUtility.RecordPrefabInstancePropertyModifications(socket);
                }
                else
                {
                    TB(names[i]+" socket",model,position+Vector3.forward*.008f,new Vector3(.054f,.07f,.009f),LoadTruckMaterial("Graphite armor"),false,.005f);
                    TB(names[i]+" cap",cap,Vector3.zero,new Vector3(.046f,.056f,.009f),LoadTruckMaterial(i==3?"Tail lamp":"Graphite armor"),false,.004f);
                }
                Text(names[i]+" engraved label",cap,i==3?"CLEAR\nALL":names[i],new Vector3(0,0,-.006f),.007f,LoadTruckMaterial("Headlamp lens"));
                if(i==0)cap.GetChild(cap.childCount-1).GetComponent<TextMesh>().color=Color.black;
                cap.GetChild(cap.childCount-1).localScale=new Vector3(1/cap.localScale.x,1/cap.localScale.y,1);
                hits[i].SetParent(cap,false);hits[i].localPosition=new Vector3(0,0,-.008f);hits[i].localScale=new Vector3(1/cap.localScale.x,1/cap.localScale.y,1);hits[i].sizeDelta=new Vector2(.052f,.065f);
                foreach(var graphic in hits[i].GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
            }
            if(board.resetViewButton)board.resetViewButton.gameObject.SetActive(false);
            board.surface.sizeDelta=new Vector2(1155,610);board.surface.anchoredPosition=new Vector2(0,25);
            var grid=board.surface.Find("Graph dot grid") as RectTransform;if(grid)grid.sizeDelta=board.surface.sizeDelta;
            board.help.text="BLUE  EXEC > IN   /   GOLD  DATA > PARAM\nWheel: zoom   |   Drag empty space: pan   |   Side keys: edit   |   Tab: close";
            foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();VerifyCircuitSystem();TabletAuthoring.VerifyAndBuild();
        }
    }
}

