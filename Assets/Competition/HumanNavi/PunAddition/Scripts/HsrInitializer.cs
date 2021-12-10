using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGVerse.Common;
using SIGVerse.Competition.HumanNavigation;

#if SIGVERSE_PUN
using Photon.Pun;
#endif

namespace SIGVerse.Competition.HumanNavigation
{
	public class HsrInitializer : MonoBehaviour
	{
#if SIGVERSE_PUN

		public GameObject rosBridgeScripts;
		public GameObject competitionScripts;
		public GameObject moderator;

		void Awake()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient) { return; }

			if (HumanNaviConfig.Instance.configInfo.IsPractice()) { return; }

			if (HumanNaviConfig.Instance.configInfo.playbackType != SIGVerse.Competition.WorldPlaybackCommon.PlaybackTypePlay)
			{
				this.GetComponent<AudioListener>().enabled = true;

				// ROS Connection Start
				this.rosBridgeScripts.SetActive(true);

				this.competitionScripts.GetComponent<HumanNaviSubGuidanceMessage>().enabled = true;

				this.moderator.GetComponent<HumanNaviPubTaskInfo>().enabled = true;
				this.moderator.GetComponent<HumanNaviPubMessage>().enabled = true;
				this.moderator.GetComponent<HumanNaviSubMessage>().enabled = true;
				this.moderator.GetComponent<HumanNaviPubAvatarStatus>().enabled = true;
				this.moderator.GetComponent<HumanNaviPubObjectStatus>().enabled = true;
				// ROS Connection End
			}
		}
#endif
	}
}
