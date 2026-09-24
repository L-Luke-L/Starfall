using UnityEngine;

namespace Starfall {
 public enum BuriedPayloadKind { RepairMaterial, DormantEncounter, StoryEvent }
 public enum RepairResource { FerricAlloy=0, CopperCompound=1, CobaltSilicate=2, RareComposite=3, PurpleCopper=4 }

 // Designer-only data. Never use these fields for unrecovered sample labels or appearance.
 [CreateAssetMenu(menuName="Starfall/Buried content definition")]
 public sealed class SubsurfaceDefinition : ScriptableObject {
  public string contentId;
  public BuriedPayloadKind payloadKind;
  public RepairResource repairResource;
  [Min(1)] public int materialQuantity=1;
  [Header("Reserved encounter integration · no combat implemented")]
  public string encounterId;
  public string fuelRewardId;
  [Header("Reserved story integration · no event implemented")]
  public string storyEventId;
 }
}
