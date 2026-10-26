using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Starfall {
 // Opt-in visual smoke test. It is added at runtime, never serialized into a scene.
 public sealed class InterfaceVerification:MonoBehaviour {
  static bool Requested=>Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-interfaceQA");
  readonly List<string> results=new List<string>(),errors=new List<string>();
  CampaignRuntime campaign;SceneDirector director;string folder;int failures,captures,captureSerial;
  bool capturePending,captureReady;int repaintFrame=-1;
  readonly Dictionary<string,HashSet<ulong>> frameHashes=new Dictionary<string,HashSet<ulong>>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){if(Requested&&!FindFirstObjectByType<InterfaceVerification>()){var root=new GameObject("Interface verification · opt in");DontDestroyOnLoad(root);root.AddComponent<InterfaceVerification>();}}
  void Check(string name,bool pass){results.Add((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;Debug.Log("INTERFACE_QA "+results[results.Count-1]);}
  void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
  static void Set(object target,string field,object value){var info=target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);if(info==null)throw new MissingFieldException(target.GetType().Name,field);info.SetValue(target,value);}
  static T Get<T>(object target,string field){var info=target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);if(info==null)throw new MissingFieldException(target.GetType().Name,field);return (T)info.GetValue(target);}
  static T Invoke<T>(object target,string method,params object[] args){var info=target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic);if(info==null)throw new MissingMethodException(target.GetType().Name,method);return (T)info.Invoke(target,args);}
  IEnumerator Start(){
   if(!Requested)yield break;Application.runInBackground=true;folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"../QA/Reports/Interface":"../../../QA/Reports/Interface"));Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;
   yield return new WaitForSecondsRealtime(2);campaign=CampaignRuntime.Current;director=FindFirstObjectByType<SceneDirector>();Check("Campaign runtime loaded using isolated QA state",campaign&&campaign.IsQA&&director);
   if(campaign&&campaign.IsQA&&director){var run=Run();while(true){object value=null;bool more;try{more=run.MoveNext();if(more)value=run.Current;}catch(Exception e){Check("Unhandled interface test: "+e,false);break;}if(!more)break;yield return value;}}
   if(campaign&&campaign.Modal)campaign.Close();Check("No runtime errors during interface rendering",errors.Count==0);File.WriteAllLines(Path.Combine(folder,"interface-report.txt"),results);if(errors.Count>0)File.WriteAllLines(Path.Combine(folder,"runtime-errors.txt"),errors);
   Debug.Log(failures==0?"STARFALL_INTERFACE_QA_PASS":"STARFALL_INTERFACE_QA_FAIL "+failures);Application.logMessageReceived-=Log;Application.Quit(failures==0?0:2);
  }
  IEnumerator Run(){
   string[] skin={"Panel","Inset","Slot","Button","ProgressBack","ProgressFill","IconBag","IconGear","IconAmmo","IconMedkit","IconCore","IconDecoy","IconCheck","IconLock","IconWrench","IconStar","IconBook","IconFuel","IconIngots","IconFlag","IconShield","IconFlask"};
   bool loaded=true;foreach(var name in skin)loaded&=Resources.Load<Texture2D>("UI/KenneySciFi/"+name)!=null;Check("All 22 Kenney CC0 skin and icon textures load",loaded);Check("Existing mineral artwork remains available",campaign.mineralSprites!=null&&campaign.mineralSprites.Length>=5&&campaign.mineralSprites[0]);
   VerifyInventoryAndMenus();
   campaign.Close();Set(campaign,"inventoryFilter",0);campaign.State.bag=new ItemContainer(32);campaign.State.warehouse=new ItemContainer(48);
   for(int i=0;i<17;i++){campaign.State.bag.Add((ItemId)i,i==12?3:i%3==0?24:7+i);campaign.State.warehouse.Add((ItemId)i,10+i*3);}
   campaign.State.unlockedLayer=3;campaign.State.coordinates.Clear();campaign.State.coordinates.Add(1);campaign.State.coordinates.Add(2);campaign.State.repairs.Clear();campaign.State.repairs.Add("0");
   for(int i=1;i<=8;i++)campaign.State.Learn("J"+i.ToString("D2"),Math.Min(5,i));
   float scale=Time.timeScale;var cursor=Cursor.lockState;bool visible=Cursor.visible;Vector3 original=director.player.position;
   // Hidden D3D11 windows can initially skip OnGUI entirely. Initialize several
   // real swapchain sizes and require a proven UI frame before formal captures.
   yield return Warmup();if(!captureReady)yield break;
   int[,] sizes={{960,720},{1920,800},{1280,720},{1600,900}};
   for(int size=0;size<sizes.GetLength(0);size++){
    yield return Resize(sizes[size,0],sizes[size,1]);
    string suffix=sizes[size,0]+"x"+sizes[size,1];
    director.player.position=new Vector3(director.camp.transform.position.x+12,director.surfaceY,original.z);campaign.Open("bag");Set(campaign,"bagPage",0);Check("Opening inventory pauses gameplay at "+suffix,campaign.Modal&&Mathf.Approximately(Time.timeScale,0));yield return Capture("01-backpack-"+suffix,sizes[size,0],sizes[size,1]);campaign.Close();
    campaign.Open("bag");Set(campaign,"bagPage",1);yield return Capture("07-backpack-page-two-"+suffix,sizes[size,0],sizes[size,1]);Check("Backpack page two remains in range at "+suffix,Get<int>(campaign,"bagPage")==1);campaign.Close();
    director.player.position=new Vector3(director.camp.transform.position.x,director.surfaceY,original.z);campaign.Open("camp");Set(campaign,"bagPage",0);Set(campaign,"warehousePage",0);yield return Capture("02-camp-storage-"+suffix,sizes[size,0],sizes[size,1]);
    Set(campaign,"selected",campaign.State.bag.stacks.FindIndex(x=>x.item==ItemId.Medkit));Set(campaign,"fromWarehouse",false);yield return Capture("03-medkit-details-"+suffix,sizes[size,0],sizes[size,1]);campaign.Close();
    campaign.Open("journal");Set(campaign,"tab",0);Set(campaign,"terminalSelection",0);yield return Capture("04-task-tracking-"+suffix,sizes[size,0],sizes[size,1]);
    Set(campaign,"tab",1);Set(campaign,"selectedJournal",0);yield return Capture("05-journal-long-text-"+suffix,sizes[size,0],sizes[size,1]);campaign.Close();
    campaign.Open("journal");Set(campaign,"tab",1);Set(campaign,"selectedJournal",0);Set(campaign,"journalScroll",new Vector2(0,10000));yield return Capture("08-journal-scroll-end-"+suffix,sizes[size,0],sizes[size,1],false);var scroll=Get<Vector2>(campaign,"journalScroll");Check("Journal scrolling clamps to actual content at "+suffix,scroll.y>=0&&scroll.y<10000);results.Add("METRIC journalScrollEnd "+suffix+" y="+scroll.y.ToString("F2"));campaign.Close();
    campaign.Open("frontier");Set(campaign,"frontierTab",1);yield return Capture("06-commissions-"+suffix,sizes[size,0],sizes[size,1]);campaign.Close();
    Check("Closing menus restores pause and cursor at "+suffix,!campaign.Modal&&Mathf.Approximately(Time.timeScale,scale)&&Cursor.lockState==cursor&&Cursor.visible==visible);
   }
   director.player.position=original;Check("Thirty-two nonempty current OnGUI screenshots captured",captures==32);
  }
  void VerifyInventoryAndMenus(){
   Vector3 original=director.player.position;float originalScale=Time.timeScale;
   var bag=new ItemContainer(4);var warehouse=new ItemContainer(3);bag.Add(ItemId.IronOre,17);
   Check("Split preserves an odd item quantity and creates two stacks",bag.Split(0)&&bag.Count(ItemId.IronOre)==17&&bag.stacks.Count==2&&bag.stacks[0].count==9&&bag.stacks[1].count==8);
   Check("Partial transfer preserves combined quantities",bag.TransferTo(warehouse,ItemId.IronOre,5)&&bag.Count(ItemId.IronOre)==12&&warehouse.Count(ItemId.IronOre)==5);
   Check("Reverse transfer restores quantities",warehouse.TransferTo(bag,ItemId.IronOre,3)&&bag.Count(ItemId.IronOre)==15&&warehouse.Count(ItemId.IronOre)==2);
   bag.Sort();Check("Sorting merges split stacks without losing items",bag.Count(ItemId.IronOre)==15&&bag.stacks.Count==1);
   var full=new ItemContainer(1);full.Add(ItemId.Fuel,99);string prior=JsonUtility.ToJson(bag);
   Check("Full destination rejects transfer atomically",!bag.TransferTo(full,ItemId.IronOre,4)&&JsonUtility.ToJson(bag)==prior&&full.Count(ItemId.Fuel)==99);
   Check("Zero, negative, excessive and self transfers are rejected",!bag.TransferTo(warehouse,ItemId.IronOre,0)&&!bag.TransferTo(warehouse,ItemId.IronOre,-1)&&!bag.TransferTo(warehouse,ItemId.IronOre,100)&&!bag.TransferTo(bag,ItemId.IronOre,1)&&JsonUtility.ToJson(bag)==prior);
   Check("A full container cannot split or lose its original stack",!full.Split(0)&&full.stacks.Count==1&&full.Count(ItemId.Fuel)==99);
   campaign.State.bag=new ItemContainer(4);campaign.State.warehouse=new ItemContainer(4);campaign.State.bag.Add(ItemId.Medkit,2);campaign.State.health=100;
   Check("Medical item at full health is not consumed",!campaign.UseMedkit()&&campaign.State.bag.Count(ItemId.Medkit)==2);
   campaign.State.health=63;Check("Medical item heals 25 and consumes exactly one",campaign.UseMedkit()&&campaign.State.health==88&&campaign.State.bag.Count(ItemId.Medkit)==1);
   campaign.State.health=94;Check("Medical item clamps to full health",campaign.UseMedkit()&&campaign.State.health==100&&campaign.State.bag.Count(ItemId.Medkit)==0);
   campaign.State.Flag("ammo-recipe");campaign.State.bag=new ItemContainer(1);campaign.State.bag.Add(ItemId.PurpleOre,3);campaign.State.warehouse=new ItemContainer(4);campaign.State.warehouse.Add(ItemId.Fuel,1);
   director.player.position=new Vector3(director.camp.transform.position.x,director.surfaceY,original.z);
   Check("At camp crafting rejects full output without spending ingredients",campaign.AtCamp&&!campaign.CraftAmmo()&&campaign.State.bag.Count(ItemId.PurpleOre)==3&&campaign.State.warehouse.Count(ItemId.Fuel)==1);
   campaign.State.bag.Remove(ItemId.PurpleOre,1);director.player.position+=Vector3.right*12;
   Check("Warehouse crafting is unavailable away from camp",!campaign.AtCamp&&!campaign.CraftAmmo()&&campaign.State.bag.Count(ItemId.PurpleOre)==2&&campaign.State.warehouse.Count(ItemId.Fuel)==1);
   var crate=new SavedCrate{id="interface-only-permission-check",region=0};crate.items.Add(ItemId.Fuel,1);campaign.State.crates.Add(crate);
   Check("Camp crate cannot be taken remotely",!campaign.RecoverCrate(crate.id)&&crate.items.Count(ItemId.Fuel)==1);
   director.player.position-=Vector3.right*12;
   Check("At camp recipe uses warehouse and newly freed bag slot",campaign.CraftAmmo()&&campaign.State.bag.Count(ItemId.Ammo)==24&&campaign.State.bag.Count(ItemId.PurpleOre)==0&&campaign.State.warehouse.Count(ItemId.Fuel)==0);
   campaign.State.crates.Remove(crate);
   Time.timeScale=.37f;campaign.Open("bag");Set(campaign,"selected",0);
   Check("Opening a modal pauses a nondefault simulation speed",campaign.Modal&&Mathf.Approximately(Time.timeScale,0));
   campaign.Open("journal");Check("Switching menus clears item selection",Get<int>(campaign,"selected")==-1);campaign.Close();
   Check("Switching menus preserves the original simulation speed",!campaign.Modal&&Mathf.Approximately(Time.timeScale,.37f));Time.timeScale=originalScale;
   director.SetPaused(true);campaign.Open("journal");campaign.Close();Check("Closing a modal preserves an existing pause",director.IsPaused&&Mathf.Approximately(Time.timeScale,0));director.SetPaused(false);
   campaign.TrackObjective("quest:0");Check("Quest tracking supplies the current task hint",campaign.State.trackedObjective=="quest:0"&&!string.IsNullOrEmpty(campaign.TrackedHint));
   Check("Manual save round trip preserves selected tracking and item quantities",campaign.Save("interface-checkpoint")&&TrackedSaveMatches(campaign.Store.Load("interface-checkpoint")));
   campaign.TrackObjective("");Check("Returning tracking to main quest clears the side objective",campaign.State.trackedObjective==""&&campaign.TrackedHint=="");
   director.player.position=original;Time.timeScale=originalScale;
   VerifyInventorySelection();
   VerifySaveValidation();
  }
  void VerifyInventorySelection(){
   var matches=typeof(CampaignRuntime).GetMethod("InventoryMatches",BindingFlags.Static|BindingFlags.NonPublic);
   if(matches==null)throw new MissingMethodException("CampaignRuntime","InventoryMatches");
   bool categoriesComplete=true;
   for(int id=0;id<CampaignRules.ItemNames.Length;id++){
    int sections=0;for(int category=1;category<5;category++)if((bool)matches.Invoke(null,new object[]{(ItemId)id,category}))sections++;
    categoriesComplete&=(bool)matches.Invoke(null,new object[]{(ItemId)id,0})&&sections==1;
   }
   Check("All 17 item types belong to all-items and exactly one of four categories",categoriesComplete);
   Vector3 original=director.player.position;director.player.position=new Vector3(director.camp.transform.position.x,director.surfaceY,original.z);
   campaign.State.bag=new ItemContainer(8);campaign.State.bag.Add(ItemId.Medkit,3);campaign.State.bag.Add(ItemId.PurpleOre,12);campaign.State.bag.Add(ItemId.IronIngot,5);campaign.State.bag.Add(ItemId.AlienCrystal,4);campaign.State.bag.Add(ItemId.MachineParts,2);
   campaign.State.warehouse=new ItemContainer(1);campaign.State.warehouse.Add(ItemId.AlienCrystal,98);Set(campaign,"inventoryFilter",1);
   var indices=Invoke<List<int>>(campaign,"InventoryIndices",campaign.State.bag);
   Check("Filtered ore grid preserves actual stack indices instead of display positions",indices.Count==2&&indices[0]==1&&indices[1]==3);
   Set(campaign,"selected",indices[1]);Set(campaign,"fromWarehouse",false);
   Check("Real UI move transfers only available destination space and leaves the remainder",Invoke<bool>(campaign,"MoveInventorySelection",4)&&campaign.State.bag.Count(ItemId.AlienCrystal)==3&&campaign.State.warehouse.Count(ItemId.AlienCrystal)==99&&Get<int>(campaign,"selected")==-1);
   Check("Filtered item transfer never moves the item at the unfiltered display index",campaign.State.bag.Count(ItemId.PurpleOre)==12&&campaign.State.bag.Count(ItemId.Medkit)==3&&campaign.State.bag.Count(ItemId.IronIngot)==5);
   Set(campaign,"selected",3);string priorBag=JsonUtility.ToJson(campaign.State.bag),priorStore=JsonUtility.ToJson(campaign.State.warehouse);
   Check("Real UI full-destination transfer preserves items and selection",!Invoke<bool>(campaign,"MoveInventorySelection",3)&&JsonUtility.ToJson(campaign.State.bag)==priorBag&&JsonUtility.ToJson(campaign.State.warehouse)==priorStore&&Get<int>(campaign,"selected")==3);
   Set(campaign,"selected",0);Set(campaign,"fromWarehouse",true);
   Check("Real UI warehouse withdrawal supports partial selected quantities",Invoke<bool>(campaign,"MoveInventorySelection",50)&&campaign.State.warehouse.Count(ItemId.AlienCrystal)==49&&campaign.State.bag.Count(ItemId.AlienCrystal)==53);
   Set(campaign,"selected",0);priorBag=JsonUtility.ToJson(campaign.State.bag);priorStore=JsonUtility.ToJson(campaign.State.warehouse);director.player.position+=Vector3.right*12;
   Check("Real UI warehouse action is blocked away from camp",!Invoke<bool>(campaign,"MoveInventorySelection",10)&&JsonUtility.ToJson(campaign.State.bag)==priorBag&&JsonUtility.ToJson(campaign.State.warehouse)==priorStore);
   director.player.position-=Vector3.right*12;
   Check("Real UI transfer rejects zero and negative quantities",!Invoke<bool>(campaign,"MoveInventorySelection",0)&&!Invoke<bool>(campaign,"MoveInventorySelection",-1)&&JsonUtility.ToJson(campaign.State.bag)==priorBag&&JsonUtility.ToJson(campaign.State.warehouse)==priorStore);
   Set(campaign,"selected",99);Check("Real UI transfer rejects a stale selected stack",!Invoke<bool>(campaign,"MoveInventorySelection",1));
   Set(campaign,"inventoryFilter",3);indices=Invoke<List<int>>(campaign,"InventoryIndices",campaign.State.bag);Check("Supply category returns the actual medical stack",indices.Count==1&&indices[0]==0);
   Set(campaign,"inventoryFilter",4);indices=Invoke<List<int>>(campaign,"InventoryIndices",campaign.State.bag);Check("Component category returns the actual machine-parts stack",indices.Count==1&&indices[0]==4);
   Set(campaign,"inventoryFilter",0);Set(campaign,"selected",-1);Set(campaign,"fromWarehouse",false);director.player.position=original;
  }
  bool TrackedSaveMatches(CampaignState saved){return saved!=null&&saved.trackedObjective=="quest:0"&&saved.bag.Count(ItemId.Ammo)==24&&saved.warehouse.Count(ItemId.Fuel)==0;}
  void VerifySaveValidation(){
   var store=new CampaignStore(Path.Combine(folder,"state-fixtures"));var valid=CampaignState.New();valid.Learn("J01",1);store.Save(valid,"journal-region");
   var invalid=CampaignStore.Copy(valid);invalid.journal[0].region=99;store.Save(invalid,"journal-region");var recovered=store.Load("journal-region");
   Check("Invalid journal region recovers the safe backup before opening UI",recovered!=null&&recovered.journal[0].region==1&&store.RecoveredFromBackup);
  }
  IEnumerator Resize(int width,int height){
   Screen.SetResolution(width,height,FullScreenMode.Windowed);float deadline=Time.realtimeSinceStartup+3;
   while((Screen.width!=width||Screen.height!=height)&&Time.realtimeSinceStartup<deadline)yield return null;
   yield return new WaitForSecondsRealtime(.45f);yield return new WaitForEndOfFrame();
   Check("Window reached "+width+"x"+height,Screen.width==width&&Screen.height==height);
  }
  IEnumerator Warmup(){
   int[,] sizes={{1920,800},{960,720},{1280,720},{960,720}};
   campaign.Open("bag");capturePending=true;captureReady=false;
   for(int size=0;size<sizes.GetLength(0);size++){
    int width=sizes[size,0],height=sizes[size,1];captureSerial++;yield return Resize(width,height);bool ready=false;
    for(int attempt=0;attempt<3&&!ready;attempt++){
     yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Texture2D texture=null;
     try{
      texture=ScreenCapture.CaptureScreenshotAsTexture();ready=ValidFrame(texture,width,height,out string reason,out ulong hash);
      if(!ready&&attempt==0){
       string diagnostic="WARMUP "+width+"x"+height+" "+reason;results.Add(diagnostic);Debug.LogWarning(diagnostic);
       string directory=Path.Combine(folder,"Diagnostics");Directory.CreateDirectory(directory);
       if(texture)File.WriteAllBytes(Path.Combine(directory,"warmup-"+size+"-"+width+"x"+height+"-first-failure.png"),texture.EncodeToPNG());
      }
     }catch(Exception exception){Debug.LogWarning("Interface warmup: "+exception.Message);}
     finally{if(texture)Destroy(texture);}
    }
    captureReady=ready;
   }
   capturePending=false;campaign.Close();Check("Capture warmup ends with a current nonempty OnGUI frame",captureReady);
  }
  IEnumerator Capture(string name,int width,int height,bool distinct=true){
   capturePending=true;captureSerial++;bool captured=false;string reason="No rendered frame";
   for(int attempt=0;attempt<3&&!captured;attempt++){
    yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Texture2D texture=null;
    try{
     texture=ScreenCapture.CaptureScreenshotAsTexture();
     bool valid=ValidFrame(texture,width,height,out reason,out ulong hash);
     if(valid){
      string key=width+"x"+height;if(!frameHashes.TryGetValue(key,out var hashes)){hashes=new HashSet<ulong>();frameHashes.Add(key,hashes);}
      if(distinct&&!hashes.Add(hash)){reason="Current page pixels repeat an earlier page";valid=false;}
      else{
       var png=texture.EncodeToPNG();File.WriteAllBytes(Path.Combine(folder,name+".png"),png);captures++;captured=true;
       results.Add("METRIC "+name+" bytes="+png.Length+" contentHash="+hash.ToString("X16")+" attempts="+(attempt+1));
      }
     }
     if(!valid&&attempt==0){
      string diagnostics=Path.Combine(folder,"Diagnostics");Directory.CreateDirectory(diagnostics);
      if(texture)File.WriteAllBytes(Path.Combine(diagnostics,name+"-first-failure.png"),texture.EncodeToPNG());
      string detail="DIAGNOSTIC "+name+" "+reason+" capture="+captureSerial+" Screen="+Screen.width+"x"+Screen.height+" focused="+Application.isFocused+" graphics="+SystemInfo.graphicsDeviceType;
      results.Add(detail);Debug.LogWarning(detail);
     }
    }catch(Exception exception){reason=exception.Message;}
    finally{if(texture)Destroy(texture);}
   }
   capturePending=false;Check(name+" has current nonempty full OnGUI pixels"+(captured?"":" : "+reason),captured);
  }
  bool ValidFrame(Texture2D texture,int width,int height,out string reason,out ulong hash){
   reason="";hash=14695981039346656037UL;
   if(!texture){reason="ScreenCapture returned null";return false;}
   if(texture.width!=width||texture.height!=height){reason="Screenshot size differs from requested window";return false;}
   Color marker=texture.GetPixel(width-19,9),flippedMarker=texture.GetPixel(width-19,height-10),expected=MarkerColor();
   float scale=Mathf.Min(width/1600f,height/900f),offsetX=(width-1600*scale)*.5f,offsetY=(height-900*scale)*.5f;
   var pixels=texture.GetPixels32();var colors=new HashSet<int>();int bright=0,samples=0;
   for(int y=Mathf.RoundToInt(offsetY+180*scale);y<offsetY+760*scale;y+=3)for(int x=Mathf.RoundToInt(offsetX+170*scale);x<offsetX+1430*scale;x+=3){
    Color32 pixel=pixels[y*width+x];colors.Add(((pixel.r>>3)<<10)|((pixel.g>>3)<<5)|(pixel.b>>3));if(pixel.r>135&&pixel.g>135&&pixel.b>135)bright++;samples++;
    unchecked{hash^=(uint)(pixel.r|(pixel.g<<8)|(pixel.b<<16));hash*=1099511628211UL;}
   }
   // The campaign modal's full-screen alpha shade can composite after this
   // separate OnGUI behaviour. It dims the mark to ~22% while preserving hue.
   // Compare chroma-normalized colors; black/gray frames still fail >.10 chroma.
   var actualHue=NormalizedChroma(marker,out float chroma);var expectedHue=NormalizedChroma(expected,out float expectedChroma);
   string markerInfo=" markerActual="+marker.ToString("F3")+" expected="+expected.ToString("F3")+" chroma="+chroma.ToString("F3")+" normalized="+actualHue.ToString("F3")+" mirrored="+flippedMarker.ToString("F3")+" repaintFrame="+repaintFrame+" frame="+Time.frameCount+" colors="+colors.Count+" bright="+bright;
   if(chroma<=.10f||Mathf.Abs(actualHue.r-expectedHue.r)+Mathf.Abs(actualHue.g-expectedHue.g)+Mathf.Abs(actualHue.b-expectedHue.b)>.22f||repaintFrame<Time.frameCount-2){reason="Missing current OnGUI frame marker"+markerInfo;return false;}
   if(colors.Count<32||bright<Mathf.Max(40,samples/800)){reason="UI content is black, blank or missing text (colors="+colors.Count+", bright="+bright+")";return false;}
   return true;
  }
  static Color NormalizedChroma(Color color,out float chroma){float min=Mathf.Min(color.r,Mathf.Min(color.g,color.b)),max=Mathf.Max(color.r,Mathf.Max(color.g,color.b));chroma=max-min;return chroma>.0001f?new Color((color.r-min)/chroma,(color.g-min)/chroma,(color.b-min)/chroma,1):Color.black;}
  Color MarkerColor(){switch(captureSerial%6){case 0:return Color.red;case 1:return Color.green;case 2:return Color.blue;case 3:return Color.yellow;case 4:return Color.cyan;default:return Color.magenta;}}
  void OnGUI(){
   if(!capturePending||Event.current.type!=EventType.Repaint)return;
   // A tiny QA-only corner mark proves ScreenCapture includes the latest OnGUI
   // pass. A camera-only render or a stale/black swapchain cannot satisfy this.
   var prior=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.matrix=Matrix4x4.identity;GUI.depth=-10000;
   GUI.color=MarkerColor();GUI.DrawTexture(new Rect(Screen.width-22,Screen.height-12,6,6),Texture2D.whiteTexture);
   GUI.matrix=prior;GUI.color=color;GUI.depth=depth;repaintFrame=Time.frameCount;
  }
  void OnDestroy(){Application.logMessageReceived-=Log;}
 }
}
