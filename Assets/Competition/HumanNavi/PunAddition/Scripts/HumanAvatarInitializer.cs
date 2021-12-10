using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGVerse.Common;
using SIGVerse.Human.IK;
using SIGVerse.Competition;
using System.Linq;
using Valve.VR;
using Valve.VR.InteractionSystem;
using SIGVerse.Human.VR;
using UnityEngine.XR.Management;
using Photon.Pun;
using UnityEngine.SpatialTracking;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanAvatarInitializer : MonoBehaviourPun
	{
		public GameObject cameraRig;

		public GameObject ethan;

		public GameObject eyeAnchor;

		private XRLoader activeLoader;

		private bool updatedFieldOfView = false;

		private void Awake()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode()!=PunMode.PunServer)
			{
				StartCoroutine(this.InitializeXR());
//				StartCoroutine(this.StartXRCoroutine());

				SteamVR_Actions.sigverse.Activate(SteamVR_Input_Sources.Any);
			}
		}

		public IEnumerator InitializeXR()
		{
			// Initialize XR System
			XRManagerSettings xrManagerSettings = XRGeneralSettings.Instance.Manager;

			if (xrManagerSettings == null) { SIGVerseLogger.Error("xrManagerSettings == null"); yield break; }

			yield return xrManagerSettings.InitializeLoader();

			if (xrManagerSettings.activeLoader == null)
			{
				SIGVerseLogger.Error("xrManagerSettings.activeLoader == null"); yield break;
			}

			//this.activeLoader = xrManagerSettings.activeLoader;

			//if (this.activeLoader == null)
			//{
			//	Debug.LogError("Initializing XR Failed.");
			//	yield break;
			//}

			xrManagerSettings.StartSubsystems();

			//yield return null;
		}

		//public IEnumerator StartXRCoroutine()
		//{
		//	Debug.Log("Initializing XR...");
		//	yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

		//	if (XRGeneralSettings.Instance.Manager.activeLoader == null)
		//	{
		//		Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
		//	}
		//	else
		//	{
		//		Debug.Log("Starting XR...");
		//		XRGeneralSettings.Instance.Manager.StartSubsystems();
		//	}
		//}

		void Start()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				this.ethan.GetComponent<Rigidbody>().useGravity = false;
				return;
			}

			if (HumanNaviConfig.Instance.configInfo.PunMode() != PunMode.PunServer)
			{
				this.GetComponent<Player>().enabled = true;

				this.cameraRig.GetComponent<SIGVerse.Human.IK.AnchorPostureCalculator>().enabled = true;

				SteamVR_Behaviour_Pose[] steamVrBehaviourPoses = this.cameraRig.GetComponentsInChildren<SteamVR_Behaviour_Pose>();

				foreach(SteamVR_Behaviour_Pose steamVrBehaviourPose in steamVrBehaviourPoses)
				{
					steamVrBehaviourPose.enabled = true;
				}

				Hand[] hands = this.cameraRig.GetComponentsInChildren<Hand>(true);

				foreach(Hand hand in hands)
				{
					hand.enabled = true;
				}

				foreach (HandDataUpdater handDataUpdater in this.GetComponentsInChildren<HandDataUpdater>())
				{
					handDataUpdater.enabled = true;
				}

//				this.eyeAnchor.GetComponent<Camera>()              .enabled = true;
				this.eyeAnchor.GetComponent<AudioListener>()       .enabled = true;
				this.eyeAnchor.GetComponent<SteamVR_CameraHelper>().enabled = true;

				this.ethan.GetComponent<Animator>()                     .enabled = true;
				this.ethan.GetComponent<SimpleHumanVRControllerForPun>().enabled = true;
				this.ethan.GetComponent<SimpleIK>()                     .enabled = true;
				this.ethan.GetComponent<CapsuleCollider>()              .enabled = true;
				this.ethan.GetComponent<BoxCollider>()                  .enabled = true;

				Rigidbody[] rigidbodies = this.GetComponentsInChildren<Rigidbody>(true);

				Debug.Log("rigidbodies.Length="+rigidbodies.Length);
				foreach(Rigidbody rigidbody in rigidbodies)
				{
					rigidbody.useGravity = true;
//					rigidbody.isKinematic = true;
				}
			}
			else
			{
				// Disable the automatically added TrackedPoseDriver.
				this.GetComponentInChildren<TrackedPoseDriver>().enabled = false;
			}
		}

		void Update()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient)
			{
				if (this.updatedFieldOfView) { return; }

				StartCoroutine(this.UpdateFieldOfViewCoroutine());

				this.updatedFieldOfView = true;
			}
		}

		public IEnumerator UpdateFieldOfViewCoroutine(float waitTime = 5.0f)
		{
			yield return new WaitForSeconds(waitTime);

			float fov = this.eyeAnchor.GetComponent<Camera>().fieldOfView;

			this.photonView.RPC(nameof(UpdateFieldOfView), RpcTarget.OthersBuffered, fov);

			SIGVerseLogger.Info("Sent FieldOfView fov="+fov);
		}


		[PunRPC]
		public void UpdateFieldOfView(float fieldOfView, PhotonMessageInfo info)
		{
			this.eyeAnchor.GetComponent<Camera>().fieldOfView = fieldOfView;

			SIGVerseLogger.Info("Updated FieldOfView fov=" + fieldOfView);
		}

		void OnDestroy()
		{
			// It is mandatory to perform this termination process.
			if(this.activeLoader != null)
			{
				this.activeLoader.Stop();
				XRGeneralSettings.Instance.Manager.DeinitializeLoader();
			}
		}
		//private void EnableCleanupAvatarVRHandControllerForSteamVR()
		//{
		//	CleanupAvatarVRHandControllerForSteamVR[] cleanupAvatarVRHandControllerForSteamVRs = this.ethan.GetComponents<CleanupAvatarVRHandControllerForSteamVR>();

		//	foreach (CleanupAvatarVRHandControllerForSteamVR cleanupAvatarVRHandControllerForSteamVR in cleanupAvatarVRHandControllerForSteamVRs)
		//	{
		//		cleanupAvatarVRHandControllerForSteamVR.enabled = true;
		//	}
		//}
	}
}
