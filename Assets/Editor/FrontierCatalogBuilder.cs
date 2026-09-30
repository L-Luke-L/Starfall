using UnityEditor;
using UnityEngine;
using Starfall;

public static class FrontierCatalogBuilder {
 static ItemStack S(ItemId id,int n)=>new ItemStack(id,n);
 public static void Generate(){
  const string path="Assets/Resources/FrontierCatalog.asset";var c=AssetDatabase.LoadAssetAtPath<FrontierCatalog>(path);if(!c){c=ScriptableObject.CreateInstance<FrontierCatalog>();AssetDatabase.CreateAsset(c,path);}
  c.projects=new[]{
   new FrontierCatalog.Project{id="carrier",title="远行货架",requirement="engineer-rescued",effect="背包扩至24格；死亡后容量保留",cost=new[]{S(ItemId.IronIngot,2),S(ItemId.CopperIngot,2),S(ItemId.MachineParts,6)}},
   new FrontierCatalog.Project{id="tactical",title="战术整备台",requirement="engineer-rescued",effect="制作爆裂芯与声纹诱饵",cost=new[]{S(ItemId.CopperIngot,2),S(ItemId.CobaltIngot,1),S(ItemId.MachineParts,5)}},
   new FrontierCatalog.Project{id="culture",title="药用培养槽",requirement="greenhouse",effect="把培养材料制成急救剂",cost=new[]{S(ItemId.PurpleIngot,2),S(ItemId.MachineParts,4),S(ItemId.Fuel,1)}},
   new FrontierCatalog.Project{id="archive",title="余烬档案台",requirement="drone-rescued",effect="营地实体终端；浏览勘测与委托",cost=new[]{S(ItemId.CopperIngot,1),S(ItemId.MachineParts,3)}}
  };
  c.commissions=new[]{
   new FrontierCatalog.Commission{id="miner",title="埋藏的明天",eventId="mining",goal=6,reward=new[]{S(ItemId.MachineParts,4),S(ItemId.Fuel,2)}},
   new FrontierCatalog.Commission{id="smith",title="落锤之后",eventId="forging",goal=5,reward=new[]{S(ItemId.MachineParts,5),S(ItemId.Ammo,18)}},
   new FrontierCatalog.Commission{id="defender",title="守住回程",eventId="kill",goal=4,reward=new[]{S(ItemId.MachineParts,4),S(ItemId.Medkit,1)}},
   new FrontierCatalog.Commission{id="surveyor",title="夜空的坐标",eventId="survey",goal=3,reward=new[]{S(ItemId.MachineParts,5),S(ItemId.Fuel,2)}},
   new FrontierCatalog.Commission{id="archaeologist",title="遗迹仍在回答",eventId="ruin",goal=2,reward=new[]{S(ItemId.MachineParts,6),S(ItemId.CobaltIngot,1)}},
   new FrontierCatalog.Commission{id="tracker",title="变异的边界",eventId="variant",goal=2,reward=new[]{S(ItemId.MachineParts,6),S(ItemId.Medkit,2)}}
  };
  c.landmarks=new[]{
   new FrontierCatalog.Landmark{region=1,title="折翼航标",position=new Vector3(-24,0,24),story="岩道背后的星光依然指向归途。航标的最后一次转动，替后来者避开了断崖。"},
   new FrontierCatalog.Landmark{region=2,title="无声的撤离线",position=new Vector3(22,0,-27),story="沙尘掩去了车辙，却没有掩去并排的脚印。有人曾在这里等最后一班车。"},
   new FrontierCatalog.Landmark{region=3,title="蓝息温床",position=new Vector3(-16,0,33),story="结晶之间留有细微呼吸。冷光保护着温室最深处的一枚孢子。"},
   new FrontierCatalog.Landmark{region=4,title="停泊的巨影",position=new Vector3(-25,0,-29),story="舰腹投下的阴影像一座山。维修人员把孩子画的太阳留在了最后的焊缝旁。"},
   new FrontierCatalog.Landmark{region=5,title="黑石星环",position=new Vector3(25,0,25),story="星环内的碎片仍按旧日轨迹运行。机器忘了战争结束，夜空却从未忘记。"}
  };
  c.ruins=new[]{
   new FrontierCatalog.Ruin{region=2,title="方向锁",position=new Vector3(-4,0,17),solution=new[]{1,3,2},reward=new[]{S(ItemId.IronIngot,2),S(ItemId.MachineParts,3)}},
   new FrontierCatalog.Ruin{region=3,title="结晶记忆",position=new Vector3(18,0,14),solution=new[]{0,2,1,3},reward=new[]{S(ItemId.CobaltIngot,2),S(ItemId.MachineParts,3)}},
   new FrontierCatalog.Ruin{region=4,title="余载配平",position=new Vector3(-20,0,-17),solution=new[]{3,1,2},reward=new[]{S(ItemId.StarIngot,1),S(ItemId.MachineParts,3)}}
  };
  c.recipes=new[]{
   new FrontierCatalog.Recipe{id="blast",title="爆裂芯 ×2",requirement="build-tactical",cost=new[]{S(ItemId.Fuel,1),S(ItemId.MachineParts,3)},result=S(ItemId.BlastCore,2)},
   new FrontierCatalog.Recipe{id="decoy",title="声纹诱饵 ×2",requirement="build-tactical",cost=new[]{S(ItemId.MachineParts,2)},result=S(ItemId.SoundDecoy,2)},
   new FrontierCatalog.Recipe{id="medicine",title="急救剂 ×1",requirement="build-culture",cost=new[]{S(ItemId.PurpleOre,2),S(ItemId.Fuel,1)},result=S(ItemId.Medkit,1)}
  };
  c.species=new[]{
   new FrontierCatalog.Species{id="mauler",title="玄脊地龙",detail="冲撞前会压低头部。横向闪避，趁恢复期靠近侧腹。"},
   new FrontierCatalog.Species{id="flyer",title="苍翼飞龙",detail="喷射前翼膜收紧。实体岩石能够拦下弹道。"},
   new FrontierCatalog.Species{id="titan",title="蚀穹古龙",detail="巨响之后观察地面预警。攻击结束的空窗才适合反击。"},
   new FrontierCatalog.Species{id="armored",title="玄甲地龙",detail="正面装甲厚重。诱导冲撞实体掩体，等待侧腹暴露。"},
   new FrontierCatalog.Species{id="thunder",title="雷冠翼龙",detail="落雷有三段锁定预警。保持移动，停栖时寻找弱点。"},
   new FrontierCatalog.Species{id="magnetic",title="磁蚀地龙",detail="冲撞路径留下紫色磁痕，亮起前离开；诱饵不能打断已起手的冲撞。"},
   new FrontierCatalog.Species{id="frost",title="霜环翼龙",detail="喷射后锁定一片地面。冰环亮起前撤出，可避免生命与体力同时流失。"}
  };EditorUtility.SetDirty(c);
 }
}
