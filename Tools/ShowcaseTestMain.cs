using System;
using System.Reflection;
using Starfall;

// Pure managed checks against the real runtime assembly; no Unity process or save is opened.
public static class ShowcaseTestMain {
 static int failures;
 static void Check(string name,bool ok){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 public static int Main(){
  var runtime=typeof(CampaignRuntime);
  Check("Showcase can be entered and exited through public runtime API",runtime.GetMethod("StartShowcase",Type.EmptyTypes)!=null&&runtime.GetMethod("StopShowcase",Type.EmptyTypes)!=null);
  Check("Showcase state is observable for UI and runtime verification",runtime.GetProperty("ShowcaseActive")!=null&&runtime.GetProperty("ShowcaseElapsed")!=null&&runtime.GetProperty("ShowcasePaused")!=null);
  Check("Original mine progression still has five distinct coordinate fragments",CampaignRules.NodeCount(1)==12&&CampaignRules.NodeCount(2)==12&&CampaignRules.NodeCount(3)==8&&CampaignRules.NodeCount(4)==10&&CampaignRules.NodeCount(5)==12);
  int[] totals=new int[14];for(int layer=1;layer<=5;layer++)for(int node=0;node<CampaignRules.NodeCount(layer);node++)totals[(int)CampaignRules.NodeItem(layer,node)]+=CampaignRules.NodeQuantity(layer,node);
  Check("Mine retains original complete resource totals",totals[0]==30&&totals[1]==12&&totals[2]==8&&totals[3]==8&&totals[4]==10&&totals[13]==12);
  int[] costs=new int[14];for(int part=0;part<4;part++)foreach(var stack in CampaignRules.RepairCost(part))costs[(int)stack.item]+=stack.count;foreach(var stack in CampaignRules.BombCost)costs[(int)stack.item]+=stack.count;
  Check("Full ship and bomb can still be built from original mine resources",costs[5]<=totals[0]&&costs[6]<=totals[1]&&costs[7]<=totals[2]&&costs[8]<=totals[3]&&costs[9]<=totals[4]&&costs[13]<=totals[13]);
  Check("Both ending gates remain available",runtime.GetProperty("EscapeReady")!=null&&runtime.GetProperty("SavingReady")!=null&&runtime.GetMethod("Escape")!=null&&runtime.GetMethod("InstallBomb")!=null&&runtime.GetMethod("BeginDetonation")!=null&&runtime.GetMethod("Evacuate")!=null);
  Check("Demo does not replace original three-strike forge API",typeof(ForgeController).GetMethod("SubmitStrike")!=null&&typeof(ForgeController).GetProperty("StrikeReady")!=null);
  Console.WriteLine("SHOWCASE_MANAGED "+(failures==0?"PASS":"FAIL "+failures));return failures==0?0:2;
 }
}
