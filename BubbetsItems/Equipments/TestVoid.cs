using RoR2;
using UnityEngine.Networking;

namespace BubbetsItems.Equipments
{
	public class TestVoid : EquipmentBase
	{
		public override bool RequiresSotv => true; // do automatic solution for this
		protected override void MakeTokens()
		{
			base.MakeTokens();
			VoidEquipmentManager.TransformationInfos.Add(new VoidEquipmentManager.TransformationInfo()
			{
				originalEquipment = RoR2Content.Equipment.Scanner.equipmentIndex,
				transformedEquipment = EquipmentDef.equipmentIndex
			});
		}
		
		public override EquipmentActivationState PerformEquipment(EquipmentSlot equipmentSlot)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "The equipment was activated."
			});
			return EquipmentActivationState.ConsumeStock;
		}
	}
}