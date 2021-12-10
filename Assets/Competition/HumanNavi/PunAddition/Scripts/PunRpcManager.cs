using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using SIGVerse.Common;
using SIGVerse.Competition.HumanNavigation;

#if SIGVERSE_PUN
using Photon.Pun;
#endif

namespace SIGVerse.Competition.HumanNavigation
{

#if SIGVERSE_PUN

	public class PunRpcManager : MonoBehaviour
	{
		//-----------------------------

		public GameObject moderator;

		private PhotonView photonView;

		public SAPIVoiceSynthesisExternal tts;

		public PunLauncher punLauncher;

		void Awake()
		{
		}

		void Start()
		{
			this.photonView = this.GetComponent<PhotonView>();
		}

//		public void ForwardSubRosMessage(RosBridge.human_navigation.HumanNaviMsg humanNaviMsg)
//		{
//			this.photonView.RPC(nameof(ForwardSubRosMessageRPC), RpcTarget.All, humanNaviMsg.message, humanNaviMsg.detail);
//		}

//		[PunRPC]
//		private void ForwardSubRosMessageRPC(string message, string detail, PhotonMessageInfo info)
//		{
//			Debug.LogWarning("ForwardSubRosMessageRPC msg="+message);

//			// Forward the message 
////			foreach (GameObject destination in this.destinations)
//			{
//				ExecuteEvents.Execute<IReceiveHumanNaviMsgHandler>
//				(
////					target: destination,
//					target: this.moderator,
//					eventData: null,
//					functor: (reciever, eventData) => reciever.OnReceiveRosMessage(new RosBridge.human_navigation.HumanNaviMsg(message, detail))
//				);
//			}
//		}

//		public void ForwardObjectGrasp(string objectName)
//		{
//			this.photonView.RPC(nameof(ForwardObjectGraspRPC), RpcTarget.All, objectName);
//		}

//		[PunRPC]
//		private void ForwardObjectGraspRPC(string objectName, PhotonMessageInfo info)
//		{
//			this.punLauncher.DesableRigidbody(objectName);
//		}

//		public void ForwardTargetObjectGrasp(string graspedObjectName)
//		{
//			this.photonView.RPC(nameof(ForwardTargetObjectGraspRPC), RpcTarget.All, graspedObjectName);
//		}

//		[PunRPC]
//		private void ForwardTargetObjectGraspRPC(string graspedObjectName, PhotonMessageInfo info)
//		{
//			//this.moderator.GetComponent<HumanNaviModerator>().TargetGrasp(graspedObjectName);
//		}
	}
#endif
}

