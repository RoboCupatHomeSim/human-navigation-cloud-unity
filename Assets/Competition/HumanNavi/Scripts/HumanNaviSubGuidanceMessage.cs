using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.RosBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;
using Photon.Pun;

namespace SIGVerse.Competition.HumanNavigation
{
	public enum GuidanceMessageDisplayType
	{
		All,
		RobotOnly,
		AvatarOnly,
		None,
	}

	//public interface IReceiveHumanNaviGuidanceMsgHandler : IEventSystemHandler
	//{
	//	void OnReceiveROSHumanNaviGuidanceMessage(RosBridge.human_navigation.HumanNaviGuidanceMsg guidaneMsg);
	//}

	public class HumanNaviSubGuidanceMessage : RosSubMessage<RosBridge.human_navigation.HumanNaviGuidanceMsg>
	{
		//public SAPIVoiceSynthesis tts;
		public SAPIVoiceSynthesisExternal tts;

		private PhotonView photonView;

		//--------------------------------------------------

		void Awake()
		{
			this.photonView = this.GetComponent<PhotonView>();
		}

		protected override void SubscribeMessageCallback(RosBridge.human_navigation.HumanNaviGuidanceMsg guidaneMsg)
		{
			SIGVerseLogger.Info("Received guide message: " + guidaneMsg.message + ", display type: " + guidaneMsg.display_type + ", source lang: " + guidaneMsg.source_language + ", target lang: " + guidaneMsg.target_language);

			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.NotUsed)
			{
				this.tts.OnReceiveROSHumanNaviGuidanceMessage(guidaneMsg);
				
			}
			else if(HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunServer)
			{
				this.photonView.RPC(nameof(ForwardSubGuidanceMessageRPC), RpcTarget.Others, guidaneMsg.message, guidaneMsg.display_type, guidaneMsg.source_language, guidaneMsg.target_language);
			}
		}

		[PunRPC]
		private void ForwardSubGuidanceMessageRPC(string message, string display_type, string source_language, string target_language, PhotonMessageInfo info)
		{
//			Debug.LogWarning("ForwardSubGuidanceMessageRPC msg="+message);

			this.tts.OnReceiveROSHumanNaviGuidanceMessage(new RosBridge.human_navigation.HumanNaviGuidanceMsg(message, display_type, source_language, target_language));
		}
	}
}
