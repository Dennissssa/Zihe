using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 public static partial class TutorialAuthoring
 {
  public static void RefineLessons(){EditorSceneManager.OpenScene(Path);RefineCurrent();}
  public static void RefineCurrent()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();
   foreach(var page in t.pages)if(page.diagram)UnityEngine.Object.DestroyImmediate(page.diagram);
   string[] titles={"MOVE AND CARRY","AMMO / WHAT TO FIRE","RATE / HOW FAST","DEPLOY A BEACON","DRIVING / GO TO A","REPAIR A CONNECTION","FACING / AIM AT A","FIRE / TWO PARAMETERS","TIMER / WHEN TO STOP","PACK AND TRANSPORT","FINAL ENGAGEMENT"};
   string[] guides={
    "Walk onto the blue pad with WASD. Look around with the mouse.\n\nWalk to the AMMO machine. Aim at its round carry knob and press E. Scroll to rotate the ghost preview, then place it on the orange pad with E.\n\nYou are only learning movement and carrying here. Function buttons come next. RMB cancels carrying.",
    "This area contains only the AMMO machine. It chooses WHAT the robot fires.\n\nClick the arrow buttons with LMB, then SELECT. Press TAB and find the new AMMO block.\n\nAMMO is a PARAMETER: it supplies a value to an action. It does not fire by itself. No wires are needed in this lesson.",
    "This area contains only the RATE machine. It chooses HOW FAST the robot fires.\n\nClick CHANGE to choose LOW, MID or HIGH, then press the red confirm button. TAB: find the RATE block.\n\nRATE is another PARAMETER. FIRE will need BOTH AMMO and RATE. No wires are needed yet.",
    "RMB raises the launcher. Scroll to A and LMB fires a beacon onto a surface. Select A and press E to recall it remotely, then fire it again.\n\nA beacon is a location that actions can use. DRIVING moves the robot to it; FACING turns the turret towards it.\n\nEach letter has its own color. RMB hides the launcher.",
    "The DRIVING machine adds a movement action and its selected Point parameter. A is already placed. Choose A and press the green DRIVING button.\n\nTAB: click START OUT, then DRIVING IN. Click POINT A output, then DRIVING PARAM. Either endpoint can be clicked first.\n\nCHECK, close TAB, then dock the tablet at the robot's back. Watch the robot drive to A.",
    "Select DRIVING on the prepared tablet circuit and press UNLINK. Its target wire disappears.\n\nReconnect POINT A output to DRIVING PARAM. Press CHECK.\n\nThe white execution wire says WHEN. The gold parameter wire says WHERE. Blocks touching each other does not connect them. Drag to arrange; scroll to zoom.",
    "The FACING machine turns the turret; it does not move the robot or fire. Choose A and press its green key.\n\nConnect START OUT -> FACING IN, and POINT A output -> FACING PARAM. CHECK and dock.\n\nObserve the turret aim at A. A Point can be shared by actions, but each action needs its own parameter wire.",
    "1. FIRE machine: click FIRE to add an ACTION. Connect START OUT -> FIRE IN.\n\n2. AMMO machine: SELECT a type. Connect AMMO output -> FIRE PARAM.\n\n3. RATE machine: confirm a speed. Connect RATE output -> the SAME FIRE PARAM. The second wire does not replace the first.\n\nBoth wires are required. CHECK, dock, observe firing, then remove the tablet to stop. FACING is not required for this exercise.",
    "The TIMER machine adds WAIT and a DURATION parameter. Set 2 seconds using its up/down keys, then confirm. Long-press adjusts faster.\n\nBuild FACING -> FIRE -> WAIT -> CEASE FIRE after START. Connect A to Facing, Ammo AND Rate to Fire, and Duration to Wait.\n\nCHECK and dock. WAIT delays the next action while firing continues. CEASE FIRE, from the FIRE machine's right button, stops it.",
    "This bay brings together the machines you have learned. Carry each into its tray preview and press E to pack all seven.\n\nCarry the tray to the truck bed and E to attach. LMB opens the door; E at the seat enters. WASD drives; SPACE brakes.\n\nDrive 8 metres, stop, E to exit. Unload the tray and take out one machine.",
    "Build START -> FACING -> FIRE. Facing uses POINT A; Fire needs both AMMO and RATE. CHECK and dock.\n\nR opens the remote. Click ENGAGE when ready. Docking does not start the wave.\n\nDefeat three ghosts. A destroyed base automatically restarts this stage."};
   int[] regions={0,0,0,1,2,2,2,3,3,4,5};
   CircuitKind[][] machineSets={new[]{CircuitKind.Ammo},new[]{CircuitKind.Ammo},new[]{CircuitKind.Rate},new CircuitKind[0],new[]{CircuitKind.DriveTo},new[]{CircuitKind.DriveTo},new[]{CircuitKind.Facing},new[]{CircuitKind.Fire,CircuitKind.Ammo,CircuitKind.Rate},new[]{CircuitKind.Facing,CircuitKind.Fire,CircuitKind.Ammo,CircuitKind.Rate,CircuitKind.Wait},new CircuitKind[0],new[]{CircuitKind.Facing,CircuitKind.Fire,CircuitKind.Ammo,CircuitKind.Rate}};
   t.pages=new TutorialPage[11];
   for(int i=0;i<11;i++)
   {
    GameObject diagram=null;if(i>=4&&i<=8||i==10){var rect=Rect(t.guidePanel.transform,"Circuit diagram "+i,new Vector2(345,20),new Vector2(560,490));diagram=rect.gameObject;
     if(i==6){Diagram(rect,4);foreach(var text in rect.GetComponentsInChildren<UnityEngine.UI.Text>())if(text.text=="DRIVING")text.text="FACING";}
     else Diagram(rect,i==7?9:i==8?7:i==10?9:4);diagram.SetActive(false);
    }
    t.pages[i]=new TutorialPage{title=titles[i],guide=guides[i],diagram=diagram,timeLimit=i<4?360:900,areaCenter=new Vector3(500+regions[i]*42,0,0),machines=machineSets[i],showRobot=i>=4};
   }
   t.zoneSize=new Vector2(40,75);t.body.rectTransform.anchoredPosition=new Vector2(0,10);t.body.rectTransform.sizeDelta=new Vector2(1150,530);t.body.text=guides[0];t.heading.text="01 / 11  MOVE AND CARRY";
   var floor=GameObject.Find("Tutorial training ground");floor.transform.position=new Vector3(605,-.3f,0);floor.transform.localScale=new Vector3(292,.6f,76);
   if(t.defense.surface){t.defense.surface.center=new Vector3(605,0,0);t.defense.surface.size=new Vector3(292,12,76);}
   foreach(var slot in t.rack.slots){var m=slot.machine;var nav=m.GetComponent<NavigationConsole>();if(m.GetComponent<RobotCircuitRunner>())continue;float x=m.GetComponent<AmmoSelectionConsole>()?-3:m.GetComponent<FireRateConsole>()?0:nav&&nav.mode==CircuitKind.Fire?3:nav&&nav.mode==CircuitKind.Wait?6:-6;m.transform.position=new Vector3(500+x,.05f,3);}
   t.placementGoal.position=new Vector3(497,.015f,-2);
   foreach(var obj in scene.GetRootGameObjects())if(obj.GetComponent<TextMesh>()&&obj.name.EndsWith(" label")&&((obj.transform.position.x>480&&obj.transform.position.x<520&&obj.transform.position.y<6)||obj.name.StartsWith("0")))UnityEngine.Object.DestroyImmediate(obj);
   // Only tutorial-owned room markers and partitions are added; equipment models are preserved.
   var previous=GameObject.Find("Tutorial area dividers");if(previous)UnityEngine.Object.DestroyImmediate(previous);
   var rooms=new GameObject("Tutorial area dividers");var wallMat=Mat("Room dividers",new Color(.14f,.18f,.23f));
   string[] names={"01 / INTERACTION","02 / BEACONS","03 / CIRCUIT LAB","04 / FIRING LAB","05 / TRANSPORT","06 / ENGAGEMENT"};
   for(int i=0;i<6;i++)
   {
    var center=new Vector3(500+i*42,0,0);
    if(i<5){var wall=Box("Training divider "+i,center+new Vector3(21,2.5f,0),new Vector3(.35f,5,76),wallMat);wall.transform.SetParent(rooms.transform,true);}
    var pad=Box(names[i],center+new Vector3(0,.01f,-9),new Vector3(14,.02f,2),Mat("Zone accent "+i,Color.Lerp(blue,gold,i/5f)));pad.transform.SetParent(rooms.transform,true);Sign(names[i],center+new Vector3(0,3,5));
   }
   foreach(var spawner in UnityEngine.Object.FindObjectsByType<GhostSpawner>())
   {
    if(!spawner.spawnOrigin){var spawn=new GameObject("Tutorial lane spawn").transform;spawn.SetParent(spawner.transform,false);spawn.position=spawner.transform.position+Vector3.back*3;spawner.spawnOrigin=spawn;}
    spawner.spawnRadius=0;
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("TUTORIAL_REFINED: 11 lessons, six separated bays, per-lesson machines, dual Fire parameters, diagrams only for circuit lessons");
  }
 }
}


