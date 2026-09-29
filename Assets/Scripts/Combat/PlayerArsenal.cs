using System;
using UnityEngine;

namespace Starfall {
 public sealed class PlayerArsenal:MonoBehaviour {
  public FirstPersonExplorer player;public BasinExpedition expedition;
  public Transform rifle,tool,muzzle;public Light muzzleLight;public Material tracerMaterial;
  public int Magazine {get;private set;}=12;
  public int ShotgunMagazine {get;private set;}=8;
  public int RailMagazine {get;private set;}=3;
  public int CurrentWeapon {get;private set;}
  LostRealmCatalog.Weapon WeaponConfig=>LostRealmCatalog.Current?LostRealmCatalog.Current.WeaponAt(CurrentWeapon):null;
  public int ActiveMagazine=>CurrentWeapon==1?ShotgunMagazine:CurrentWeapon==2?RailMagazine:Magazine;
  public int ActiveCapacity=>WeaponConfig!=null?WeaponConfig.magazine:CurrentWeapon==1?8:CurrentWeapon==2?3:12;
  public int ActiveCost=>WeaponConfig!=null?WeaponConfig.ammoPerShot:CurrentWeapon==1?2:CurrentWeapon==2?3:1;
  int legacyReserve=60;
  public int Reserve {get=>CampaignRuntime.Current?CampaignRuntime.Current.State.bag.Count(ItemId.Ammo):legacyReserve;private set {if(CampaignRuntime.Current){int delta=value-Reserve;if(delta>0)CampaignRuntime.Current.State.bag.Add(ItemId.Ammo,delta);else if(delta<0)CampaignRuntime.Current.State.bag.Remove(ItemId.Ammo,-delta);}else legacyReserve=value;}}
  public float Stamina {get;private set;}=100;
  public float MaxStamina=>CampaignRuntime.Current&&CampaignRuntime.Current.ModuleEquipped("stamina")?125:100;
  public float ReloadRemaining {get;private set;}
  public bool IsCharging=>charge>0;
  public bool IsBusy=>ReloadRemaining>0||cooldown>.12f||IsCharging;
  public bool Ready=>initialized&&player.Ready&&player.arena.CanTarget&&!player.Paused;
  public int ShotsFired {get;private set;}
  public int ConfirmedHits {get;private set;}
  public float TargetHealth=>target?target.Health:0;
  public bool MeleePending=>pendingMelee;
  public bool MinimalHud=>true;
  BasinProgress progress;bool initialized,exhausted,pendingMelee;
  float cooldown,recoil,melee,flash,hitMarker,restDelay,meleeDelay,charge,weaponClock,toolVisual,reloadVisual;
  CreatureVitality target;Vector3 rifleRest,toolRest;Transform shotgun,rail,shotgunMuzzle,railMuzzle;
  public void Initialize(BasinProgress state){if(initialized)return;initialized=true;progress=state;
   if(CampaignRuntime.Current){var save=CampaignRuntime.Current.State;Magazine=save.magazine;ShotgunMagazine=save.shotgunMagazine;RailMagazine=save.railMagazine;CurrentWeapon=CampaignRuntime.Current.WeaponUnlocked(save.selectedWeapon)?save.selectedWeapon:0;}
   else{Magazine=state.magazine;Reserve=state.reserve;}
   rifleRest=rifle.localPosition;toolRest=tool.localPosition;BuildSecondaryModels();ShowWeapon();
  }
  void BuildSecondaryModels(){
   Material metal=null,glow=null;foreach(var r in rifle.GetComponentsInChildren<Renderer>()){if(!metal)metal=r.sharedMaterial;if(r.sharedMaterial&&r.sharedMaterial.IsKeywordEnabled("_EMISSION"))glow=r.sharedMaterial;}
   if(!metal)metal=tracerMaterial;if(!glow)glow=metal;
   shotgun=new GameObject("破障霰射器 · articulated weapon").transform;shotgun.SetParent(rifle.parent,false);shotgun.localPosition=rifleRest;shotgun.gameObject.layer=30;
   WeaponPart("Long reinforced receiver",shotgun,new Vector3(0,0,.10f),new Vector3(.16f,.14f,.40f),metal);
   WeaponPart("Twin broad barrels",shotgun,new Vector3(-.052f,.055f,.35f),new Vector3(.065f,.07f,.49f),metal);
   WeaponPart("Parallel pressure barrel",shotgun,new Vector3(.052f,.055f,.35f),new Vector3(.065f,.07f,.49f),metal);
   WeaponPart("Sliding pump",shotgun,new Vector3(0,-.04f,.21f),new Vector3(.15f,.09f,.20f),glow);
   WeaponPart("Stock",shotgun,new Vector3(.08f,-.08f,-.22f),new Vector3(.13f,.13f,.32f),metal);
   shotgunMuzzle=new GameObject("Scatter muzzle").transform;shotgunMuzzle.SetParent(shotgun,false);shotgunMuzzle.localPosition=new Vector3(0,.055f,.61f);
   rail=new GameObject("磁轨穿刺枪 · articulated weapon").transform;rail.SetParent(rifle.parent,false);rail.localPosition=rifleRest;rail.gameObject.layer=30;
   WeaponPart("Rail spine",rail,new Vector3(0,0,.13f),new Vector3(.11f,.13f,.70f),metal);
   for(int side=-1;side<=1;side+=2){WeaponPart("Separated induction rail",rail,new Vector3(side*.10f,.07f,.28f),new Vector3(.045f,.065f,.52f),glow);WeaponPart("Capacitor",rail,new Vector3(side*.12f,-.06f,-.06f),new Vector3(.12f,.17f,.19f),metal);}
   WeaponPart("Charge coil",rail,new Vector3(0,.08f,.04f),new Vector3(.15f,.11f,.16f),glow);
   railMuzzle=new GameObject("Magnetic muzzle").transform;railMuzzle.SetParent(rail,false);railMuzzle.localPosition=new Vector3(0,.06f,.70f);
  }
  void WeaponPart(string name,Transform parent,Vector3 at,Vector3 scale,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=at;g.transform.localScale=scale;g.layer=30;Destroy(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;}
  void ShowWeapon(){if(rifle)rifle.gameObject.SetActive(CurrentWeapon==0);if(shotgun)shotgun.gameObject.SetActive(CurrentWeapon==1);if(rail)rail.gameObject.SetActive(CurrentWeapon==2);}
  public bool SelectWeapon(int index){if(index<0||index>2||index==CurrentWeapon||!initialized||!CampaignRuntime.Current||!CampaignRuntime.Current.WeaponUnlocked(index)||ReloadRemaining>0||cooldown>0)return false;charge=0;CurrentWeapon=index;CampaignRuntime.Current.State.selectedWeapon=index;ShowWeapon();Sync();InteractionFeel.Play(FeelCue.Reload,.45f);return true;}
  public void AddAmmo(int amount){Reserve=Mathf.Min(180,Reserve+Mathf.Max(0,amount));Sync();}
  public void Sync(){if(progress==null)return;progress.magazine=Magazine;progress.reserve=Reserve;if(CampaignRuntime.Current){var state=CampaignRuntime.Current.State;state.magazine=Magazine;state.shotgunMagazine=ShotgunMagazine;state.railMagazine=RailMagazine;state.selectedWeapon=CurrentWeapon;}}
  public bool SpendStamina(float amount){if(!initialized)return true;if(Stamina<amount)return false;Stamina-=amount;restDelay=1.1f;return true;}
  public bool Sprint(float dt){if(!initialized)return true;if(exhausted){if(Stamina<28)return false;exhausted=false;}if(Stamina<26*dt){exhausted=true;return false;}return SpendStamina(26*dt);}
  public bool Reload(){if(!Ready||ReloadRemaining>0||cooldown>0||charge>0||ActiveMagazine>=ActiveCapacity||Reserve<ActiveCost)return false;float baseDuration=WeaponConfig!=null?WeaponConfig.reload:CurrentWeapon==0?2.35f:CurrentWeapon==1?2.7f:1.8f;ReloadRemaining=baseDuration*(CampaignRuntime.Current&&CampaignRuntime.Current.ModuleEquipped("reload")?.8f:1);InteractionFeel.Play(FeelCue.Reload,.7f);return true;}
  public bool Attack(bool close=false){if(!Ready||ReloadRemaining>0||cooldown>0||expedition.MiningProgress>0)return false;
   if(close){charge=0;if(!SpendStamina(22)){InteractionFeel.Play(FeelCue.Deny,.4f);return false;}cooldown=.82f;melee=.45f;pendingMelee=true;meleeDelay=.13f;InteractionFeel.Play(FeelCue.Slash,.7f);return true;}
   if(CurrentWeapon==2){if(RailMagazine<3){Reload();return false;}if(charge<=0){charge=.001f;InteractionFeel.Play(FeelCue.Alert,.35f);}return true;}
   return Fire();
  }
  bool Fire(){if(!Ready||ActiveMagazine<ActiveCost){InteractionFeel.Play(FeelCue.Deny,.5f);Reload();return false;}
   if(CurrentWeapon==0)Magazine--;else if(CurrentWeapon==1)ShotgunMagazine-=2;else RailMagazine-=3;
   ShotsFired++;cooldown=WeaponConfig!=null?WeaponConfig.interval:CurrentWeapon==0?.28f:CurrentWeapon==1?.72f:1.25f;recoil=CurrentWeapon==0?.11f:CurrentWeapon==1?.22f:.26f;flash=.07f;charge=0;
   InteractionFeel.Play(FeelCue.Shot,CurrentWeapon==0?.85f:1);player.ImpactPulse(CurrentWeapon==0?.2f:.35f);Sync();
   if(CurrentWeapon==1){for(int i=0;i<6;i++){float a=i*2.39996f;Vector3 spread=player.viewCamera.transform.forward+player.viewCamera.transform.right*Mathf.Cos(a)*.075f+player.viewCamera.transform.up*Mathf.Sin(a)*.075f;ResolveShot(spread.normalized,WeaponConfig!=null?WeaponConfig.range:18,WeaponConfig!=null?WeaponConfig.bodyDamage:14,false);}}
   else ResolveShot(player.viewCamera.transform.forward,WeaponConfig!=null?WeaponConfig.range:CurrentWeapon==2?100:160,WeaponConfig!=null?WeaponConfig.bodyDamage:CurrentWeapon==2?70:26,CurrentWeapon==2);
   return true;
  }
  void ResolveShot(Vector3 direction,float range,int baseDamage,bool piercing){var origin=player.viewCamera.transform.position;bool found=Aim(origin,direction,range,out var hit);Vector3 endpoint=found?hit.point:origin+direction*range;
   if(found){var bossPart=hit.collider.GetComponent<CampaignBossPart>();if(bossPart){int value=piercing?baseDamage:CurrentWeapon==0?CampaignRules.GunDamage(1):baseDamage;if(bossPart.boss.Hit(bossPart.node,value)){ConfirmedHits++;hitMarker=.18f;player.arena.Impact(hit.point,Color.cyan);}}
    else{var vitality=hit.collider.GetComponentInParent<CreatureVitality>();if(vitality&&!vitality.Dead){float multiplier=vitality.DamageMultiplier(hit.point);int value=CurrentWeapon==0?CampaignRules.GunDamage(multiplier):piercing?(multiplier>1?(WeaponConfig!=null?WeaponConfig.weakDamage:120):baseDamage):Mathf.RoundToInt(baseDamage*multiplier);if(CurrentWeapon==1)value=Mathf.RoundToInt(value*Mathf.Lerp(.55f,1,Mathf.Clamp01((range-hit.distance)/(range*.67f))));if(multiplier>1&&CampaignRuntime.Current&&CampaignRuntime.Current.ModuleEquipped("weakness"))value=Mathf.RoundToInt(value*1.2f);if(vitality.TakeDamage(value,hit.point)){ConfirmedHits++;hitMarker=.18f;target=vitality;player.arena.Impact(hit.point,Color.cyan);}}else player.arena.Impact(hit.point,Color.gray);}}
   var g=new GameObject("Weapon shot tracer");g.transform.SetParent(player.arena.transform);var line=g.AddComponent<LineRenderer>();line.sharedMaterial=tracerMaterial;line.positionCount=2;line.SetPosition(0,CurrentWeapon==1?shotgunMuzzle.position:CurrentWeapon==2?railMuzzle.position:muzzle.position);line.SetPosition(1,endpoint);line.startWidth=CurrentWeapon==2?.065f:.022f;line.endWidth=.006f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;var fx=g.AddComponent<PlayerShotTrace>();fx.arena=player.arena;
  }
  void ResolveMelee(){var origin=player.viewCamera.transform.position;if(!Aim(origin,player.viewCamera.transform.forward,3.2f,out var hit))return;var part=hit.collider.GetComponent<CampaignBossPart>();if(part){if(part.boss.Hit(part.node,44)){ConfirmedHits++;hitMarker=.18f;player.arena.Impact(hit.point,Color.cyan);}return;}var vitality=hit.collider.GetComponentInParent<CreatureVitality>();if(vitality&&!vitality.Dead&&vitality.TakeDamage(44,hit.point)){ConfirmedHits++;hitMarker=.18f;player.arena.Impact(hit.point,Color.cyan);}}
  public bool Aim(Vector3 origin,Vector3 direction,float distance,out RaycastHit first){var hits=Physics.RaycastAll(origin,direction,distance,~(1<<30),QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var h in hits){if(h.collider.GetComponentInParent<FirstPersonExplorer>())continue;first=h;return true;}first=default;return false;}
  void Update(){if(!initialized||!player.Ready||player.Paused||!player.arena.Active)return;float dt=Time.deltaTime;
   cooldown=Mathf.Max(0,cooldown-dt);recoil=Mathf.Max(0,recoil-dt);melee=Mathf.Max(0,melee-dt);flash=Mathf.Max(0,flash-dt);hitMarker=Mathf.Max(0,hitMarker-dt);restDelay-=dt;if(restDelay<=0)Stamina=Mathf.Min(MaxStamina,Stamina+19*dt);
   if(player.arena.Downed){ReloadRemaining=0;melee=0;flash=0;charge=0;pendingMelee=false;expedition.MiningProgressReset();}
   else if(ReloadRemaining>0){ReloadRemaining-=dt;if(ReloadRemaining<=0){int load=Mathf.Min(ActiveCapacity-ActiveMagazine,Reserve);if(CurrentWeapon==1)load-=load%2;if(CurrentWeapon==2)load-=load%3;if(CurrentWeapon==0)Magazine+=load;else if(CurrentWeapon==1)ShotgunMagazine+=load;else RailMagazine+=load;Reserve-=load;ReloadRemaining=0;Sync();InteractionFeel.Play(FeelCue.Reload,.65f);}}
   if(pendingMelee){meleeDelay-=dt;if(meleeDelay<=0){pendingMelee=false;if(Ready)ResolveMelee();}}
   float chargeDuration=WeaponConfig!=null?WeaponConfig.charge:.9f;
   if(player.Automated&&CurrentWeapon==2&&charge>0){charge+=dt;if(charge>=chargeDuration)Fire();}
   if(!player.Automated&&Cursor.lockState==CursorLockMode.Locked&&!player.arena.Downed){
    if(Input.GetKeyDown(KeyCode.Alpha1))SelectWeapon(0);if(Input.GetKeyDown(KeyCode.Alpha2))SelectWeapon(1);if(Input.GetKeyDown(KeyCode.Alpha3))SelectWeapon(2);
    if(CurrentWeapon==2){if(Input.GetMouseButton(0)&&charge<=0)Attack();if(charge>0){if(Input.GetMouseButton(0)){charge+=dt;if(charge>=chargeDuration)Fire();}else charge=0;}}
    else if(Input.GetMouseButton(0))Attack();if(Input.GetMouseButtonDown(1))Attack(true);if(Input.GetKeyDown(KeyCode.R))Reload();
   }
   target=null;if(Aim(player.viewCamera.transform.position,player.viewCamera.transform.forward,160,out var aim))target=aim.collider.GetComponentInParent<CreatureVitality>();
   weaponClock+=dt;float walking=InteractionFeel.ReducedMotion?0:Mathf.Clamp01(player.Motor.velocity.magnitude/player.runSpeed);float mining=expedition.MiningProgress>0?Mathf.Sin(weaponClock*17)*.045f:0;reloadVisual=Mathf.Lerp(reloadVisual,ReloadRemaining>0?1:0,1-Mathf.Exp(-dt*13));toolVisual=Mathf.Lerp(toolVisual,melee>0||expedition.MiningProgress>0?1:0,1-Mathf.Exp(-dt*18));
   Vector3 bob=new Vector3(Mathf.Sin(weaponClock*6)*.006f,Mathf.Cos(weaponClock*12)*.004f,0)*walking;Vector3 pose=rifleRest+bob+new Vector3(0,-reloadVisual*.16f-toolVisual*.44f,-recoil*.8f);
   var active=CurrentWeapon==0?rifle:CurrentWeapon==1?shotgun:rail;active.localPosition=pose;active.localRotation=Quaternion.Euler(-recoil*85+toolVisual*18,reloadVisual*9,-reloadVisual*29);
   tool.gameObject.SetActive(melee>0||expedition.MiningProgress>0);tool.localPosition=toolRest+new Vector3(-Mathf.Sin(melee/.45f*Mathf.PI)*.35f,mining,0);tool.localRotation=Quaternion.Euler(-Mathf.Sin(melee/.45f*Mathf.PI)*75+mining*300,0,0);muzzleLight.intensity=flash>0?3:0;
  }
  void OnGUI(){if(!initialized||!player.Ready||player.Paused)return;var matrix=GUI.matrix;float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale;GUI.depth=-24;var light=new Color(.82f,.91f,.93f,.85f);var center=new Vector2(w*.5f,450);
   InteractionFeel.Text(new Rect(w-250,771,205,28),CurrentWeapon==0?"P12":CurrentWeapon==1?"破障霰射器":"磁轨穿刺枪",player.uiFont,13,light,TextAnchor.MiddleRight);
   InteractionFeel.Text(new Rect(w-195,800,150,40),ActiveMagazine+" / "+Reserve,player.uiFont,20,ActiveMagazine<ActiveCost?new Color(1,.47f,.22f):light,TextAnchor.MiddleRight);
   if(ReloadRemaining>0)InteractionFeel.Ring(center,22,1-ReloadRemaining/2.7f,new Color(.65f,.85f,.88f,.8f),2);
   if(charge>0)InteractionFeel.Ring(center,24,Mathf.Clamp01(charge/.9f),new Color(.40f,.81f,1,.9f),2);
   if(Stamina<MaxStamina-2){GUI.color=new Color(.03f,.06f,.08f,.6f);GUI.DrawTexture(new Rect(44,831,132,3),Texture2D.whiteTexture);GUI.color=Stamina<25?new Color(.96f,.4f,.18f):new Color(.37f,.7f,.8f);GUI.DrawTexture(new Rect(44,831,132*Stamina/MaxStamina,3),Texture2D.whiteTexture);GUI.color=Color.white;}
   if(target&&!target.Dead){Vector3 point=target.transform.position+Vector3.up*(target.GetComponent<SkyTitan>()?20:4);var titan=target.GetComponent<SkyTitan>();if(titan)point=titan.head.position+Vector3.up*10;var viewport=player.viewCamera.WorldToViewportPoint(point);float x=Mathf.Clamp(viewport.x*w,160,w-160),y=Mathf.Clamp((1-viewport.y)*900,100,670);InteractionFeel.Text(new Rect(x-140,y-30,280,24),target.DisplayName,player.uiFont,12,light,TextAnchor.MiddleCenter);GUI.color=new Color(.07f,.08f,.09f,.65f);GUI.DrawTexture(new Rect(x-52,y,104,3),Texture2D.whiteTexture);GUI.color=new Color(.91f,.38f,.17f);GUI.DrawTexture(new Rect(x-52,y,104*target.Health/(float)target.maxHealth,3),Texture2D.whiteTexture);GUI.color=Color.white;}
   if(hitMarker>0)InteractionFeel.Text(new Rect(center.x-20,430,40,40),"×",player.uiFont,26,new Color(1,.85f,.55f,Mathf.Clamp01(hitMarker*10)),TextAnchor.MiddleCenter);
   if(!string.IsNullOrEmpty(expedition.Prompt))InteractionFeel.Text(new Rect(center.x-210,494,420,28),expedition.Prompt,player.uiFont,14,light,TextAnchor.MiddleCenter);
   if(expedition.MiningProgress>0)InteractionFeel.Ring(center,20,expedition.MiningProgress,new Color(.38f,.86f,.87f,.9f),2);
   if(!string.IsNullOrEmpty(expedition.Notice))InteractionFeel.Text(new Rect(center.x-240,568-(1-expedition.NoticeAlpha)*18,480,30),expedition.Notice,player.uiFont,17,new Color(.9f,.94f,.9f,expedition.NoticeAlpha),TextAnchor.MiddleCenter);
   GUI.matrix=matrix;
  }
 }
 public sealed class PlayerShotTrace:MonoBehaviour {public CreatureArena arena;float t;void Update(){if(!arena||!arena.Active)return;t+=Time.deltaTime;if(t>.055f)Destroy(gameObject);}}
}
