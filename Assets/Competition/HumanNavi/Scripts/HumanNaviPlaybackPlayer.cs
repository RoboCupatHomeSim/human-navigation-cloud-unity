using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Valve.VR;
using Valve.VR.InteractionSystem;
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;

namespace SIGVerse.Competition.HumanNavigation
{
	[RequireComponent(typeof (HumanNaviPlaybackCommon))]
	public class HumanNaviPlaybackPlayer : TrialPlaybackPlayer
	{
		[HeaderAttribute("Handyman Objects")]
		public HumanNaviScoreManager scoreManager;

		[HeaderAttribute("Session Manager")]
		public HumanNaviSessionManager sessionManager;

		public SAPIVoiceSynthesisExternal sapiVoiceSynthesisExternal;

		private PlaybackGuidanceMessageEventController guidanceMessageController;

		protected override void Awake()
		{
			this.isPlay = HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay;

			base.Awake();

			HumanNaviPlaybackCommon common = this.GetComponent<HumanNaviPlaybackCommon>();

			if (this.isPlay)
			{
				Transform robot = common.robot.transform;

				//robot.Find("CompetitionScripts").gameObject.SetActive(false);
				robot.Find("RosBridgeScripts")  .gameObject.SetActive(false);

				Transform moderator = GameObject.FindGameObjectWithTag("Moderator").transform;

				moderator.GetComponent<HumanNaviModerator>().enabled = false;
				moderator.GetComponent<HumanNaviPubMessage>().enabled = false;
				moderator.GetComponent<HumanNaviSubMessage>().enabled = false;
				moderator.GetComponent<HumanNaviPubTaskInfo>().enabled = false;
				moderator.GetComponent<HumanNaviPubAvatarStatus>().enabled = false;
				moderator.GetComponent<HumanNaviPubObjectStatus>().enabled = false;

				robot.GetComponentInChildren<HumanNaviSubGuidanceMessage>().enabled = false;

				this.scoreManager.enabled = false;

				// Avatar
				Transform avatar = GameObject.FindGameObjectWithTag("Avatar").transform;
				avatar.GetComponentInChildren<Valve.VR.InteractionSystem.Player>().enabled = false;

				foreach (SteamVR_Behaviour_Pose pose in avatar.GetComponentsInChildren<SteamVR_Behaviour_Pose>()) { pose.enabled = false; }
				foreach (Hand hand in avatar.GetComponentsInChildren<Hand>()) { hand.enabled = false; }
				avatar.GetComponentInChildren<SteamVR_CameraHelper>().enabled = false;
				avatar.GetComponentInChildren<TrackedPoseDriver>().enabled = false;

				XRLoader activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
				if (activeLoader != null)
				{
					activeLoader.Stop();
					XRGeneralSettings.Instance.Manager.DeinitializeLoader();
				}

				avatar.GetComponentInChildren<Animator>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.VR.SimpleHumanVRController>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.IK.SimpleIK>().enabled = false;

				avatar.GetComponentInChildren<Animator>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.VR.SimpleHumanVRController>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.IK.SimpleIK>().enabled = false;

				//UnityEngine.XR.XRSettings.enabled = false;

				this.timeLimit = HumanNaviConfig.Instance.configInfo.sessionTimeLimit;
			}
		}

		protected override void Start()
		{
			HumanNaviPlaybackCommon common = this.GetComponent<HumanNaviPlaybackCommon>();

			common.robot.gameObject.SetActive(true);

			this.filePath = common.GetFilePath();

			//this.transformController   = new PlaybackTransformEventController  (common);  // Transform
			this.videoPlayerController = new PlaybackVideoPlayerEventController(common);  // Video Player

			this.taskInfoController = new PlaybackTaskInfoEventController(this.teamNameText, this.trialNumberText, this.timeLeftValText, this.taskMessageText);
			this.scoreController = new PlaybackScoreEventController(this.scoreText, this.totalText); // Score
			this.panelNoticeController = new PlaybackPanelNoticeEventController(this, this.mainMenu);      // Notice of a Panel
			this.collisionController = new PlaybackCollisionEventController(this.collisionEffect);       // Collision
			this.hsrCollisionController = new PlaybackHsrCollisionEventController(this.collisionEffect);    // HSR Collision
		}

		protected override void ReadData(string[] headerArray, string dataStr)
		{
			base.ReadData(headerArray, dataStr);

			this.guidanceMessageController.ReadEvents(headerArray, dataStr);
		}

		protected override void StartInitializing()
		{
			base.StartInitializing();

			this.guidanceMessageController.StartInitializingEvents();
		}

		protected override void UpdateIndexAndElapsedTime(float elapsedTime)
		{
			base.UpdateIndexAndElapsedTime(elapsedTime);

			this.guidanceMessageController.UpdateIndex(elapsedTime);
		}

		protected override void UpdateData()
		{
			base.UpdateData();

			this.guidanceMessageController.ExecutePassedAllEvents(this.elapsedTime, this.deltaTime);
		}

		protected override float GetTotalTime()
		{
			return Mathf.Max(
				base.GetTotalTime(),
				this.taskInfoController.GetTotalTime(),
				this.scoreController.GetTotalTime(),
				this.panelNoticeController.GetTotalTime(),
				this.collisionController.GetTotalTime(),
				this.hsrCollisionController.GetTotalTime(),
				this.guidanceMessageController.GetTotalTime()
			);
		}

		public override void OnReadFileButtonClick()
		{
			this.trialNo = int.Parse(this.trialNoInputField.text);

			string filePath = string.Format(Application.dataPath + HumanNaviPlaybackCommon.FilePathFormat, this.trialNo);

			this.sessionManager.ChangeEnvironment(this.trialNo);

			HumanNaviPlaybackCommon common = this.GetComponent<HumanNaviPlaybackCommon>();

			common.SetPlaybackTargets();

			this.guidanceMessageController = new PlaybackGuidanceMessageEventController(this.sapiVoiceSynthesisExternal.gameObject); // Guidance message
			this.transformController = new PlaybackTransformEventController(common);  // Transform

			this.Initialize(filePath);
		}
	}
}

