using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 public sealed partial class CampaignRegion:MonoBehaviour {
  public CampaignRuntime campaign;public FirstPersonExplorer player;public int id;public CampaignBoss boss;
  readonly Dictionary<string,Transform> sites=new Dictionary<string,Transform>();readonly List<Material> materials=new List<Material>();readonly Dictionary<string,GameObject> crates=new Dictionary<string,GameObject>();
  public Material steel,accent,dark,warningMaterial;GameObject root;string focus;Mesh armorMesh;
  public void Configure(CampaignRuntime owner,int region){
   campaign=owner;id=region;player=GetComponent<FirstPersonExplorer>();root=new GameObject("Campaign region "+region);root.transform.SetParent(player.arena.transform);
   steel=Mat("Weathered alloy",new Color(.24f,.29f,.31f));accent=Mat("Anchor glow",new Color(.25f,.76f,.84f),.7f);dark=Mat("Dark panels",new Color(.07f,.10f,.13f));warningMaterial=Mat("D4 amber warning",new Color(1,.32f,.08f),.65f);armorMesh=CampaignGeometry.Armor();
   if(region==1){foreach(var resource in player.expedition.resources)resource.gameObject.SetActive(false);player.expedition.resources=new BasinResource[0];Wreck();}
   else{
    if(region<4){var titan=FindFirstObjectByType<SkyTitan>();if(titan)titan.gameObject.SetActive(false);}
    if(region==5){var titan=FindFirstObjectByType<SkyTitan>();if(titan)titan.gameObject.SetActive(false);foreach(var actor in player.arena.creatures)actor.gameObject.SetActive(false);player.arena.creatures=new AlienCreature[0];}
    for(int i=0;i<player.expedition.resources.Length;i++){var r=player.expedition.resources[i];r.metal=region==2?(i%2==0?RepairResource.FerricAlloy:RepairResource.CopperCompound):region==3?RepairResource.CobaltSilicate:RepairResource.RareComposite;r.gameObject.SetActive(region!=5);var block=new MaterialPropertyBlock();block.SetColor("_Color",OreInventory.MetalColor(r.metal)*.48f);block.SetColor("_EmissionColor",OreInventory.MetalColor(r.metal)*.08f);foreach(var renderer in r.crystals.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(block);}
    var station=MakeSite("terminal",new Vector3(-6,0,-20),"档案与补给");Box("Console",station,new Vector3(0,.6f,0),new Vector3(1.7f,1.2f,.7f),steel,true);Box("Screen",station,new Vector3(0,1.28f,-.08f),new Vector3(1.4f,.10f,.8f),accent,false);
    // Each campaign region carries its own saved geometry and navigation scene.
    if(region==5){boss=new GameObject("D4 guardian mech").AddComponent<CampaignBoss>();boss.transform.SetParent(root.transform);boss.Configure(this);var control=MakeSite("core",new Vector3(0,0,21),"D4核心");Box("Isolation terminal",control,Vector3.up*.9f,new Vector3(2.2f,1.8f,1),steel,true);Box("Isolation light",control,new Vector3(0,1.5f,-.53f),new Vector3(1.8f,.45f,.03f),accent,false);}
   }
   BuildExpansion();BuildFrontier();RelocateSavedLoot();foreach(var c in campaign.State.crates)if(c.region==id)SpawnCrate(c);
  }
  Material Mat(string name,Color color,float glow=0){var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Metallic",.6f);m.SetFloat("_Glossiness",.25f);if(glow>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*glow);}materials.Add(m);return m;}
  public Transform Box(string name,Transform parent,Vector3 local,Vector3 size,Material material,bool collide){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<MeshFilter>().sharedMesh=armorMesh;if(!collide)Destroy(g.GetComponent<Collider>());else if(name=="Medical habitat"||name=="Stored D4 cells"||name.StartsWith("Core blast shelter")||name.StartsWith("Power barrier")){var obstacle=g.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.carving=true;}return g.transform;}
  Transform MakeSite(string key,Vector3 position,string label){var g=new GameObject(label).transform;g.SetParent(root.transform);if(Mathf.Abs(position.y)<.001f)position.y=AlienSurfaceLandmarks.Height(position.x,position.z);if(NavMesh.SamplePosition(position,out var nav,5,NavMesh.AllAreas))position=nav.position;g.position=position;sites[key]=g;return g;}
  void Wreck(){
   var wreck=MakeSite("wreck",new Vector3(0,0,-4),"Half buried explorer ship");wreck.rotation=Quaternion.Euler(0,-14,-7);
   Box("Buried keel",wreck,new Vector3(0,.55f,0),new Vector3(4.4f,1.3f,8.8f),dark,true);Box("Hull crown",wreck,new Vector3(0,1.25f,-.8f),new Vector3(3.5f,1.3f,5.2f),steel,true);Box("Cockpit",wreck,new Vector3(0,1.85f,-2),new Vector3(2.1f,.42f,1.9f),accent,false);
   var nose=Box("Collapsed tapered nose",wreck,new Vector3(0,.6f,-4),new Vector3(2.6f,.9f,2.7f),steel,true);nose.localRotation=Quaternion.Euler(-17,0,0);
   for(int i=0;i<6;i++){var plate=Box("Independent hull rib "+i,wreck,new Vector3((i%2==0?-1:1)*2.2f,.72f,-2+i/2*1.8f),new Vector3(1.2f,.18f,1.6f),steel,true);plate.localRotation=Quaternion.Euler(0,0,(i%2==0?-1:1)*15);Box("Panel inset "+i,wreck,new Vector3((i%2==0?-1:1)*1.63f,1.95f,-2+i/2*1.4f),new Vector3(.10f,.045f,.95f),dark,false);}
   for(int side=-1;side<=1;side+=2){var wing=Box("Broken swept wing",wreck,new Vector3(side*3.1f,.22f,1.5f),new Vector3(3.6f,.27f,2.6f),steel,true);wing.localRotation=Quaternion.Euler(0,side*27,side*8);Box("Engine pod",wreck,new Vector3(side*1.4f,.65f,3.6f),new Vector3(1.1f,1.3f,2.1f),dark,true);Box("Cold engine interior",wreck,new Vector3(side*1.4f,.7f,4.67f),new Vector3(.7f,.7f,.06f),accent,false);}
   var obstacle=wreck.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=Vector3.up;obstacle.size=new Vector3(5.5f,3,9.5f);obstacle.carving=true;
   var chest=MakeSite("chest",new Vector3(0,0,-11),"Alliance cargo chest");Box("Cargo base",chest,Vector3.up*.4f,new Vector3(1.35f,.8f,.85f),steel,true);Box("Cargo lid",chest,new Vector3(0,.83f,0),new Vector3(1.4f,.12f,.90f),dark,true);Box("Alliance seal",chest,new Vector3(0,.50f,-.44f),new Vector3(.5f,.19f,.03f),accent,false);
  }
  void Decorate(int region){
   for(int i=0;i<7;i++){var g=new GameObject("Regional structure "+i).transform;g.SetParent(root.transform);float x=(i%2==0?-1:1)*(12+i*2),z=-9+i*6;g.position=new Vector3(x,AlienSurfaceLandmarks.Height(x,z),z);
    if(region==2){Box("Broken road",g,new Vector3(0,.07f,0),new Vector3(5,.15f,8),dark,false);var slab=Box("Evacuation barrier",g,Vector3.up*.8f,new Vector3(3,1.6f,.7f),steel,true);slab.localRotation=Quaternion.Euler(0,i*17,i%2*12);}
    else if(region==3){Box("Medical habitat",g,new Vector3(0,1.5f,0),new Vector3(4,3,4),steel,true);Box("Medical cross vertical",g,new Vector3(0,1.8f,-2.03f),new Vector3(.22f,1.15f,.04f),accent,false);Box("Medical cross horizontal",g,new Vector3(0,1.8f,-2.04f),new Vector3(.8f,.22f,.04f),accent,false);}
    else{Box("Dock support",g,new Vector3(0,3,0),new Vector3(1.1f,6,1.1f),steel,true);Box("Military gantry",g,new Vector3(2.4f,6,0),new Vector3(6,.6f,1.2f),dark,true);Box("Stored D4 cells",g,new Vector3(2,1,2),new Vector3(3,2,2),steel,true);}
   }
  }
  public void SpawnCrate(SavedCrate c){if(c.region!=id||crates.ContainsKey(c.id))return;var g=new GameObject("Recovery cache "+c.id);g.transform.SetParent(root.transform);g.transform.position=c.position;Box("Sealed recovery case",g.transform,Vector3.up*.33f,new Vector3(.8f,.65f,.7f),steel,true);Box("Recovery beacon strip",g.transform,new Vector3(0,.68f,0),new Vector3(.75f,.04f,.68f),accent,false);crates[c.id]=g;sites["crate:"+c.id]=g.transform;}
  public bool Near(string key){if(!sites.TryGetValue(key,out var target)||!target)return false;return Vector3.Distance(player.transform.position,target.position)<3.5f&&player.expedition.Visible(target.position+Vector3.up*.65f,target);}
  void Update(){
   if(!player.Ready||player.Paused||!player.arena.Active)return;
   focus=null;float closest=4;
   foreach(var s in sites){if(s.Key=="wreck")continue;float distance=Vector3.Distance(player.transform.position,s.Value.position);if(distance<closest&&Near(s.Key)){focus=s.Key;closest=distance;}}
   TickRegionalEvent();TickRelays();TickFrontier();foreach(var id in new List<string>(crates.Keys))if(!campaign.State.crates.Exists(x=>x.id==id&&x.region==this.id)){sites.Remove("crate:"+id);Destroy(crates[id]);crates.Remove(id);}
  }
  public bool Interact(){
   if(campaign.Modal||player.Paused)return false;
   if(Vector3.Distance(player.transform.position,player.returnBeacon.position)<4){if(campaign.BombSeconds>=0){campaign.Evacuate();return true;}if(campaign.State.device&&!campaign.State.calibrated)campaign.CalibrateDevice();player.Return();return true;}
   if(focus==null)return false;if(focus.StartsWith("crate:")){if(!campaign.RecoverCrate(focus.Substring(6)))campaign.Notice("背包空间不足");return true;}
   if(IsFrontierSite(focus))return FrontierInteract(focus);if(IsRelaySite(focus)){ActivateRelay(int.Parse(focus.Substring(6)));return true;}
   if(IsExpansionSite(focus)){campaign.ExpansionInteract(focus);RefreshExpansionSite(focus);return true;}
   campaign.Open(focus=="chest"?"chest":focus=="core"?"core":"terminal");return true;
  }
  void OnGUI(){if(!player.Ready||player.Paused||campaign.Modal)return;DrawScanMarkers();DrawRelayHud();DrawFrontierHud();if(focus==null)return;var title=focus.StartsWith("crate:")?"回收物资":IsFrontierSite(focus)?FrontierSiteName(focus):IsRelaySite(focus)?"同步中继":IsExpansionSite(focus)?ExpansionSiteName(focus):focus=="chest"?"打开遗骸货舱":focus=="core"?"核心控制台":"查看区域终端";InteractionFeel.Text(new Rect(Screen.width/2-230,Screen.height*.69f,460,35),"E  "+title,player.uiFont,18,new Color(.92f,.77f,.49f),TextAnchor.MiddleCenter);}
  public void DrawTerminalView(){
   campaign.StoryText(new Rect(240,215,1110,165),id==2?"撤离记录仍在循环。\n军需官（救援站频道）：先修好中继，我们就能一起回家。":id==3?"医疗报告、裂隙实验与中继日志保存在这里。\n诊断上传只需要局部数据；完整请求包含现实端出口权限。":id==4?"军用船坞内的军令、原型维护记录与舰队清单相互吻合。\n指挥官：把装置交出来，我们可以一起离开。你也能分享下一次远征。":"同盟最后的隔离方案与种源库记录已找到。\n共用核心连接全部星界，切断现实耦合才能限制坍缩。",22);
   if(campaign.StoryAction(new Rect(260,429,1040,55),"读取档案与领取补给")){ReadArchive();}
   if(id==3){if(campaign.StoryAction(new Rect(260,514,480,48),"仅发送局部诊断")){campaign.State.Flag("partial-data");campaign.Changed();campaign.Notice("出口权限保持锁定");}if(campaign.StoryAction(new Rect(805,514,495,48),"查看完整出口权限请求")){campaign.ReviewPermission();}}
   if(id==4){string[] choices={"拒绝交出装置","拖延并调查","假意配合，记录路线"};for(int i=0;i<3;i++)if(campaign.StoryAction(new Rect(230+i*380,550,350,48),choices[i])){campaign.State.Flag("commander-route");campaign.Changed();campaign.Notice("通路已记录，装置仍由你掌握");}}
  }
  public void DrawCoreView(){
   campaign.StoryText(new Rect(245,209,1100,90),campaign.State.Has("boss-defeated")?"守核机甲已停机。指挥官拒绝放下武器，仍试图启动D4。":"远征议会指挥官：你以为你修好的是回家的路。那是我们重新出征的航道。\n先击破三处供能节点，等待胸部弱点暴露。",21);
   string[] labels={"广播解除武装与撤离窗口，核查生命维持舱","切断现实端能量耦合","锁定现实安全锚点"};string[] flags={"life-support-checked","coupling-cut","evac-anchor"};
   for(int i=0;i<3;i++)if(campaign.StoryAction(new Rect(230,347+i*74,1140,52),labels[i]+(campaign.State.Has(flags[i])?" · 已完成":""),campaign.State.Has("boss-defeated")&&!campaign.State.Has(flags[i]))){CoreStep(i);}
   if(campaign.StoryAction(new Rect(260,603,460,52),campaign.State.Has("bomb-installed")?"炸弹已安装":"安装异晶炸弹",campaign.SavingReady&&!campaign.State.Has("bomb-installed")))campaign.InstallBomb();
   if(campaign.StoryAction(new Rect(840,603,460,52),"检查倒计时与撤离",campaign.State.Has("bomb-installed")))campaign.ShowDetonate();
  }
  public bool ReadArchive(){if(!Near("terminal"))return false;if(id==2)campaign.Learn("J03");if(id==3){campaign.Learn("J04");campaign.Learn("J05");}if(id==4)campaign.Learn("J06");if(id==5){campaign.Learn("J07");campaign.Learn("J08");}campaign.ClaimSupplies(id);campaign.Changed();campaign.Notice("档案已记录 · J 查看手账");return true;}
  public bool CoreStep(int step){if(id!=5||!Near("core")||!campaign.State.Has("boss-defeated")||step<0||step>2)return false;string[] flags={"life-support-checked","coupling-cut","evac-anchor"};campaign.State.Flag(flags[step]);if(step==1)campaign.State.flags.Remove("exit-authorized");campaign.Changed();campaign.Notice(step==0?"终端确认：无其他生命维持舱，仅余拒绝撤离的武装残党":"已完成隔离校准");return true;}
  void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);if(armorMesh)Destroy(armorMesh);}
 }
 public sealed partial class CampaignRuntime {
  public void StoryText(Rect r,string text,int size=20)=>Paragraph(r,text,size);
  public bool StoryAction(Rect r,string text,bool enabled=true)=>Action(r,text,enabled);
  public void ShowDetonate(){panel="detonate";}
 }
}
