using UnityEditor;
using UnityEngine;
using Starfall;

public static class VoyageCatalogBuilder {
 public static void Generate(){
  var catalog=AssetDatabase.LoadAssetAtPath<LostRealmCatalog>("Assets/Resources/LostRealmCatalog.asset");
  catalog.relays=new[]{
   new LostRealmCatalog.Relay{region=2,title="战场 · 撤离信号",seconds=24,story="断桥两端的信号重新合拍。最后的撤离编号终于抵达另一端。",anchors=new[]{new Vector3(-12,0,-20),new Vector3(-17,0,-5),new Vector3(-10,0,11)}},
   new LostRealmCatalog.Relay{region=3,title="医疗站 · 生命回声",seconds=26,story="三份独立监测记录指向同一片种源。它们在停电之后仍然活着。",anchors=new[]{new Vector3(13,0,-23),new Vector3(22,0,-9),new Vector3(10,0,1)}},
   new LostRealmCatalog.Relay{region=4,title="船坞 · 舰队余波",seconds=25,story="货运信号与战前星图重叠。拾光找到了一种无需伤害生命的防卫频率。",anchors=new[]{new Vector3(-12,0,-22),new Vector3(-10,0,-5),new Vector3(-16,0,8)}}
  };
  catalog.relics=new[]{
   new LostRealmCatalog.Relic{region=1,title="褪色航图",story="边角写着一行小字：每次回家，都把新的星星画上去。"},
   new LostRealmCatalog.Relic{region=2,title="撤离铭牌",story="姓名已磨平，背面的家庭编号依然清楚。它曾被某个人攥得很紧。"},
   new LostRealmCatalog.Relic{region=3,title="玻璃种匣",story="匣内保存着一粒蓝色种子。苏禾说，留下它的人相信会有下一次春天。"},
   new LostRealmCatalog.Relic{region=4,title="旧舰队徽章",story="徽章上的誓言早于战争：保护远航者，守住他们回家的路。"},
   new LostRealmCatalog.Relic{region=5,title="核心记忆晶",story="最后一条记录没有坐标，只有一幅安静的夜空。五件遗物能拼出它的来处。"}
  };
  EditorUtility.SetDirty(catalog);
 }
}
