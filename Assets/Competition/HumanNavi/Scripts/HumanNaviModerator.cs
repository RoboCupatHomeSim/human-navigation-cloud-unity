using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.Common;
using SIGVerse.RosBridge;
using System.Threading;
using UnityEngine.SceneManagement;

using Valve.VR;
using Valve.VR.InteractionSystem;
using static SIGVerse.Competition.HumanNavigation.HandData;

#if SIGVERSE_PUN
using Photon.Pun;
#endif

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IReachMaxWrongObjectGraspCountHandler : IEventSystemHandler
	{
		void OnReachMaxWrongObjectGraspCount();
	}


	public class HumanNaviModerator : MonoBehaviourPun, ITimeIsUpHandler, IStartTrialHandler, IGoToNextTrialHandler, IReceiveHumanNaviMsgHandler, ISendSpeechResultHandler, IReachMaxWrongObjectGraspCountHandler
	{
		private const int SendingAreYouReadyInterval = 1000;

		private const string MsgAreYouReady     = "Are_you_ready?";
		private const string MsgTaskSucceeded   = "Task_succeeded";
		private const string MsgTaskFailed      = "Task_failed";
		private const string MsgTaskFinished    = "Task_finished";
		private const string MsgGoToNextSession = "Go_to_next_session";
		private const string MsgMissionComplete = "Mission_complete";

		private const string ReasonTimeIsUp = "Time_is_up";
		private const string ReasonGiveUp   = "Give_up";
		private const string ReasonReachMaxWrongObjectGraspCount = "Reach_max_wrong_object_grasp_count";

		private const string MsgIamReady        = "I_am_ready";
		private const string MsgGetAvatarStatus = "Get_avatar_status";
		private const string MsgGetObjectStatus = "Get_object_status";
		private const string MsgGetSpeechState  = "Get_speech_state";
		private const string MsgGiveUp          = "Give_up";

		private const string MsgRequest      = "Guidance_request";
		private const string MsgSpeechState  = "Speech_state";
		private const string MsgSpeechResult = "Speech_result";

		private const string TagNameOfGraspables  = "Graspables";
		private const string TagNameOfFurniture   = "Furniture";
		private const string TagNameOfDestination = "Destination";

		private enum Step
		{
			Initialize,
			WaitForPreProcess,
			WaitForStart,
			SessionStart,
			WaitForIamReady,
			SendTaskInfo,
			WaitForEndOfSession,
			WaitForPlaybackFinished,
			WaitForNextSession,
		};

		//-----------------------------

		[HeaderAttribute("Score Manager")]
		public HumanNaviScoreManager scoreManager;

		[HeaderAttribute("Session Manager")]
		public HumanNaviSessionManager sessionManager;

//		[HeaderAttribute("Avatar")]
//		public float heightThresholdForPoseReset = -0.5f;

		[HeaderAttribute("Avatar")]
		public GameObject avatarForSimpleIK;
		public GameObject headForSimpleIK;
		public GameObject bodyForSimpleIK;
//		public NewtonVR.NVRHand LeftHandForSimpleIK;
//		public NewtonVR.NVRHand rightHandForSimpleIK;

		public HandData leftHandData;
		public HandData rightHandData;

//		public Hand LeftHandForSimpleIK;
//		public Hand rightHandForSimpleIK;
		public GameObject noticePanelForSimpleIKAvatar;
		public UnityEngine.UI.Text noticeTextForSimpleIKAvatar;

		[HeaderAttribute("Menu")]
		public Camera birdviewCamera;
		public GameObject startTrialPanel;
		public GameObject goToNextTrialPanel;

		[HeaderAttribute("Scenario Logger")]
		public GameObject playbackManager;

		[HeaderAttribute("Photon")]
		public PunLauncher   punLauncher;
//		public PunRpcManager punRpcManager;
		//-----------------------------

		private GameObject avatar;
		private GameObject head;
		private GameObject body;
//		private NewtonVR.NVRHand LeftHand;
//		private NewtonVR.NVRHand rightHand;
		
//		private Hand LeftHand;
//		private Hand rightHand;

		private GameObject noticePanelForAvatar;
		private UnityEngine.UI.Text noticeTextForAvatar;

		private Vector3 initialAvatarPosition;
		private Vector3 initialAvatarRotation;

		private GameObject mainMenu;
		private PanelMainController panelMainController;

		private SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo taskInfoForRobot;
		private HumanNavigation.TaskInfo currentTaskInfo;

		private Step step;

		private float waitingTime;

		private bool isCompetitionStarted = false;
		private bool isDuringSession = false;

		private Dictionary<string, bool> receivedMessageMap;
		private bool isTargetAlreadyGrasped = false;
		private bool isAllTaskFinished = false;
		private string interruptedReason;

		private bool isStoppedForPunClient = false;

		private StepTimer stepTimer;

		private HumanNaviPlaybackRecorder playbackRecorder;

		private Vector3 initialTargetObjectPosition;
		private Vector3 initialDestinationPosition;

		private string objectIdInLeftHand;
		private string objectIdInRightHand;

		private string objectIdInLeftHandPreviousFrame;
		private string objectIdInRightHandPreviousFrame;

		private List<string> alreadyGraspedObjects;

		private int numberOfSession;

		private List<GameObject> graspableObjects;

		private Dictionary<HandType, string> preInteractingNameMap;

		//private int countWrongObjectsGrasp;

		//private bool preLeftHandHoldButtonDown = false;
		//private bool preLeftHandHoldButtonUp   = false;
		//private bool preRightHandHoldButtonDown = false;
		//private bool preRightHandHoldButtonUp   = false;

		//-----------------------------

		private IRosConnection[] rosConnections = new IRosConnection[] { };

		//-----------------------------

		private bool isPracticeMode = false;

		private bool isStartingDisconnectFromPUN = false;

		private bool isPreprocessFinished = false;


		void Awake()
		{
			try
			{
				this.avatar    = this.avatarForSimpleIK;
				this.head      = this.headForSimpleIK;
				this.body      = this.bodyForSimpleIK;
//				this.LeftHand  = this.LeftHandForSimpleIK;
//				this.rightHand = this.rightHandForSimpleIK;
				this.noticePanelForAvatar = this.noticePanelForSimpleIKAvatar;
				this.noticeTextForAvatar  = this.noticeTextForSimpleIKAvatar;

				// Practice mode
				if (HumanNaviConfig.Instance.configInfo.IsPractice())
				{
					this.isPracticeMode = true;
				}

				// Playback system
				this.playbackRecorder = this.playbackManager.GetComponent<HumanNaviPlaybackRecorder>();

				// Avatar 
				this.initialAvatarPosition = this.avatar.transform.position;
				this.initialAvatarRotation = this.avatar.transform.eulerAngles;

				// GUI
				this.mainMenu = GameObject.FindGameObjectWithTag("MainMenu");
				this.panelMainController = mainMenu.GetComponent<PanelMainController>();

				this.noticePanelForAvatar.SetActive(false);
				this.noticeTextForAvatar.text = "";

				// MessageMap
				this.receivedMessageMap = new Dictionary<string, bool>();
				this.receivedMessageMap.Add(MsgIamReady, false);
				this.receivedMessageMap.Add(MsgGetAvatarStatus, false);
				this.receivedMessageMap.Add(MsgGetObjectStatus, false);
				this.receivedMessageMap.Add(MsgGetSpeechState, false);
				this.receivedMessageMap.Add(MsgGiveUp, false);

				this.preInteractingNameMap = new Dictionary<HandType, string>();
				this.preInteractingNameMap.Add(HandType.LeftHand,  null);
				this.preInteractingNameMap.Add(HandType.RightHand, null);

				// Timer
				this.stepTimer = new StepTimer();
			}
			catch (Exception exception)
			{
				Debug.LogError(exception);
				SIGVerseLogger.Error(exception.Message);
				SIGVerseLogger.Error(exception.StackTrace);
				this.ApplicationQuitAfter1sec();
			}
		}

		void Start()
		{
			this.step = Step.Initialize;

			this.panelMainController.SetTeamNameText("Team: " + HumanNaviConfig.Instance.configInfo.teamName);

			this.ShowStartTaskPanel();

			this.InitializeMainPanel();

			this.interruptedReason = string.Empty;
		}

		//void OnDestroy()
		//{
		//	this.CloseRosConnections();
		//}

		// Update is called once per frame
		void Update()
		{
			try
			{
				if (this.isAllTaskFinished)
				{ 
					return;
				}

				if (this.isStoppedForPunClient)
				{
					return;
				}

				if (this.interruptedReason != string.Empty && this.step != Step.WaitForNextSession)
				{
					SIGVerseLogger.Info("Failed '" + this.interruptedReason + "'");
					this.SendPanelNotice("Failed\n" + interruptedReason.Replace('_', ' '), 100, PanelNoticeStatus.Red);
					this.TimeIsUp();
				}

				this.leftHandData .ClearDataForModerator();
				this.rightHandData.ClearDataForModerator();

				//if (this.preLeftHandHoldButtonDown  && this.leftHandData.HoldButtonDownForModerator) { this.leftHandData.HoldButtonDownForModerator  = false; }
				//if (this.preLeftHandHoldButtonUp    && this.leftHandData.HoldButtonUpForModerator)   { this.leftHandData.HoldButtonUpForModerator    = false; }
				//if (this.preRightHandHoldButtonDown && this.rightHandData.HoldButtonDownForModerator){ this.rightHandData.HoldButtonDownForModerator = false; }
				//if (this.preRightHandHoldButtonUp   && this.rightHandData.HoldButtonUpForModerator)  { this.rightHandData.HoldButtonUpForModerator   = false; }

				// Giveup for practice mode
				if ( this.isPracticeMode &&(
					(SteamVR_Actions.sigverse_PressThumbstick.GetStateDown(SteamVR_Input_Sources.LeftHand)  && SteamVR_Actions.sigverse_PressNearButton.GetStateDown(SteamVR_Input_Sources.LeftHand)  && SteamVR_Actions.sigverse_PressFarButton.GetStateDown(SteamVR_Input_Sources.LeftHand)) ||
					(SteamVR_Actions.sigverse_PressThumbstick.GetStateDown(SteamVR_Input_Sources.RightHand) && SteamVR_Actions.sigverse_PressNearButton.GetStateDown(SteamVR_Input_Sources.RightHand) && SteamVR_Actions.sigverse_PressFarButton.GetStateDown(SteamVR_Input_Sources.RightHand)) ||
					(Input.GetKeyDown(KeyCode.Escape))
				)){
					this.OnGiveUp();
				}

//				if (SteamVR_Actions.sigverse_PressNearButton.GetStateDown(SteamVR_Input_Sources.LeftHand) && this.isDuringSession)
				if (this.leftHandData.nearButtonDownForModerator && this.isDuringSession)
				{
					if (this.isPracticeMode)
					{
						this.sessionManager.SpeakGuidanceMessageForPractice(HumanNaviConfig.Instance.configInfo.guidanceMessageForPractice);
					}
					else
					{
						//if (!this.sessionManager.GetTTSRuningState())
						{
							this.SendRosHumanNaviMessage(MsgRequest, "");
						}
					}
				}

				//if (this.avatar.transform.position.y < heightThresholdForPoseReset)
				//{
				//	this.ResetAvatarTransform();
				//}

				//this.punLauncher.TransferOwnershipOfGraspables(this.leftHandData);
				//this.punLauncher.TransferOwnershipOfGraspables(this.rightHandData);

				switch (this.step)
				{
					case Step.Initialize:
					{
						if (this.isCompetitionStarted)
						{
							StartCoroutine(this.PreProcess());
							this.step++;
						}

						break;
					}
					case Step.WaitForPreProcess:
					{
						if(this.isPreprocessFinished) // True only on the server side.
						{
							if (PhotonNetwork.IsMasterClient || this.stepTimer.IsTimePassed((int)this.step, 2000)) // Wait for client initialization
							{
								this.ShowNoticeMessagePanelForAvatar("Please wait", 2.0f);
								this.step++;
							}
						}

						break;
					}
					case Step.WaitForStart:
					{
						if (this.stepTimer.IsTimePassed((int)this.step, 3000))
						{
//							if (!HumanNaviConfig.Instance.configInfo.photonServerMachine || this.IsConnectedToRos())
							if (this.IsPlaybackInitialized() && this.IsConnectedToRos())
							{
								this.step++;
							}
						}

						break;
					}
					case Step.SessionStart:
					{
//						this.punLauncher.Connect();

						this.goToNextTrialPanel.SetActive(false);

						this.scoreManager.TaskStart();
						this.StartPlaybackRecord();

						SIGVerseLogger.Info("Session start!");
						this.RecordEventLog("Session_start");

						this.SendPanelNotice("Session start!", 100, PanelNoticeStatus.Green);
						this.ShowNoticeMessagePanelForAvatar("Session start!", 3.0f);

						this.isDuringSession = true;
						this.step++;

						break;
					}
					case Step.WaitForIamReady:
					{
						// For Practice
						if (this.isPracticeMode)
						{
							this.sessionManager.ResetNotificationDestinationsOfTTS();

							this.step++;
							break;
						}

						if (this.receivedMessageMap[MsgIamReady])
						{
							//this.StartPlaybackRecord();
							this.step++;
							break;
						}

						this.SendMessageAtIntervals(MsgAreYouReady, "", SendingAreYouReadyInterval);

						break;
					}
					case Step.SendTaskInfo:
					{
						if (!this.isPracticeMode)
						{
							this.SendRosTaskInfoMessage(this.taskInfoForRobot);
						}

						if (this.isPracticeMode) // first instruction for practice mode (TODO: this code should be in more appropriate position)
						{
							this.sessionManager.SpeakGuidanceMessageForPractice(HumanNaviConfig.Instance.configInfo.guidanceMessageForPractice);
						}

						SIGVerseLogger.Info("Waiting for end of session");

						this.step++;

						break;
					}
					case Step.WaitForEndOfSession:
					{
						// for score (grasp)
						this.JudgeGraspingObject();

						// for log (grasp)
						this.CheckGraspingStatus(this.leftHandData);
						this.CheckGraspingStatus(this.rightHandData);

						// for avatar status
						this.objectIdInLeftHandPreviousFrame  = this.objectIdInLeftHand;
						this.objectIdInRightHandPreviousFrame = this.objectIdInRightHand;
						this.objectIdInLeftHand  = this.GetGraspingObjectId(this.leftHandData);
						this.objectIdInRightHand = this.GetGraspingObjectId(this.rightHandData);

						// for penalty of distance between the robot and the target/destination
						this.JudgeDistanceFromTargetObject();
						this.JudgeDistanceFromDestination();

						break;
					}
					case Step.WaitForPlaybackFinished:
					{
						if (this.stepTimer.IsTimePassed((int)this.step, 7000))
						{
							if (!this.IsPlaybackFinished()) { break; }
//#if SIGVERSE_PUN
//							PhotonNetwork.LeaveRoom();
////							StartCoroutine(this.punLauncher.Disconnect());
//#endif
							step++;
						}
						break;
					}
					case Step.WaitForNextSession:
					{
//#if SIGVERSE_PUN
//						if (PhotonNetwork.InRoom) { break; }
//#endif
						if(!this.isStartingDisconnectFromPUN)
						{
							StartCoroutine(this.punLauncher.DisconnectAndLoadScene());

							this.isStartingDisconnectFromPUN = true;
						}

//						SceneManager.LoadScene(SceneManager.GetActiveScene().name);
//						PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);

						break;
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogError(exception);
				SIGVerseLogger.Error(exception.Message);
				SIGVerseLogger.Error(exception.StackTrace);
				this.ApplicationQuitAfter1sec();
			}
		}

		//-----------------------------

		private void ApplicationQuitAfter1sec()
		{
			StartCoroutine(this.CloseRosConnections());

			Thread.Sleep(1000);
			Application.Quit();
		}

		//-----------------------------

		private void InitializeMainPanel()
		{
			this.panelMainController.SetTeamNameText("Team: " + HumanNaviConfig.Instance.configInfo.teamName);

			if (this.isPracticeMode)
			{
				HumanNaviConfig.Instance.numberOfTrials = 1;
			}
			else
			{
				HumanNaviConfig.Instance.InclementNumberOfTrials(HumanNaviConfig.Instance.configInfo.playbackType);
			}

			this.numberOfSession = HumanNaviConfig.Instance.numberOfTrials;

			//this.panelMainController.SetTrialNumberText(HumanNaviConfig.Instance.numberOfTrials);
			this.panelMainController.SetTrialNumberText(this.numberOfSession);
			SIGVerseLogger.Info("##### " + this.panelMainController.GetTrialNumberText() + " #####");

			this.panelMainController.SetTaskMessageText("");

			this.scoreManager.ResetTimeLeftText();
		}

		private IEnumerator PreProcess()
		{
			float delayTime = 2.0f;

			if(HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient)
			{
				if(PhotonNetwork.IsMasterClient)
				{
					this.photonView.RPC(nameof(StartTrial), RpcTarget.Others);

					yield return new WaitForSeconds(delayTime);

					this.ExecPreProcess();
				}
				else
				{
					this.ExecPreProcess();

					yield return new WaitForSeconds(delayTime);

					this.photonView.RPC(nameof(StartTrial), RpcTarget.Others);
				}
			}
			else
			{
				this.ExecPreProcess();
			}
		}

		private void ExecPreProcess()
		{
			this.sessionManager.ChangeEnvironment(this.numberOfSession);

			this.ResetAvatarTransform();

			this.sessionManager.ActivateRobot();

			if (!this.isPracticeMode)
			{
				this.rosConnections = SIGVerseUtils.FindObjectsOfInterface<IRosConnection>();
				SIGVerseLogger.Info("ROS connection : count=" + this.rosConnections.Length);
			}
			else
			{
				SIGVerseLogger.Info("No ROS connection (Practice mode)");
			}

			//this.currentTaskInfo = this.sessionManager.GetCurrentTaskInfo();
			this.currentTaskInfo = this.sessionManager.GetTaskInfo(this.numberOfSession);

			this.taskInfoForRobot = new SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo();
			string environmentName = this.sessionManager.GetEnvironment().name;
			this.taskInfoForRobot.environment_id = environmentName.Substring(0, environmentName.Length - 3);
			this.SetObjectListToHumanNaviTaskInfo();
			this.SetFurnitureToHumanNaviTaskInfo();
			this.SetDestinationToHumanNaviTaskInfo();

			this.waitingTime = 0.0f;

			this.interruptedReason = string.Empty;

			this.objectIdInLeftHand  = "";
			this.objectIdInRightHand = "";
			this.objectIdInLeftHandPreviousFrame  = ""; // Tentative Code
			this.objectIdInRightHandPreviousFrame = ""; // Tentative Code

			this.alreadyGraspedObjects = new List<string>();

			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient) 
			{ 
				this.isStoppedForPunClient = true;
				SIGVerseLogger.Info("Stop Moderator Update - PunClient");

				return;
			}

			this.InitializePlayback();

			this.isPreprocessFinished = true;
			//this.countWrongObjectsGrasp = 0;
		}

		private void PostProcess()
		{
			if (HumanNaviConfig.Instance.numberOfTrials == HumanNaviConfig.Instance.configInfo.maxNumberOfTrials)
			{
				this.SendRosHumanNaviMessage(MsgMissionComplete, "");

				StartCoroutine(this.StopTime());

				StartCoroutine(this.CloseRosConnections());

				StartCoroutine(this.DisplayEndMessage());

				this.punLauncher.DisconnectAndLoadScene();

				this.isAllTaskFinished = true;
			}
			else
			{
				this.SendRosHumanNaviMessage(MsgTaskFinished, "");

				SIGVerseLogger.Info("Go to next session");
				//this.RecordEventLog("Go_to_next_session");

				this.SendRosHumanNaviMessage(MsgGoToNextSession, "");

				StartCoroutine(this.ClearRosConnections());
			}

			this.StopPlaybackRecord();

			this.isDuringSession = false;
			this.interruptedReason = string.Empty;

//			this.step = Step.WaitForNextSession;
			this.step = Step.WaitForPlaybackFinished;
		}

		private void ResetAvatarTransform()
		{
			this.avatar.transform.position = this.initialAvatarPosition;
			this.avatar.transform.eulerAngles = this.initialAvatarRotation;
			this.avatar.transform.Find("[CameraRig]").localPosition = Vector3.zero;
			this.avatar.transform.Find("[CameraRig]").localRotation = Quaternion.identity;
			this.avatar.transform.Find("Ethan").localPosition = Vector3.zero;
			this.avatar.transform.Find("Ethan").localRotation = Quaternion.identity;
		}

		private void SetObjectListToHumanNaviTaskInfo()
		{
			// Get graspable objects
			this.graspableObjects = GameObject.FindGameObjectsWithTag(TagNameOfGraspables).ToList<GameObject>();
			if (this.graspableObjects.Count == 0)
			{
				throw new Exception("Graspable object is not found.");
			}

			foreach (GameObject graspableObject in this.graspableObjects)
			{
				// transtrate the coordinate system of GameObject (left-handed, Z-axis:front, Y-axis:up) to ROS coodinate system (right-handed, X-axis:front, Z-axis:up)
				Vector3    positionInROS    = this.ConvertCoordinateSystemUnityToROS_Position(graspableObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoordinateSystemUnityToROS_Rotation(graspableObject.transform.rotation);

				if (graspableObject.name == currentTaskInfo.target)
				{
					taskInfoForRobot.target_object.name = graspableObject.name.Substring(0, graspableObject.name.Length - 3);
					taskInfoForRobot.target_object.position = positionInROS;
					taskInfoForRobot.target_object.orientation = orientationInROS;

					// for penalty
					this.initialTargetObjectPosition = graspableObject.transform.position;
				}
				else
				{
					SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
					{
						name = graspableObject.name.Substring(0, graspableObject.name.Length - 3),
						position = positionInROS,
						orientation = orientationInROS
					};

					taskInfoForRobot.non_target_objects.Add(objInfo);

//					SIGVerseLogger.Info("Non-target object : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
				}
			}
			SIGVerseLogger.Info("Target object : " + taskInfoForRobot.target_object.name + " " + taskInfoForRobot.target_object.position + " " + taskInfoForRobot.target_object.orientation);

			if (taskInfoForRobot.target_object.name == "")
			{
				throw new Exception("Target object is not found.");
			}
		}

		private void SetFurnitureToHumanNaviTaskInfo()
		{
			// Get furniture
			List<GameObject> furnitureObjects = GameObject.FindGameObjectsWithTag(TagNameOfFurniture).ToList<GameObject>();
			if (furnitureObjects.Count == 0)
			{
				throw new Exception("Furniture is not found.");
			}

			foreach (GameObject furnitureObject in furnitureObjects)
			{
				// transtrate the coordinate system of GameObject (left-handed, Z-axis:front, Y-axis:up) to ROS coodinate system (right-handed, X-axis:front, Z-axis:up)
				Vector3    positionInROS    = this.ConvertCoordinateSystemUnityToROS_Position(furnitureObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoordinateSystemUnityToROS_Rotation(furnitureObject.transform.rotation);

				SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
				{
					name = furnitureObject.name.Substring(0, furnitureObject.name.Length - 3),
					position = positionInROS,
					orientation = orientationInROS
				};

				taskInfoForRobot.furniture.Add(objInfo);

//				SIGVerseLogger.Info("Furniture : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
			}
		}

		private void SetDestinationToHumanNaviTaskInfo()
		{
			List<GameObject> destinations = GameObject.FindGameObjectsWithTag(TagNameOfDestination).ToList<GameObject>();
			if (destinations.Count == 0)
			{
				throw new Exception("Destination candidate is not found.");
			}

			if (!destinations.Any(obj => obj.name == this.currentTaskInfo.destination))
			{
				throw new Exception("Destination is not found.");
			}

			GameObject destination = destinations.Where(obj => obj.name == this.currentTaskInfo.destination).SingleOrDefault();

			taskInfoForRobot.destination.position    = this.ConvertCoordinateSystemUnityToROS_Position(destination.transform.position);
			taskInfoForRobot.destination.orientation = this.ConvertCoordinateSystemUnityToROS_Rotation(destination.transform.rotation);
			taskInfoForRobot.destination.size        = this.ConvertCoordinateSystemUnityToROS_Position(destination.GetComponent<BoxCollider>().size);
			// TODO: size parameter depends on the scale of parent object (for now, scale of all parent objects should be scale = (1,1,1))

			// for penalty
			this.initialDestinationPosition = destination.transform.position;

			SIGVerseLogger.Info("Destination : " + taskInfoForRobot.destination);
		}

		private void SendMessageAtIntervals(string message, string detail, int interval_ms = 1000)
		{
			this.waitingTime += UnityEngine.Time.deltaTime;

			if (this.waitingTime > interval_ms * 0.001)
			{
				this.SendRosHumanNaviMessage(message, detail);
				this.waitingTime = 0.0f;
			}
		}

		private void TimeIsUp()
		{
			this.ShowNoticeMessagePanelForAvatar("Time is up", 3.0f);
			this.SendRosHumanNaviMessage(MsgTaskFailed, ReasonTimeIsUp);
			this.SendPanelNotice("Time is up", 100, PanelNoticeStatus.Red);

			this.TaskFinished();
		}

		private void TaskFinished()
		{
			this.scoreManager.TaskEnd();
			this.PostProcess();
		}

		public void OnReceiveRosMessage(RosBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			if (!this.isDuringSession)
			{
				SIGVerseLogger.Warn("Illegal timing [session is not started]");
				return;
			}

			if (this.receivedMessageMap.ContainsKey(humanNaviMsg.message))
			{
				if (humanNaviMsg.message == MsgIamReady)
				{
					if(this.step != Step.WaitForIamReady)
					{
						SIGVerseLogger.Warn("Illegal timing [message : " + humanNaviMsg.message + ", step:" + this.step + "]");
						return;
					}
				}

				if (humanNaviMsg.message == MsgGetAvatarStatus)
				{
					this.SendRosAvatarStatusMessage();
				}

				if (humanNaviMsg.message == MsgGetObjectStatus)
				{
					this.SendRosObjectStatusMessage();
				}

				if (humanNaviMsg.message == MsgGetSpeechState)
				{
					this.SendRosHumanNaviMessage(MsgSpeechState, this.sessionManager.GetSeechRunStateMsgString());
				}

				if (humanNaviMsg.message == MsgGiveUp)
				{
					this.OnGiveUp();
				}

				this.receivedMessageMap[humanNaviMsg.message] = true;
			}
			else
			{
				SIGVerseLogger.Warn("Received Illegal message [message: " + humanNaviMsg.message +"]");
			}
		}

		public void OnSendSpeechResult(string speechResult)
		{
			this.SendRosHumanNaviMessage(MsgSpeechResult, speechResult);
		}

		private Vector3 ConvertCoordinateSystemUnityToROS_Position(Vector3 unityPosition)
		{
			return new Vector3(unityPosition.z, -unityPosition.x, unityPosition.y);
		}
		private Quaternion ConvertCoordinateSystemUnityToROS_Rotation(Quaternion unityQuaternion)
		{
			return new Quaternion(-unityQuaternion.z, unityQuaternion.x, -unityQuaternion.y, unityQuaternion.w);
		}

		private void InitializePlayback()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				this.playbackRecorder.SetPlaybackTargets();
				//this.playbackRecorder.Initialize(HumanNaviConfig.Instance.numberOfTrials);
				this.playbackRecorder.Initialize(this.numberOfSession);
			}
		}

		private bool IsPlaybackInitialized()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				if (!this.playbackRecorder.IsInitialized()) { return false; }
			}

			return true;
		}

		private bool IsPlaybackFinished()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackCommon.PlaybackTypeRecord)
			{
				if (!this.playbackRecorder.IsFinished()) { return false; }
			}

			//if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackCommon.PlaybackTypePlay)
			//{
			//	if (!this.playbackPlayer.IsFinished()) { return false; }
			//}

			return true;
		}

		private void StartPlaybackRecord()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				bool isStarted = this.playbackRecorder.Record();

				if (!isStarted) { SIGVerseLogger.Warn("Cannot start the world playback recording"); }
			}
		}

		private void StopPlaybackRecord()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				bool isStopped = this.playbackRecorder.Stop();

				if (!isStopped) { SIGVerseLogger.Warn("Cannot stop the world playback recording"); }
			}
		}

		private void ShowNoticeMessagePanelForAvatar(string text, float waitTime = 1.0f)
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() != PunMode.PunClient)
			{
				this.photonView.RPC(nameof(ShowNoticeMessagePanelForAvatarRPC), RpcTarget.All, text, waitTime);
			}
		}

		[PunRPC]
		private void ShowNoticeMessagePanelForAvatarRPC(string text, float waitTime)
		{
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatarCoroutine(text, waitTime));
		}

		private IEnumerator ShowNoticeMessagePanelForAvatarCoroutine(string text, float waitTime)
		{
			this.noticeTextForAvatar.text = text;
			this.noticePanelForAvatar.SetActive(true);

			yield return new WaitForSeconds(waitTime);

			this.noticePanelForAvatar.SetActive(false);
		}

		private void SendRosHumanNaviMessage(string message, string detail)
		{
			ExecuteEvents.Execute<IRosHumanNaviMessageSendHandler>
			(
				target: this.gameObject, 
				eventData: null, 
				functor: (reciever, eventData) => reciever.OnSendRosHumanNaviMessage(message, detail)
			);

			ExecuteEvents.Execute<IPlaybackRosMessageHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosMessage(new SIGVerse.RosBridge.human_navigation.HumanNaviMsg(message, detail))
			);
		}

		private void SendRosAvatarStatusMessage()
		{
			RosBridge.human_navigation.HumanNaviAvatarStatus avatarStatus = new RosBridge.human_navigation.HumanNaviAvatarStatus();

			avatarStatus.head.position          = ConvertCoordinateSystemUnityToROS_Position(this.head.transform.position);
			avatarStatus.head.orientation       = ConvertCoordinateSystemUnityToROS_Rotation(this.head.transform.rotation);
			avatarStatus.body.position          = ConvertCoordinateSystemUnityToROS_Position(this.body.transform.position);
			avatarStatus.body.orientation       = ConvertCoordinateSystemUnityToROS_Rotation(this.body.transform.rotation);
			avatarStatus.left_hand.position     = ConvertCoordinateSystemUnityToROS_Position(this.leftHandData.transform.position);
			avatarStatus.left_hand.orientation  = ConvertCoordinateSystemUnityToROS_Rotation(this.leftHandData.transform.rotation);
			avatarStatus.right_hand.position    = ConvertCoordinateSystemUnityToROS_Position(this.rightHandData.transform.position);
			avatarStatus.right_hand.orientation = ConvertCoordinateSystemUnityToROS_Rotation(this.rightHandData.transform.rotation);
			avatarStatus.object_in_left_hand    = this.objectIdInLeftHand  == "" ? "" : this.objectIdInLeftHand .Substring(0, this.objectIdInLeftHand .Length - 3);
			avatarStatus.object_in_right_hand   = this.objectIdInRightHand == "" ? "" : this.objectIdInRightHand.Substring(0, this.objectIdInRightHand.Length - 3);
			avatarStatus.is_target_object_in_left_hand  = this.IsTargetObject(this.objectIdInLeftHand);
			avatarStatus.is_target_object_in_right_hand = this.IsTargetObject(this.objectIdInRightHand);

			ExecuteEvents.Execute<IRosAvatarStatusSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosAvatarStatusMessage(avatarStatus)
			);

			ExecuteEvents.Execute<IRosAvatarStatusSendHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosAvatarStatusMessage(avatarStatus)
			);
		}

		private void SendRosObjectStatusMessage()
		{
			RosBridge.human_navigation.HumanNaviObjectStatus objectStatus = new RosBridge.human_navigation.HumanNaviObjectStatus();

			foreach (GameObject graspableObject in this.graspableObjects)
			{
				Vector3    positionInROS    = this.ConvertCoordinateSystemUnityToROS_Position(graspableObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoordinateSystemUnityToROS_Rotation(graspableObject.transform.rotation);

				if (graspableObject.name == currentTaskInfo.target)
				{
					objectStatus.target_object.name = graspableObject.name.Substring(0, graspableObject.name.Length - 3);
					objectStatus.target_object.position = positionInROS;
					objectStatus.target_object.orientation = orientationInROS;
				}
				else
				{
					SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
					{
						name = graspableObject.name.Substring(0, graspableObject.name.Length - 3),
						position = positionInROS,
						orientation = orientationInROS
					};

					objectStatus.non_target_objects.Add(objInfo);
				}
			}

			ExecuteEvents.Execute<IRosObjectStatusSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosObjectStatusMessage(objectStatus)
			);

			ExecuteEvents.Execute<IRosObjectStatusSendHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosObjectStatusMessage(objectStatus)
			);
		}

		private void SendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo taskInfo)
		{
			ExecuteEvents.Execute<IRosTaskInfoSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosTaskInfoMessage(taskInfo)
			);

			ExecuteEvents.Execute<IRosTaskInfoSendHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosTaskInfoMessage(taskInfo)
			);
		}

		private void JudgeGraspingObject()
		{
			this.CheckGraspOfObject(this.leftHandData);
			this.CheckGraspOfObject(this.rightHandData);
		}

