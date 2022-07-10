using UnityEngine;
using System;
using System.Collections;

using Valve.VR.InteractionSystem;
using Valve.VR;

#if SIGVERSE_PUN
using Photon.Pun;
#endif

namespace SIGVerse.Competition.HumanNavigation
{
#if SIGVERSE_PUN
	public class HandDataUpdater : MonoBehaviourPun
#else
	public class HandDataUpdater : MonoBehaviour
#endif
	{
		private const float ValueUpdateInterval = 0.2f; //[s]

		private Hand     hand;
		private HandData handData;

		private bool   handTriggerState;
		private float  handTriggerValue;
		private bool   isInteracting;
		private string currentlyInteractingName; 
		private string currentlyInteractingTag; 

		private bool   preHandTriggerState;
		private float  preHandTriggerValue;
		private bool   preIsInteracting;
		private string preCurrentlyInteractingName; 
		private string preCurrentlyInteractingTag; 

		private PunMode punMode;

		private float preTime = 0f;

		public void Awake()
		{
			this.hand = this.GetComponent<Hand>();
			this.handData = this.GetComponent<HandData>();

			this.punMode = (PunMode)HumanNaviConfig.Instance.configInfo.PunMode();
		}

		public void Update()
		{
			if (punMode == PunMode.NotUsed || punMode == PunMode.PunClient)
			{
				this.handTriggerState = SteamVR_Actions.sigverse.PressMiddle  .GetState(this.hand.handType);
				this.handTriggerValue = SteamVR_Actions.sigverse.SqueezeMiddle.GetAxis(this.hand.handType);

				this.isInteracting            = hand.currentAttachedObject!=null;
				this.currentlyInteractingName = hand.currentAttachedObject==null? string.Empty : hand.currentAttachedObject.name;
				this.currentlyInteractingTag  = hand.currentAttachedObject==null? string.Empty : hand.currentAttachedObject.tag;

				this.SetHandData
				(
					SteamVR_Actions.sigverse.PressMiddle  .GetState(this.hand.handType), 
					SteamVR_Actions.sigverse.SqueezeMiddle.GetAxis(this.hand.handType), 
					hand.currentAttachedObject!=null, 
					hand.currentAttachedObject==null? string.Empty : hand.currentAttachedObject.name, 
					hand.currentAttachedObject==null? string.Empty : hand.currentAttachedObject.tag,
					SteamVR_Actions.sigverse.PressMiddle    .GetStateDown(this.hand.handType),
					SteamVR_Actions.sigverse.PressMiddle    .GetStateUp  (this.hand.handType),
					SteamVR_Actions.sigverse.PressNearButton.GetStateDown(this.hand.handType),
					SteamVR_Actions.sigverse.PressFarButton .GetStateDown(this.hand.handType)
				);
			}
		}


		private void UpdatePreNVRHandData()
		{
			this.preHandTriggerState = this.handTriggerState;
			this.preHandTriggerValue = this.handTriggerValue;

			this.preIsInteracting            = this.isInteracting;
			this.preCurrentlyInteractingName = this.currentlyInteractingName;
			this.preCurrentlyInteractingTag  = this.currentlyInteractingTag;
		}


		private void SetHandData
		(
			bool   handTriggerState, 
			float  handTriggerValue, 
			bool   isInteracting, 
			string currentlyInteractingName, 
			string currentlyInteractingTag,
			bool   handTriggerDown,
			bool   handTriggerUp,
			bool   nearButtonDown,
			bool   farButtonDown
		){
			if (!this.IsChanged()) { return; }

#if SIGVERSE_PUN
			if(PhotonNetwork.IsConnectedAndReady)
			{
				this.photonView.RPC(nameof(ForwardHandDataRPC), RpcTarget.All,
					handTriggerState,
					handTriggerValue,
					isInteracting,
					currentlyInteractingName,
					currentlyInteractingTag,
					handTriggerDown,
					handTriggerUp,
					nearButtonDown,
					farButtonDown
				);
			}
#endif

			this.UpdatePreNVRHandData();

			this.preTime = Time.time;
		}

		private bool IsChanged()
		{
			if (SteamVR_Actions.sigverse.PressMiddle    .GetStateDown(this.hand.handType)){ return true; }
			if (SteamVR_Actions.sigverse.PressMiddle    .GetStateUp  (this.hand.handType)){ return true; }
			if (SteamVR_Actions.sigverse.PressNearButton.GetStateDown(this.hand.handType)){ return true; }
			if (SteamVR_Actions.sigverse.PressNearButton.GetStateUp  (this.hand.handType)){ return true; }
			if (SteamVR_Actions.sigverse.PressFarButton .GetStateDown(this.hand.handType)){ return true; }
			if (SteamVR_Actions.sigverse.PressFarButton .GetStateUp  (this.hand.handType)){ return true; }

			if (this.preHandTriggerState != this.handTriggerState) { return true; }

			if (this.preIsInteracting            != this.isInteracting)            { return true; }
			if (this.preCurrentlyInteractingName != this.currentlyInteractingName) { return true; }
			if (this.preCurrentlyInteractingTag  != this.currentlyInteractingTag)  { return true; }

			if(Time.time > this.preTime + ValueUpdateInterval)
			{
				if (this.preHandTriggerValue != this.handTriggerValue) { return true; }
			}

			return false;
		}


		// There must be a PhotonView in the same object.
#if SIGVERSE_PUN
		[PunRPC]
#endif
		public void ForwardHandDataRPC 
		(
			bool handTriggerState,
			float handTriggerValue,
			bool isInteracting,
			string currentlyInteractingName,
			string currentlyInteractingTag,
			bool handTriggerPressed,
			bool handTriggerUp,
			bool nearButtonPressed,
			bool farButtonPressed,
			PhotonMessageInfo info
		)
		{
//			Debug.LogError("ForwardNVRHandDataRPC");

			this.handData.SetHandData
			(
				handTriggerState,
				handTriggerValue,
				isInteracting,
				currentlyInteractingName,
				currentlyInteractingTag,
				handTriggerPressed,
				handTriggerUp,
				nearButtonPressed,
				farButtonPressed
			);
		}
	}
}

