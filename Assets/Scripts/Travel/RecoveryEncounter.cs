using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Starfall {
 [Serializable] public sealed class EncounterRequest {
  public SubsurfaceDefinition source;
  public int sourceId;
  public string encounterId,fuelRewardId;
  public Vector3 recoveryPosition;
  public EncounterRequest(SubsurfaceDefinition definition,int id,Vector3 position){source=definition;sourceId=id;encounterId=definition.encounterId;fuelRewardId=definition.fuelRewardId;recoveryPosition=position;}
 }
 [Serializable] public sealed class EncounterRequestedEvent:UnityEvent<EncounterRequest>{}
 public sealed class RecoveryEncounter:MonoBehaviour {
  public SceneDirector director;
  [Header("Connect the future combat scene here. No combat or rewards are implemented.")]
  public EncounterRequestedEvent onBattleRequested=new EncounterRequestedEvent();
  public event Action<EncounterRequest> BattleRequested;
  public EncounterRequest LastRequest {get;private set;}
  public bool IsPresenting {get;private set;}
  public float AlertAge {get;private set;}
  public bool ExclamationVisible=>IsPresenting&&(AlertAge<.24f||(AlertAge>=.46f&&AlertAge<.70f));
  public int DispatchCount {get;private set;}
  readonly HashSet<int> handled=new HashSet<int>();
  EncounterRequest active;
  public bool TryBegin(SubsurfaceDefinition source,int sourceId){
   if(IsPresenting||!source||source.payloadKind!=BuriedPayloadKind.DormantEncounter||handled.Contains(sourceId))return false;
   handled.Add(sourceId);active=new EncounterRequest(source,sourceId,director.player.position);InteractionFeel.Play(FeelCue.Alert,.9f);AlertAge=0;IsPresenting=true;return true;
  }
  void Update(){
   if(!IsPresenting)return;
   AlertAge+=Time.deltaTime;
   if(AlertAge<.94f)return;
   LastRequest=active;active=null;IsPresenting=false;DispatchCount++;
   // Future battle code owns the scene transition after this once-only notification.
   // With no subscriber the player can continue exploring; no placeholder battle screen.
   onBattleRequested.Invoke(LastRequest);BattleRequested?.Invoke(LastRequest);
  }
  void OnGUI(){
   if(!ExclamationVisible||!director||!director.worldCamera||!director.uiFont)return;
   var old=GUI.matrix;GUI.depth=-80;float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
   GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
   float head=director.playerSprite?director.playerSprite.bounds.max.y:director.player.position.y+1.1f;
   var p=director.worldCamera.WorldToViewportPoint(new Vector3(director.player.position.x,head+.2f,0));
   float x=p.x*1600,y=Mathf.Clamp((1-p.y)*900,150,760),phase=AlertAge<.24f?AlertAge:AlertAge-.46f;
   float size=44+6*Mathf.Sin(Mathf.Clamp01(phase/.24f)*Mathf.PI);
   var style=new GUIStyle(GUI.skin.label){font=director.uiFont,fontSize=Mathf.RoundToInt(size),fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
   style.normal.textColor=new Color(.04f,.025f,.02f);GUI.Label(new Rect(x-26+2,y-50+3,52,58),"!",style);
   style.normal.textColor=new Color(1,.72f,.18f);GUI.Label(new Rect(x-26,y-50,52,58),"!",style);GUI.matrix=old;
  }
 }
}
