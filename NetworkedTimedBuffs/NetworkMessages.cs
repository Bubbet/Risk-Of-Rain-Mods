using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine.Networking;

namespace NetworkedTimedBuffs
{
	public class SyncTimedBuffAdd : INetMessage
	{
		public NetworkInstanceId netID;
		public float timer;
		public BuffIndex buffIndex;

		public SyncTimedBuffAdd(NetworkInstanceId networkIdentityNetId, BuffIndex itemBuffIndex, float itemTimer)
		{
			netID = networkIdentityNetId;
			buffIndex = itemBuffIndex;
			timer = itemTimer;
		}

		public SyncTimedBuffAdd(){}

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(netID);
			writer.WritePackedUInt32((uint) buffIndex);
			writer.Write(timer);
		}

		public void Deserialize(NetworkReader reader)
		{
			netID = reader.ReadNetworkId();
			buffIndex = (BuffIndex) reader.ReadPackedUInt32();
			timer = reader.ReadSingle();
		}

		public void OnReceived()
		{
			if (NetworkServer.active) return;
			var body = Util.FindNetworkObject(netID).GetComponent<CharacterBody>();
			body.timedBuffs.Add(new CharacterBody.TimedBuff {buffIndex = buffIndex, timer = timer});
		}
	}
	
	public class SyncTimedBuffRemove : INetMessage
	{
		public NetworkInstanceId netID;
		public int index;
		public SyncTimedBuffRemove(){}

		public SyncTimedBuffRemove(NetworkInstanceId networkIdentityNetId, int index)
		{
			netID = networkIdentityNetId;
			this.index = index;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(netID);
			writer.Write(index);
		}

		public void Deserialize(NetworkReader reader)
		{
			netID = reader.ReadNetworkId();
			index = reader.ReadInt32();
		}

		public void OnReceived()
		{
			if (NetworkServer.active) return;
			var body = Util.FindNetworkObject(netID).GetComponent<CharacterBody>();
			body.timedBuffs.RemoveAt(index); //throwing out of range
		}
	}

	public class SyncTimedBuffUpdate : INetMessage
	{
		public NetworkInstanceId netID;
		public int index;
		public float timer;
		public SyncTimedBuffUpdate(){}

		public SyncTimedBuffUpdate(NetworkInstanceId networkIdentityNetId, int index, float duration)
		{
			netID = networkIdentityNetId;
			this.index = index;
			timer = duration;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(netID);
			writer.Write(index);
			writer.Write(timer);
		}

		public void Deserialize(NetworkReader reader)
		{
			netID = reader.ReadNetworkId();
			index = reader.ReadInt32();
			timer = reader.ReadSingle();
		}

		public void OnReceived()
		{
			if (NetworkServer.active) return;
			var obj = Util.FindNetworkObject(netID);
			if (!obj) return;
			var body = obj.GetComponent<CharacterBody>();
			if (!body) return;
			body.timedBuffs[index].timer = timer;
		}
	}
}