//		private void CheckGraspOfObject(NewtonVR.NVRHand hand)
		private void CheckGraspOfObject(HandData hand)
		{
			if (this.preInteractingNameMap[hand.handType] == hand.currentlyInteractingName) { return; }

			this.preInteractingNameMap[hand.handType] = hand.currentlyInteractingName;

//			if (hand.currentAttachedObject != null)
//			if (hand.handTriggerState && hand.isInteracting)
			if (hand.isInteracting)
			{
//				if (hand.currentAttachedObject.tag == TagNameOfGraspables)
				if (hand.currentlyInteractingTag == TagNameOfGraspables)
				{
//					if (this.IsTargetObject(hand.currentAttachedObject.name))
					if (this.IsTargetObject(hand.currentlyInteractingName))
					{
						if (!this.isTargetAlreadyGrasped)
						{
							SIGVerseLogger.Info("Target object is grasped" + "\t" + hand.currentlyInteractingName + "\t" + this.GetElapsedTimeText());

							this.SendPanelNotice("Target object is grasped", 100, PanelNoticeStatus.Green);

							this.scoreManager.AddScore(Score.ScoreType.CorrectObjectIsGrasped);
							//this.scoreManager.AddTimeScoreOfGrasp();
							this.scoreManager.AddTimeScore();

							this.isTargetAlreadyGrasped = true;
						}

//						this.RecordEventLog("Object_Is_Grasped" + "\t" + "Target_Object" + "\t" + hand.currentAttachedObject.name);
						this.RecordEventLog("Object_Is_Grasped" + "\t" + "Target_Object" + "\t" + hand.currentlyInteractingName);
					}
					else
					{
						//if (!this.isTargetAlreadyGrasped)
						{
//							if (!this.alreadyGraspedObjects.Contains(hand.currentAttachedObject.name))
							if (!this.alreadyGraspedObjects.Contains(hand.currentlyInteractingName))
							{
								SIGVerseLogger.Info("Wrong object is grasped [new]" + "\t" + hand.currentlyInteractingName + "\t" + this.GetElapsedTimeText());

								this.SendPanelNotice("Wrong object is grasped", 100, PanelNoticeStatus.Red);

								this.scoreManager.AddScore(Score.ScoreType.IncorrectObjectIsGrasped);
								//this.scoreManager.ImposeTimePenalty(Score.TimePnaltyType.IncorrectObjectIsGrasped);

								//this.countWrongObjectsGrasp++;

//								this.alreadyGraspedObjects.Add(hand.currentAttachedObject.name);
								this.alreadyGraspedObjects.Add(hand.currentlyInteractingName);
							}
							else
							{
								SIGVerseLogger.Info("Wrong object is grasped [already grasped]" + "\t" + hand.currentlyInteractingName + "\t" + this.GetElapsedTimeText());
							}

//							this.RecordEventLog("Object_Is_Grasped" + "\t" + "Wrong_Object" + "\t" + hand.currentAttachedObject.name);
							this.RecordEventLog("Object_Is_Grasped" + "\t" + "Wrong_Object" + "\t" + hand.currentlyInteractingName);
						}
					}
				}
				else// if (hand.CurrentlyInteracting.tag != "Untagged")
				{
//					SIGVerseLogger.Info("Object_Is_Grasped" + "\t" + "Others" + "\t" + hand.currentAttachedObject.name + "\t" + this.GetElapsedTimeText());
//					this.RecordEventLog("Object_Is_Grasped" + "\t" + "Others" + "\t" + hand.currentAttachedObject.name);
					SIGVerseLogger.Info("Object_Is_Grasped" + "\t" + "Others" + "\t" + hand.currentlyInteractingName + "\t" + this.GetElapsedTimeText());
					this.RecordEventLog("Object_Is_Grasped" + "\t" + "Others" + "\t" + hand.currentlyInteractingName);
				}
			}
		}

		//public void TargetGrasp(string graspedObjectName)
		//{
		//	SIGVerseLogger.Info("Target object is grasped" + "\t" + this.GetElapsedTimeText());

		//	this.SendPanelNotice("Target object is grasped", this.fontSize, PanelNoticeStatus.Green);

		//	this.scoreManager.AddScore(Score.ScoreType.CorrectObjectIsGrasped);
		//	//this.scoreManager.AddTimeScoreOfGrasp();
		//	this.scoreManager.AddTimeScore();

		//	this.isTargetAlreadyGrasped = true;

		//	this.RecordEventLog("Object_Is_Grasped" + "\t" + "Target_Object" + "\t" + graspedObjectName);

		//	this.SendRosHumanNaviMessage("Target_grasp", "");
		//}

