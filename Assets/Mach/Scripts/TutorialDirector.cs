using System;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Mach
{
 public enum TutorialLesson { BasicInteraction, LaunchBeacons, Driving, Facing, Shooting, AdvancedShooting, Transport, FinalTest, DroneBeacons }
 [Serializable] public class TutorialPage
 {
  public TutorialLesson lesson;public string title;[TextArea(3,12)] public string guide;[TextArea(2,5)] public string keys;
  public string[] requirements;public GameObject diagram;public float timeLimit=900;public Vector3 areaCenter;public CircuitKind[] machines;public bool showRobot;public TutorialStageLayout layout;
 }
 [DefaultExecutionOrder(10000)] public sealed class TutorialDirector:MonoBehaviour
 {
  public TutorialPage[] pages;public BehaviorProgram program;public PointLauncher launcher;public MachinePlacement placement;public TruckDriver driver;public UtilityTruck truck;public RobotCircuitRunner robot;public EquipmentRack rack;public AmmoSelectionConsole ammo;public GhostDefense defense;
  public Transform walkGoal,placementGoal;public Vector3 zoneCenter=new Vector3(500,0,0);public Vector2 zoneSize=new Vector2(22,38);
  public GameObject guidePanel;public Text heading,body,footer,objective,keyHints;public RectTransform continueButton;public MonoBehaviour[] inputScripts;
  public TutorialGuideVideo guideVideo;
  public Color todoColor=new Color(1,.76f,.35f),doneColor=new Color(.4f,1,.65f);
  public string gameplayScene="Assets/Mach/Scenes/WalkingPickup.unity";
  [Header("Guide text layout (canvas pixels)")] public Vector2 plainTextPosition=new Vector2(0,15),plainTextSize=new Vector2(1150,560),diagramTextPosition=new Vector2(-340,15),diagramTextSize=new Vector2(620,560);
  [HideInInspector] public int editorPreviewPage=-1;
  public int Stage {get;private set;}public bool GuideVisible=>guidePanel&&guidePanel.activeSelf;public bool[] milestones=new bool[8];
  public TutorialLesson Lesson=>pages[Stage].lesson;
  public MovableMachine CarryTarget=>pages.Length>0&&pages[0].layout?pages[0].layout.ResolveCarryTarget():ammo?ammo.GetComponent<MovableMachine>():null;
  public static void ResumeLesson(int stage){resumeStage=stage;retryReason="";}
  static int resumeStage;static string retryReason="";bool[] inputEnabled;bool reloading,runAccepted,faced,waited;float elapsed;int badChecks;string checkedSignature="";MovableMachine lastCarryTarget;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetSession(){resumeStage=0;retryReason="";}
  public static void BeginNewTutorial(){ResetSession();}
  void Start()
  {
   milestones=new bool[Mathf.Max(8,pages.Max(page=>page.requirements?.Length??0))];
#if UNITY_EDITOR
   int test=UnityEditor.SessionState.GetInt("Mach.Tutorial.TestPage",-1);if(test>=0){resumeStage=test;UnityEditor.SessionState.EraseInt("Mach.Tutorial.TestPage");}
#endif
   Stage=Mathf.Clamp(resumeStage,0,pages.Length-1);program.board.Checked+=OnCheck;placement.Placed+=OnPlaced;program.DockGuard=DockGuard;program.ProgramDocked+=OnDocked;robot.ActionCompleted+=OnAction;
   SetupCheckpoint();lastCarryTarget=CarryTarget;ShowGuide(retryReason);retryReason="";
  }
  void OnDestroy(){if(program){if(program.board)program.board.Checked-=OnCheck;program.ProgramDocked-=OnDocked;program.DockGuard=null;}if(placement)placement.Placed-=OnPlaced;if(robot)robot.ActionCompleted-=OnAction;Time.timeScale=1;}
  void SetupCheckpoint()
  {
   // A saved editor preview can leave machines packed under an inactive tray.
   // Restore the reusable scene instances before applying this lesson's visibility.
   foreach(var slot in rack.slots)if(slot&&slot.machine){if(slot.Packed)slot.Unpack();var parent=slot.machine.transform.parent;if(parent&&(parent.GetComponentInParent<EquipmentRack>()||parent.GetComponentInParent<UtilityTruck>()))slot.machine.transform.SetParent(null,true);}
   var layout=pages[Stage].layout;if(layout)layout.Apply();program.GetComponent<FirstPersonWalker>().ResetMotionAndLook();program.ClearCircuit();
   if(Lesson==TutorialLesson.DroneBeacons)
   {
    layout.droneStation.ReturnedToPad+=OnDroneReturned;
    foreach(var marker in program.pointMarkers)marker.Landed+=OnDroneBeaconLanded;
   }
   bool battle=Lesson==TutorialLesson.FinalTest;defense.enabled=battle;
   foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var spawner in root.GetComponentsInChildren<GhostSpawner>(true)){spawner.target=defense.home;spawner.spawning=battle;}
   // Only the early guided movement lessons provide deployed targets.
   // Later lessons start with the authored beacons stored in the launcher.
   if(layout&&(Lesson==TutorialLesson.Driving||Lesson==TutorialLesson.Facing)){if(layout.pointA)PlacePoint(0,layout.pointA.position);if(layout.pointB)PlacePoint(1,layout.pointB.position);}
   if(Lesson==TutorialLesson.Facing)SeedDriving();
   if(battle)
   {
    defense.totalEnemies=5;foreach(var slot in rack.slots)slot.Pack();var dock=truck.GetComponent<TruckRackDock>();var mover=rack.GetComponent<MovableMachine>();
    mover.BeginPreview();rack.transform.rotation=truck.transform.rotation;var center=dock?dock.deckCenter:new Vector3(0,1,-1.5f);
    rack.transform.position=truck.transform.TransformPoint(center)+truck.transform.up*(.015f-(mover.boundsCenter.y-mover.boundsSize.y*.5f)*rack.transform.lossyScale.y);mover.EndPreview(true,truck.transform);
    Physics.SyncTransforms();if(defense.surface)defense.surface.BuildNavMesh();
   }
  }
  void SeedDriving(){int drive=program.Add(CircuitKind.DriveTo),point=program.Add(CircuitKind.Point,"A");program.nodes[point].pointId="A";program.Connect(0,drive,false);program.Connect(point,drive,true);}
  void PlacePoint(int i,Vector3 at){var p=program.pointMarkers[i];if(p.Launch(at,Vector3.forward,program.transform)){p.Attach(at,Vector3.up,null);p.Advance(1);}}
  string Signature(){var b=new StringBuilder();foreach(var n in program.nodes)if(n!=null)b.Append(n.active).Append('|').Append(n.kind).Append('|').Append(n.value).Append('|').Append(n.pointId).Append('|').Append(n.point).Append('|').Append(n.next).Append('|').Append(n.pointInput).Append('|').Append(n.ammoInput).Append('|').Append(n.rateInput).Append('|').Append(n.durationInput).Append(';');return b.ToString();}
  bool CheckedCurrent=>!string.IsNullOrEmpty(checkedSignature)&&checkedSignature==Signature();
  bool Chain(params CircuitKind[] kinds){int current=program.nodes[0].next;foreach(var kind in kinds){if(!program.Valid(current)||program.nodes[current].kind!=kind)return false;current=program.nodes[current].next;}return current<0;}
  CircuitNode Node(CircuitKind kind){int current=program.nodes[0].next;var seen=new bool[program.nodes.Length];while(program.Valid(current)&&!seen[current]){seen[current]=true;var n=program.nodes[current];if(n.kind==kind)return n;current=n.next;}return null;}
  bool PointA(CircuitKind kind){var n=Node(kind);return n!=null&&program.Valid(n.pointInput)&&program.nodes[n.pointInput].pointId=="A";}
  bool AmmoIs(string value){var n=Node(CircuitKind.Fire);return n!=null&&program.Valid(n.ammoInput)&&program.nodes[n.ammoInput].value.Equals(value,StringComparison.OrdinalIgnoreCase);}
  bool RateIs(int value){var n=Node(CircuitKind.Fire);return n!=null&&program.Valid(n.rateInput)&&program.nodes[n.rateInput].value==value.ToString();}
  bool WaitTwo(){var n=Node(CircuitKind.Wait);return n!=null&&program.Valid(n.durationInput)&&BehaviorProgram.TrySeconds(program.nodes[n.durationInput].value,out float s)&&Mathf.Abs(s-2)<.01f;}
  bool CorrectCircuit()
  {
   if(!program.Validate(out _))return false;
   switch(Lesson){case TutorialLesson.Driving:return Chain(CircuitKind.DriveTo)&&PointA(CircuitKind.DriveTo);case TutorialLesson.Facing:return Chain(CircuitKind.Facing,CircuitKind.DriveTo)&&PointA(CircuitKind.Facing)&&PointA(CircuitKind.DriveTo);case TutorialLesson.Shooting:return Chain(CircuitKind.Fire)&&AmmoIs("BURST")&&RateIs(1);case TutorialLesson.AdvancedShooting:return Chain(CircuitKind.Facing,CircuitKind.Fire,CircuitKind.Wait,CircuitKind.CeaseFire)&&PointA(CircuitKind.Facing)&&AmmoIs("AP")&&RateIs(2)&&WaitTwo();default:return true;}
  }
  string DockGuard()=>Lesson==TutorialLesson.Driving&&(!CheckedCurrent||!CorrectCircuit())?"Press CHECK on the completed Driving circuit before docking. Re-check after changing wires.":null;
  void OnCheck(bool valid,string message)
  {
   if(GuideVisible)return;
   if(valid&&CorrectCircuit()){checkedSignature=Signature();badChecks=0;}
   else{checkedSignature="";if(valid)program.Say("CHECK: use this lesson's required actions and parameters.");if(++badChecks>=3)Retry("Check the required circuit and parameters.");}
  }
  void OnDocked(RobotCircuitRunner runner)
  {
   if(runner!=robot)return;runAccepted=CheckedCurrent&&CorrectCircuit();faced=waited=false;if(!runAccepted)return;
   if(Lesson==TutorialLesson.Driving)milestones[1]=true;if(Lesson==TutorialLesson.Facing)milestones[2]=true;if(Lesson==TutorialLesson.Shooting)milestones[3]=true;if(Lesson==TutorialLesson.AdvancedShooting)milestones[4]=true;
  }
  void OnAction(CircuitKind action)
  {
   if(!runAccepted)return;if(action==CircuitKind.Facing)faced=true;if(action==CircuitKind.Wait)waited=true;
   if(action==CircuitKind.DriveTo&&Vector3.ProjectOnPlane(robot.transform.position-program.pointMarkers[0].Position,Vector3.up).magnitude<.8f){if(Lesson==TutorialLesson.Driving)milestones[2]=true;if(Lesson==TutorialLesson.Facing&&faced)milestones[3]=true;}
   if(Lesson==TutorialLesson.AdvancedShooting&&action==CircuitKind.CeaseFire&&waited&&robot.ShotCount>0)milestones[5]=true;
  }
  static Vector3 WorldSize(Transform t,Vector3 size){var x=t.TransformVector(Vector3.right*size.x);var y=t.TransformVector(Vector3.up*size.y);var z=t.TransformVector(Vector3.forward*size.z);return new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));}
  bool MachineOnGoal(MovableMachine m){var box=placementGoal.GetComponent<BoxCollider>();var pad=box?new Bounds(placementGoal.TransformPoint(box.center),WorldSize(placementGoal,box.size)):placementGoal.GetComponent<Renderer>().bounds;var item=new Bounds(m.transform.TransformPoint(m.boundsCenter),WorldSize(m.transform,m.boundsSize));return Mathf.Min(pad.max.x,item.max.x)-Mathf.Max(pad.min.x,item.min.x)>.1f&&Mathf.Min(pad.max.z,item.max.z)-Mathf.Max(pad.min.z,item.min.z)>.1f&&Mathf.Abs(item.min.y-pad.max.y)<.4f;}
  void OnPlaced(MovableMachine machine){if(Lesson==TutorialLesson.BasicInteraction&&machine==CarryTarget&&MachineOnGoal(machine))milestones[0]=true;}
  public string GuideText(int index){var text=pages[index].guide;if(pages[index].lesson==TutorialLesson.AdvancedShooting)text=text.Replace("Connect the prepared actions:","Use the machines to add and connect:");return index==0?text.Replace("{carry}",CarryTarget?CarryTarget.displayName:"selected machine").Replace("AMMO",CarryTarget?CarryTarget.displayName:"selected machine"):text;}
  bool BeaconOn(int index,Transform target){var p=program.pointMarkers[index];if(!target||p.stored||p.InFlight||p.Recalling)return false;return p.transform.IsChildOf(target);}
  void OnDroneBeaconLanded(CircuitPointMarker marker,Transform owner,Transform surface)
  {
   var layout=pages[Stage].layout;
   if(owner!=layout.droneStation.drone||!surface)return;
   if(!milestones[0]){Retry("Switch to drop view");return;}
   int step=milestones[1]?2:1;
   if(marker==program.pointMarkers[step-1]&&surface.IsChildOf(step==1?layout.beaconTargetOne:layout.beaconTargetTwo))
    milestones[step]=true;
   else if(!milestones[2])Retry("Drop A on block 1, then B on block 2.");
  }
  void OnDroneReturned(){if(milestones[2])milestones[3]=true;}
  void Evaluate()
  {
   switch(Lesson)
   {
    case TutorialLesson.BasicInteraction:if(lastCarryTarget!=CarryTarget){lastCarryTarget=CarryTarget;milestones[0]=false;}break;
    case TutorialLesson.LaunchBeacons:
     var l=pages[Stage].layout;milestones[0]|=BeaconOn(0,l.beaconTargetOne);milestones[1]|=milestones[0]&&program.pointMarkers[0].stored;milestones[2]|=milestones[1]&&BeaconOn(1,l.beaconTargetTwo);break;
    case TutorialLesson.DroneBeacons:milestones[0]|=pages[Stage].layout.droneStation.InDropView;break;
    case TutorialLesson.Driving:milestones[0]=CheckedCurrent&&CorrectCircuit();break;
    case TutorialLesson.Facing:milestones[0]=Chain(CircuitKind.Facing,CircuitKind.DriveTo)&&PointA(CircuitKind.Facing)&&PointA(CircuitKind.DriveTo);milestones[1]=CheckedCurrent&&CorrectCircuit();break;
    case TutorialLesson.Shooting:milestones[0]=AmmoIs("BURST");milestones[1]=RateIs(1);milestones[2]=CheckedCurrent&&CorrectCircuit();milestones[4]|=runAccepted&&milestones[3]&&robot.ShotCount>0&&robot.Ammo=="BURST"&&robot.Rate==1;break;
    case TutorialLesson.AdvancedShooting:milestones[0]=AmmoIs("AP");milestones[1]=RateIs(2);milestones[2]=WaitTwo();milestones[3]=CheckedCurrent&&CorrectCircuit();break;
    case TutorialLesson.Transport:milestones[0]|=rack.slots.All(s=>s.Packed);milestones[1]|=milestones[0]&&rack.GetComponentInParent<UtilityTruck>()==truck;milestones[2]|=milestones[1]&&driver.CurrentTruck==truck;break;
    case TutorialLesson.FinalTest:milestones[0]|=!rack.GetComponentInParent<UtilityTruck>()&&rack.slots.All(s=>!s.Packed)&&!placement.IsPlacing;milestones[1]|=milestones[0]&&CheckedCurrent;milestones[2]|=milestones[1]&&(defense.Active||defense.Won);milestones[3]=milestones[2]&&defense.Won&&defense.Defeated>=5;if(defense.home.Destroyed)Retry("The base was lost. Deploy and check your robot before engaging.");break;
   }
  }
  public void ShowGuide(string reason="")
  {
   var station=pages[Stage].layout?.droneStation;
   if(station&&station.Controlling)station.hud.SetActive(false);
   if(GuideVisible&&inputEnabled!=null)return;inputEnabled=new bool[inputScripts.Length];for(int i=0;i<inputScripts.Length;i++)if(inputScripts[i]){inputEnabled[i]=inputScripts[i].enabled;inputScripts[i].enabled=false;}
   guidePanel.SetActive(true);if(keyHints)keyHints.gameObject.SetActive(false);if(objective)objective.gameObject.SetActive(false);Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   heading.text=(Stage+1).ToString("00")+" / "+pages.Length+"   "+pages[Stage].title;body.text=(string.IsNullOrEmpty(reason)?"":"RETRY: "+reason+"\n\n")+GuideText(Stage);footer.text="F1  REOPEN GUIDE";
   bool illustrated=pages[Stage].diagram;body.rectTransform.anchoredPosition=illustrated?diagramTextPosition:plainTextPosition;body.rectTransform.sizeDelta=illustrated?diagramTextSize:plainTextSize;
   bool video=guideVideo&&guideVideo.Show(Stage);
   for(int i=0;i<pages.Length;i++)if(pages[i].diagram)pages[i].diagram.SetActive(i==Stage&&!video);
  }
  public void BeginPractice(){guidePanel.SetActive(false);Time.timeScale=1;for(int i=0;i<inputScripts.Length;i++)if(inputScripts[i])inputScripts[i].enabled=inputEnabled[i];if(keyHints)keyHints.gameObject.SetActive(true);if(objective)objective.gameObject.SetActive(true);Cursor.lockState=program.Editing||program.remote.IsRaised?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=Cursor.lockState==CursorLockMode.None;var station=pages[Stage].layout?.droneStation;if(station&&station.Controlling)station.hud.SetActive(true);RenderHUD();}
  void RenderHUD()
  {
   if(keyHints)keyHints.text=System.Text.RegularExpressions.Regex.Replace(pages[Stage].keys??"",@"[ \t]+(?=<color=)","\n")+"\n<color=#B5C3CC>F1  GUIDE</color>";if(!objective)return;var b=new StringBuilder("REQUIREMENTS\n");var requirements=pages[Stage].requirements??Array.Empty<string>();
   for(int i=0;i<requirements.Length;i++){bool done=milestones[i];b.Append("\n<color=#").Append(ColorUtility.ToHtmlStringRGB(done?doneColor:todoColor)).Append("><b>").Append(done?"[X]":"[ ]").Append("  ").Append(i+1).Append(".</b>  ").Append(requirements[i].Replace("{carry}",CarryTarget?CarryTarget.displayName:"selected machine")).Append("</color>\n");}objective.text=b.ToString();objective.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,objective.preferredHeight+8);
  }
  public void Retry(string reason){if(reloading)return;retryReason=reason;Reload(Stage);}
  void Reload(int stage){reloading=true;Time.timeScale=1;resumeStage=stage;SceneManager.LoadScene(gameObject.scene.path);}
  void FinishStage(){if(Stage+1<pages.Length){Reload(Stage+1);return;}reloading=true;StartCoroutine(EnterGame());}
  IEnumerator EnterGame(){yield return new WaitForSecondsRealtime(1.5f);Time.timeScale=1;SceneManager.LoadScene(gameplayScene);}
  void Update()
  {
            if(MissionPauseMenu.BlocksGameplay)return;
   if(reloading)return;var key=Keyboard.current;var mouse=Mouse.current;
   if(GuideVisible){if(key!=null&&key.enterKey.wasPressedThisFrame||mouse!=null&&mouse.leftButton.wasPressedThisFrame&&RectTransformUtility.RectangleContainsScreenPoint(continueButton,mouse.position.ReadValue()))BeginPractice();return;}
   if(key!=null&&key.f1Key.wasPressedThisFrame){ShowGuide();return;}
   elapsed+=Time.deltaTime;var p=program.transform.position;if(p.y<zoneCenter.y-5||Mathf.Abs(p.x-zoneCenter.x)>zoneSize.x*.5f||Mathf.Abs(p.z-zoneCenter.z)>zoneSize.y*.5f){Retry("You left the training area.");return;}
   if(elapsed>pages[Stage].timeLimit){Retry("Practice time ended.");return;}if(program.Status.StartsWith("ERROR:")){Retry(program.Status);return;}
   Evaluate();RenderHUD();if(!reloading&&pages[Stage].requirements!=null&&pages[Stage].requirements.Length>0&&milestones.Take(pages[Stage].requirements.Length).All(x=>x))FinishStage();
  }
 }
}

