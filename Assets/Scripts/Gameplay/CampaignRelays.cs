using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRegion {
  LostRealmCatalog.Relay relayDefinition;RegionDeviceVisual[] relayVisuals;
  float relayTime;int relayMask;bool relayAlert;
  public float RelayRemaining=>relayTime;public int RelayMask=>relayMask;
  bool IsRelaySite(string key)=>key.StartsWith("relay:");
  void BuildRelays(){
   relayDefinition=LostRealmCatalog.Current?LostRealmCatalog.Current.RelayFor(id):null;if(relayDefinition==null||relayDefinition.anchors==null||relayDefinition.anchors.Length!=3)return;
   bool complete=campaign.Exp("relay-complete-"+id);relayVisuals=new RegionDeviceVisual[3];
   for(int i=0;i<3;i++){var key="relay:"+i;var site=MakeSite(key,relayDefinition.anchors[i],"Echo relay "+id+" / "+i);if(complete)sites.Remove(key);
    Box("Relay footing",site,new Vector3(0,.12f,0),new Vector3(1.1f,.24f,.8f),dark,false);
    Box("Relay signal mast",site,new Vector3(0,.9f,0),new Vector3(.18f,1.7f,.18f),steel,false);
    var display=Box("Relay three-band receiver",site,new Vector3(0,1.4f,-.1f),new Vector3(.75f,.35f,.22f),accent,false).GetComponent<Renderer>();
    for(int j=0;j<3;j++)Box("Receiver channel "+j,site,new Vector3((j-1)*.2f,1.7f,0),new Vector3(.065f,.4f,.065f),steel,false);
    var visual=site.gameObject.AddComponent<RegionDeviceVisual>();visual.Configure(player,display,null,new Color(.62f,.58f,.9f),complete);relayVisuals[i]=visual;
   }
  }
  public bool ActivateRelay(int index){
   if(relayDefinition==null||index<0||index>2||!player.arena.CanTarget||!Near("relay:"+index)||campaign.Exp("relay-complete-"+id))return false;
   if((relayMask&(1<<index))!=0)return false;
   if(relayTime<=0){relayTime=relayDefinition.seconds;relayMask=0;relayAlert=false;campaign.Notice("同步窗口开启 · 接通另外两座中继");}
   relayMask|=1<<index;relayVisuals[index].SetCompleted(true);InteractionFeel.Play(FeelCue.Portal,.45f);
   if(relayMask==7){relayTime=0;campaign.CompleteRelay(id);for(int i=0;i<3;i++)sites.Remove("relay:"+i);focus=null;}
   return true;
  }
  public void ResetRelay(){relayTime=0;relayMask=0;relayAlert=false;if(relayVisuals!=null)foreach(var visual in relayVisuals)if(visual)visual.SetCompleted(campaign.Exp("relay-complete-"+id));}
  void TickRelays(){if(relayTime<=0)return;if(player.arena.Downed){ResetRelay();return;}relayTime=Mathf.Max(0,relayTime-Time.deltaTime);if(relayTime<5&&!relayAlert){relayAlert=true;InteractionFeel.Play(FeelCue.Alert,.4f);}if(relayTime<=0){ResetRelay();campaign.Notice("信号失步 · 可重新同步");}}
  void DrawRelayHud(){if(relayTime<=0)return;InteractionFeel.Text(new Rect(Screen.width*.5f-155,Screen.height*.82f,310,35),"● "+((relayMask&1)!=0?"◆":"◇")+"  "+((relayMask&2)!=0?"◆":"◇")+"  "+((relayMask&4)!=0?"◆":"◇")+"    "+Mathf.CeilToInt(relayTime)+"s",player.uiFont,18,relayTime<5?new Color(1,.55f,.3f):new Color(.60f,.84f,.94f),TextAnchor.MiddleCenter);}
 }
}