//		private string GetGraspingObjectId(NewtonVR.NVRHand hand)
		private string GetGraspingObjectId(HandData handData)
		{
			string graspingObjectId = "";

//			if(handData.handTriggerState && handData.isInteracting)
			if(handData.isInteracting)
			{
				if (handData.currentlyInteractingTag == TagNameOfGraspables)
				{
					graspingObjectId = handData.currentlyInteractingName;
				}
			}

			//if (hand.HoldButtonPressed)
			//{
			//	if (hand.IsInteracting)
			//	{
			//		if (hand.CurrentlyInteracting.tag == TagNameOfGraspables)
			//		{
			//			graspingObject = hand.CurrentlyInteracting.name;
			//		}
			//	}
			//}

			return graspingObjectId;
		}

		private bool IsTargetObject(string objectLabel)
		{
			if (objectLabel == this.currentTaskInfo.target) return true;
			else                                            return false;
		}

//		private void CheckGraspingStatus(NewtonVR.NVRHand hand)
		private void CheckGraspingStatus(HandData hand)
		{
//			if (SteamVR_Actions.sigverse_PressMiddle.GetStateDown(hand.handType))
			if (hand.handTriggerDownForModerator)
			{
				SIGVerseLogger.Info("HandInteraction" + "\t" + "HoldButtonDown" + "\t" + hand.name + "\t" + this.GetElapsedTimeText());
				this.RecordEventLog("HandInteraction" + "\t" + "HoldButtonDown" + "\t" + hand.name);
			}

//			if (SteamVR_Actions.sigverse_PressMiddle.GetStateUp(hand.handType))
			if (hand.handTriggerUpForModerator)
			{
				string objectInhand = "";
				if(hand.handType == HandData.HandType.LeftHand) { objectInhand = this.objectIdInLeftHandPreviousFrame; }
				else                                            { objectInhand = this.objectIdInRightHandPreviousFrame; }

				if(objectInhand != "")
				{
					SIGVerseLogger.Info("HandInteraction" + "\t" + "ReleaseObject" + "\t" + hand.name + "\t" + this.GetElapsedTimeText());
					this.RecordEventLog("HandInteraction" + "\t" + "ReleaseObject" + "\t" + hand.name + "\t" + objectInhand);
				}
				else
				{
					SIGVerseLogger.Info("HandInteraction" + "\t" + "HoldButtonUp" + "\t" + hand.name + "\t" + this.GetElapsedTimeText());
					this.RecordEventLog("HandInteraction" + "\t" + "HoldButtonUp" + "\t" + hand.name);
				}
			}
		}

		private void JudgeDistanceFromTargetObject()
		{
			if (!this.scoreManager.IsAlreadyGivenDistancePenaltyForTargetObject())
			{
				float distanceFromTargetObject = this.sessionManager.GetDistanceFromRobot(this.initialTargetObjectPosition);
				if (distanceFromTargetObject < this.scoreManager.limitDistanceFromTarget)
				{
					this.scoreManager.AddDistancePenaltyForTargetObject();
				}
			}
		}
		private void JudgeDistanceFromDestination()
		{
			if (!this.scoreManager.IsAlreadyGivenDistancePenaltyForDestination())
			{
				float distanceFromDestination = this.sessionManager.GetDistanceFromRobot(this.initialDestinationPosition);
				if (distanceFromDestination < this.scoreManager.limitDistanceFromTarget)
				{
					this.scoreManager.AddDistancePenaltyForDestination();
				}
			}
		}

		public string GetTargetObjectName()
		{
			return this.currentTaskInfo.target;
		}

		public string GetDestinationName()
		{
			return this.currentTaskInfo.destination;
		}

		public bool IsTargetAlreadyGrasped()
		{
			return this.isTargetAlreadyGrasped;
		}

		public void TargetPlacedOnDestination()
		{
			if(!this.isDuringSession)
			{
				return;
			} 

			SIGVerseLogger.Info("Target is plasced on the destination." + "\t" + this.GetElapsedTimeText());
			this.RecordEventLog("Target is plasced on the destination.");

			this.scoreManager.AddScore(Score.ScoreType.TargetObjectInDestination);
			//this.scoreManager.AddTimeScoreOfPlacement();
			this.scoreManager.AddTimeScore();

			this.SendRosHumanNaviMessage(MsgTaskSucceeded, "");
			this.SendPanelNotice("Task succeeded", 100, PanelNoticeStatus.Green);
			this.ShowNoticeMessagePanelForAvatar("Task succeeded", 10.0f);

			this.TaskFinished();
		}

		public bool IsTargetGrasped()
		{
//			bool isGraspedByLeftHand  = this.LeftHand .currentAttachedObject != null && this.IsTargetObject(this.LeftHand .currentAttachedObject.name);
//			bool isGraspedByRightHand = this.rightHand.currentAttachedObject != null && this.IsTargetObject(this.rightHand.currentAttachedObject.name);
			bool isGraspedByLeftHand  = this.leftHandData .isInteracting && this.IsTargetObject(this.leftHandData .currentlyInteractingName);
			bool isGraspedByRightHand = this.rightHandData.isInteracting && this.IsTargetObject(this.rightHandData.currentlyInteractingName);

			return isGraspedByLeftHand || isGraspedByRightHand;
		}

		public void OnTimeIsUp()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient){ return; }

			this.interruptedReason = HumanNaviModerator.ReasonTimeIsUp;
		}

		public void OnGiveUp()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient){ return; }

			if (this.isDuringSession)
			{
				this.interruptedReason = HumanNaviModerator.ReasonGiveUp;

				this.SendRosHumanNaviMessage(MsgTaskFailed, ReasonGiveUp);
				this.SendPanelNotice("Give up", 100, PanelNoticeStatus.Red);
				this.ShowNoticeMessagePanelForAvatar("Give up", 3.0f);

				this.panelMainController.giveUpPanel.SetActive(false);

				this.TaskFinished();
			}
			else
			{
				SIGVerseLogger.Warn("It is a timing not allowed to give up.");
			}
		}


		public void OnReachMaxWrongObjectGraspCount()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient){ return; }

			this.interruptedReason = HumanNaviModerator.ReasonReachMaxWrongObjectGraspCount;

			string strReason = "Reach_max_wrong_object_grasp_count";
			this.SendRosHumanNaviMessage(MsgTaskFailed, strReason);
			this.SendPanelNotice("Reach max wrong object grasp count", 100, PanelNoticeStatus.Red);

			this.ShowNoticeMessagePanelForAvatar("Reach max wrong object grasp count", 3.0f);

			this.TaskFinished();
		}

		public void OnStartTrial()
		{
			this.StartTrial(); // Execute only client side. The Server side will be executed later.
		}

		[PunRPC]
		public void StartTrial()
		{
			SIGVerseLogger.Info("Task start!");
			//this.RecordEventLog("Task_start");

			this.startTrialPanel.SetActive(false);
			this.isCompetitionStarted = true;
		}

		public void OnGoToNextTrial()
		{
			this.goToNextTrialPanel.SetActive(false);

			this.isCompetitionStarted = true; // for practice mode
		}

		private void ShowStartTaskPanel()
		{
			if (this.isPracticeMode)
			{
				this.goToNextTrialPanel.SetActive(true);
			}
			else
			{
				this.startTrialPanel.SetActive(true);
			}
		}

		private void SendPanelNotice(string message, int fontSize, Color color, float duration = 3.0f, bool shouldSendToPlaybackManager = true)
		{
			PanelNoticeStatus noticeStatus = new PanelNoticeStatus(message, fontSize, color, duration);

			// For changing the notice of a panel
			ExecuteEvents.Execute<IPanelNoticeHandler>
			(
				target: this.mainMenu,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnPanelNoticeChange(noticeStatus)
			);

			if (shouldSendToPlaybackManager)
			{
				// For recording
				ExecuteEvents.Execute<IPanelNoticeHandler>
				(
					target: this.playbackManager,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnPanelNoticeChange(noticeStatus)
				);
			}
		}

		public IEnumerator DisplayEndMessage()
		{
			yield return new WaitForSecondsRealtime(7);

			string endMessage = "All sessions have ended";

			SIGVerseLogger.Info(endMessage);

			this.SendPanelNotice(endMessage, 80, PanelNoticeStatus.Blue, 600f, false);
		}


		private void RecordEventLog(string log)
		{
			// For recording
			ExecuteEvents.Execute<IRecordEventHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnRecordEvent(log)
			);
		}

		private string GetElapsedTimeText()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				return Math.Round(this.playbackRecorder.GetElapsedTime(), 4, MidpointRounding.AwayFromZero).ToString();
			}
			else
			{
				return Math.Round(this.scoreManager.GetElapsedTime(), 4, MidpointRounding.AwayFromZero).ToString();
			}
		}

		private bool IsConnectedToRos()
		{
			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				if (!rosConnection.IsConnected())
				{
					return false;
				}
			}
			return true;
		}

		private IEnumerator ClearRosConnections()
		{
			yield return new WaitForSecondsRealtime(1.5f);

			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				//Debug.Log(rosConnection.ToString());
				rosConnection.Clear();
			}

			SIGVerseLogger.Info("Clear ROS connections");
		}

		private IEnumerator CloseRosConnections()
		{
			yield return new WaitForSecondsRealtime(1.5f);

			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				rosConnection.Close();
			}

			SIGVerseLogger.Info("Close ROS connections");
		}

		private IEnumerator StopTime()
		{
			yield return new WaitForSecondsRealtime(1.0f);

			Time.timeScale = 0.0f;
		}
	}
}
