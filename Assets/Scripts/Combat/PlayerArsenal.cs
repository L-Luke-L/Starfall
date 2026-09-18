using System;
using UnityEngine;

namespace Starfall {
 public sealed class PlayerArsenal:MonoBehaviour {
  public FirstPersonExplorer player;public BasinExpedition expedition;
  public Transform rifle,tool,muzzle;public Light muzzleLight;public Material tracerMaterial;
  public int Magazine {get;private set;}=12;
  public int Reserve {get;private set;}=60;
  public float Stamina {get;private set;}=100;
  public float ReloadRemaining {get;private set;}
  public bool IsBusy=>ReloadRemaining>0||cooldown>.12f;
  public bool Ready=>initialized&&player.Ready&&player.arena.CanTarget&&!player.Paused;
  public int ShotsFired {get;private set;}
  public int ConfirmedHits {get;private set;}
  public float TargetHealth=>target?target.Health:0;
  BasinProgress progress;bool initialized,exhausted,pendingMelee;float meleeDelay,reloadVisual,weaponClock,toolVisual;
  public bool MeleePending=>pendingMelee;
  public bool MinimalHud=>true;float cooldown,recoil,melee,flash,hitMarker,restDelay;CreatureVitality target;Vector3 rifleRest,toolRest;
  public void Initialize(BasinProgress state){if(initialized)return;initialized=true;progress=state;Magazine=state.magazine;Reserve=state.reserve;rifleRest=rifle.localPosition;toolRest=tool.localPosition;}
  public void AddAmmo(int amount){Reserve=Mathf.Min(180,Reserve+Mathf.Max(0,amount));Sync();}
  void Sync(){if(progress==null)return;progress.magazine=Magazine;progress.reserve=Reserve;}
  public bool SpendStamina(float amount){if(!initialized)return true;if(Stamina<amount)return false;Stamina-=amount;restDelay=1.1f;return true;}
  public bool Sprint(float dt){if(!initialized)return true;if(exhausted){if(Stamina<28)return false;exhausted=false;}if(Stamina<26*dt){exhausted=true;return false;}return SpendStamina(26*dt);}
  public bool Reload(){if(!Ready||ReloadRemaining>0||cooldown>0||Magazine>=12||Reserve<=0)return false;ReloadRemaining=2.35f;InteractionFeel.Play(FeelCue.Reload,.7f);return true;}
  public bool Attack(bool close=false){
   if(!Ready||ReloadRemaining>0||cooldown>0||expedition.MiningProgress>0)return false;
   if(close){if(!SpendStamina(22)){InteractionFeel.Play(FeelCue.Deny,.4f);return false;}cooldown=.82f;melee=.45f;pendingMelee=true;meleeDelay=.13f;InteractionFeel.Play(FeelCue.Slash,.7f);}else{if(Magazine<=0){InteractionFeel.Play(FeelCue.Deny,.5f);Reload();return false;}Magazine--;ShotsFired++;cooldown=.28f;recoil=.11f;flash=.065f;InteractionFeel.Play(FeelCue.Shot,.85f);player.ImpactPulse(.2f);Sync();}
   if(!close)ResolveAttack(false);return true;
  }
  void ResolveAttack(bool close){
   var origin=player.viewCamera.transform.position;var direction=player.viewCamera.transform.forward;float range=close?3.2f:160;RaycastHit hit;
   bool found=Aim(origin,direction,range,out hit);var endpoint=found?hit.point:origin+direction*range;
   if(found){var vitality=hit.collider.GetComponentInParent<CreatureVitality>();if(vitality&&!vitality.Dead){int damage=close?44:Mathf.RoundToInt(26*vitality.DamageMultiplier(hit.point));if(vitality.TakeDamage(damage,hit.point)){ConfirmedHits++;hitMarker=.18f;target=vitality;player.arena.Impact(hit.point,Color.cyan);}}else player.arena.Impact(hit.point,Color.gray);}
   if(!close){var g=new GameObject("Player shot tracer");g.transform.SetParent(player.arena.transform);var line=g.AddComponent<LineRenderer>();line.sharedMaterial=tracerMaterial;line.positionCount=2;line.SetPosition(0,muzzle.position);line.SetPosition(1,endpoint);line.startWidth=.022f;line.endWidth=.006f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;var fx=g.AddComponent<PlayerShotTrace>();fx.arena=player.arena;}
  }
  public bool Aim(Vector3 origin,Vector3 direction,float distance,out RaycastHit first){
   var hits=Physics.RaycastAll(origin,direction,distance,~(1<<30),QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
   foreach(var h in hits){if(h.collider.GetComponentInParent<FirstPersonExplorer>())continue;first=h;return true;}first=default;return false;
  }
  void Update(){
   if(!initialized||!player.Ready||player.Paused||!player.arena.Active)return;
   float dt=Time.deltaTime;cooldown=Mathf.Max(0,cooldown-dt);recoil=Mathf.Max(0,recoil-dt);melee=Mathf.Max(0,melee-dt);flash=Mathf.Max(0,flash-dt);hitMarker=Mathf.Max(0,hitMarker-dt);restDelay-=dt;if(restDelay<=0)Stamina=Mathf.Min(100,Stamina+19*dt);
   if(player.arena.Downed){ReloadRemaining=0;melee=0;flash=0;pendingMelee=false;expedition.MiningProgressReset();}
   else if(ReloadRemaining>0){ReloadRemaining-=dt;if(ReloadRemaining<=0){int load=Mathf.Min(12-Magazine,Reserve);Magazine+=load;Reserve-=load;ReloadRemaining=0;Sync();InteractionFeel.Play(FeelCue.Reload,.65f);}}
   if(pendingMelee){meleeDelay-=dt;if(meleeDelay<=0){pendingMelee=false;if(Ready)ResolveAttack(true);}}
   if(!player.Automated&&Cursor.lockState==CursorLockMode.Locked&&!player.arena.Downed){if(Input.GetMouseButton(0))Attack();if(Input.GetMouseButtonDown(1))Attack(true);if(Input.GetKeyDown(KeyCode.R))Reload();}
   target=null;if(Aim(player.viewCamera.transform.position,player.viewCamera.transform.forward,160,out var aim))target=aim.collider.GetComponentInParent<CreatureVitality>();
   weaponClock+=dt;float walking=InteractionFeel.ReducedMotion?0:Mathf.Clamp01(player.Motor.velocity.magnitude/player.runSpeed);
   float mining=expedition.MiningProgress>0?Mathf.Sin(weaponClock*17)*.045f:0;reloadVisual=Mathf.Lerp(reloadVisual,ReloadRemaining>0?1:0,1-Mathf.Exp(-dt*13));
   Vector3 bob=new Vector3(Mathf.Sin(weaponClock*6)*.006f,Mathf.Cos(weaponClock*12)*.004f,0)*walking;
   toolVisual=Mathf.Lerp(toolVisual,melee>0||expedition.MiningProgress>0?1:0,1-Mathf.Exp(-dt*18));
   rifle.localPosition=rifleRest+bob+new Vector3(0,-reloadVisual*.16f-toolVisual*.44f,-recoil*.8f);rifle.localRotation=Quaternion.Euler(-recoil*85+toolVisual*18,reloadVisual*9,-reloadVisual*29);
   tool.gameObject.SetActive(melee>0||expedition.MiningProgress>0);tool.localPosition=toolRest+new Vector3(-Mathf.Sin(melee/.45f*Mathf.PI)*.35f,mining,0);tool.localRotation=Quaternion.Euler(-Mathf.Sin(melee/.45f*Mathf.PI)*75+mining*300,0,0);
   muzzleLight.intensity=flash>0?3:0;
  }
  void OnGUI(){
   if(!initialized||!player.Ready||player.Paused)return;var matrix=GUI.matrix;float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale;GUI.depth=-24;
   var light=new Color(.82f,.91f,.93f,.85f);var center=new Vector2(w*.5f,450);
   InteractionFeel.Text(new Rect(w-195,791,150,40),Magazine+" / "+Reserve,player.uiFont,20,Magazine==0?new Color(1,.47f,.22f):light,TextAnchor.MiddleRight);
   if(ReloadRemaining>0){InteractionFeel.Ring(center,22,1-ReloadRemaining/2.35f,new Color(.65f,.85f,.88f,.8f),2);InteractionFeel.Text(new Rect(w-195,825,150,25),"换弹",player.uiFont,12,light,TextAnchor.MiddleRight);}
   if(Stamina<98){GUI.color=new Color(.03f,.06f,.08f,.6f);GUI.DrawTexture(new Rect(44,831,132,3),Texture2D.whiteTexture);GUI.color=Stamina<25?new Color(.96f,.4f,.18f):new Color(.37f,.7f,.8f);GUI.DrawTexture(new Rect(44,831,132*Stamina/100,3),Texture2D.whiteTexture);GUI.color=Color.white;}
   if(expedition.Inventory&&expedition.NoticeAlpha>0)InteractionFeel.Text(new Rect(w-330,36,285,28),"矿石 "+expedition.Inventory.UnknownCount+"   燃料 "+expedition.Inventory.FuelCount,player.uiFont,14,new Color(.82f,.91f,.93f,expedition.NoticeAlpha),TextAnchor.MiddleRight);
   if(target&&!target.Dead){
    Vector3 point=target.transform.position+Vector3.up*(target.GetComponent<SkyTitan>()?20:4);var titan=target.GetComponent<SkyTitan>();if(titan)point=titan.head.position+Vector3.up*10;
    var viewport=player.viewCamera.WorldToViewportPoint(point);float x=Mathf.Clamp(viewport.x*w,160,w-160),y=Mathf.Clamp((1-viewport.y)*900,100,670);
    InteractionFeel.Text(new Rect(x-140,y-30,280,24),target.DisplayName,player.uiFont,12,light,TextAnchor.MiddleCenter);GUI.color=new Color(.07f,.08f,.09f,.65f);GUI.DrawTexture(new Rect(x-52,y,104,3),Texture2D.whiteTexture);GUI.color=new Color(.91f,.38f,.17f);GUI.DrawTexture(new Rect(x-52,y,104*target.Health/(float)target.maxHealth,3),Texture2D.whiteTexture);GUI.color=Color.white;
   }
   if(hitMarker>0)InteractionFeel.Text(new Rect(center.x-20,430,40,40),"×",player.uiFont,26,new Color(1,.85f,.55f,Mathf.Clamp01(hitMarker*10)),TextAnchor.MiddleCenter);
   if(!string.IsNullOrEmpty(expedition.Prompt))InteractionFeel.Text(new Rect(center.x-210,494,420,28),expedition.Prompt,player.uiFont,14,light,TextAnchor.MiddleCenter);
   if(expedition.MiningProgress>0)InteractionFeel.Ring(center,20,expedition.MiningProgress,new Color(.38f,.86f,.87f,.9f),2);
   if(!string.IsNullOrEmpty(expedition.Notice))InteractionFeel.Text(new Rect(center.x-240,568-(1-expedition.NoticeAlpha)*18,480,30),expedition.Notice,player.uiFont,17,new Color(.9f,.94f,.9f,expedition.NoticeAlpha),TextAnchor.MiddleCenter);
   GUI.matrix=matrix;GUI.color=Color.white;
  }

 }
 public sealed class PlayerShotTrace:MonoBehaviour {public CreatureArena arena;float t;void Update(){if(!arena||!arena.Active)return;t+=Time.deltaTime;if(t>.055f)Destroy(gameObject);}}
}
