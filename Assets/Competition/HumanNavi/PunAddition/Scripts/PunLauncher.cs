using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGVerse.Common;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Linq;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if SIGVERSE_PUN
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
#endif

namespace SIGVerse.Competition.HumanNavigation
{
	public class PunLauncher : MonoBehaviourPunCallbacks
	{
		public const string HumanName = "Ethan";
		public const string RobotName = "HSR";

		[HeaderAttribute("Objects")]
		public GameObject human;
		public GameObject robot;

		public HumanNaviSessionManager sessionManager;

		public Button startButton;

		[HeaderAttribute("Graspables")]
		public GameObject[] rootsOfSyncTarget;

		public List<GameObject> roomObjects;

		// -----------------------

#if SIGVERSE_PUN
		private List<GameObject> graspables;

		void Awake()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.NotUsed) 
			{ 
				PhotonNetwork.OfflineMode = true;
				return; 
			}

			PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = HumanNaviConfig.Instance.configInfo.punAppIdPun;
			PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = HumanNaviConfig.Instance.configInfo.punUseNameServer;
			PhotonNetwork.PhotonServerSettings.AppSettings.Server        = HumanNaviConfig.Instance.configInfo.punServer;
			PhotonNetwork.PhotonServerSettings.AppSettings.Port          = HumanNaviConfig.Instance.configInfo.punPort;

			this.startButton.interactable = false;

			if(HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunServer)
			{
				this.startButton.gameObject.GetComponentInChildren<Text>().text = "Invalid";
			}
		}

		void Start()
		{
			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.NotUsed) { return; }

			PhotonNetwork.AutomaticallySyncScene = true;

			// Check for duplication
			List<string> duplicateNames = this.roomObjects.GroupBy(obj => obj.name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

			if (duplicateNames.Count > 0)
			{
//				throw new Exception("There are multiple objects with the same name. e.g. " + duplicateNames[0]);
			}

			// Manage the synchronized room objects using singleton
			RoomObjectManager.Instance.roomObjects = this.roomObjects;

			this.Connect();
		}

		public void Connect()
		{
			SIGVerseLogger.Info("PUN Connect Start");

			PhotonNetwork.GameVersion = PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion;

			if (!PhotonNetwork.ConnectUsingSettings())
			{
				SIGVerseLogger.Error("Failed to connect Photon Server.");
			}
		}

		public override void OnConnectedToMaster()
		{
			PhotonNetwork.JoinOrCreateRoom("HumanNaviRoom" + String.Format("{0:D3}", HumanNaviConfig.Instance.numberOfTrials), new RoomOptions(), TypedLobby.Default);
		}


		public override void OnJoinedRoom()
		{
			Application.runInBackground = true;

			this.graspables = GameObject.FindGameObjectsWithTag("Graspables").ToList();

			if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunClient)
			{
				foreach(PhotonView humanPhotonView in this.human.GetComponentsInChildren<PhotonView>())
				{
					humanPhotonView.TransferOwnership(PhotonNetwork.LocalPlayer);
				}

				this.sessionManager.TransferOwnershipOfDefaultEnvironment();

				PhotonNetwork.NickName = HumanName;
			}
			else if (HumanNaviConfig.Instance.configInfo.PunMode() == PunMode.PunServer)
			{
				foreach(PhotonView robotPhotonView in this.robot.GetComponentsInChildren<PhotonView>())
				{
					robotPhotonView.TransferOwnership(PhotonNetwork.LocalPlayer);
				}

				PhotonNetwork.NickName = RobotName;

				foreach (GameObject roomObject in this.roomObjects)
				{
					Rigidbody rigidbodyGraspable = roomObject.GetComponent<Rigidbody>();
					rigidbodyGraspable.useGravity = false;
					rigidbodyGraspable.collisionDetectionMode = CollisionDetectionMode.Discrete;
					rigidbodyGraspable.isKinematic = true;
//					rigidbodyGraspable.constraints = RigidbodyConstraints.FreezeAll;
				}
			}

//			this.mainPanel.SetActive(false);

			if (HumanNaviConfig.Instance.configInfo.PunMode() != PunMode.PunServer)
			{
				StartCoroutine(this.EnableStartButton());
			}

			SIGVerseLogger.Info("PUN OnJoinedRoom End");
		}

		private IEnumerator EnableStartButton()
		{
			while(PhotonNetwork.CurrentRoom.PlayerCount!=2 || PhotonNetwork.PlayerListOthers[0].NickName!=PunLauncher.RobotName)
			{
				yield return null;
			}

			this.startButton.interactable = true;
		}

		public IEnumerator DisconnectAndLoadScene()
		{
			if(HumanNaviConfig.Instance.configInfo.PunMode() != PunMode.PunServer)
			{
				SIGVerseLogger.Error("This method should only be called from the server side.");
				yield break;
			}

			this.photonView.RPC(nameof(DisconnectAndLoadSceneRPC), RpcTarget.Others); // Disconnect client side

			while(PhotonNetwork.CurrentRoom.PlayerCount!=1)
			{
				yield return null;
			}

			this.DisconnectAndLoadSceneRPC(); // Disconnect server side
		}

		[PunRPC]
		private void DisconnectAndLoadSceneRPC()
		{
			PhotonNetwork.Disconnect();
		}

		public override void OnDisconnected(DisconnectCause cause)
		{
			SIGVerseLogger.Warn("Disconnected from PUN");

			if (HumanNaviConfig.Instance.numberOfTrials != HumanNaviConfig.Instance.configInfo.maxNumberOfTrials)
			{
				SceneManager.LoadScene(SceneManager.GetActiveScene().name);
			}
		}

		//public void DesableRigidbody(string objectName)
		//{
		//	GameObject graspedObj = this.graspables.SingleOrDefault(obj => obj.name == objectName);

		//	if (graspedObj.name == objectName)
		//	{
		//		if (!graspedObj.GetComponentInChildren<PhotonView>().IsMine)
		//		{
		//			graspedObj.GetComponent<Rigidbody>().useGravity = false;
		//			graspedObj.GetComponent<Rigidbody>().isKinematic = true;
		//		}
		//	}
		//}

		public void SetRoomObjects(List<GameObject> roomObjects)
		{
			this.roomObjects = roomObjects;
		}

		//public static void AddPhotonTransformView(PhotonView photonView, GameObject synchronizedTarget, bool syncPos = false, bool syncRot = true)
		//{
		//	PhotonTransformView photonTransformView = synchronizedTarget.AddComponent<PhotonTransformView>();

		//	photonTransformView.m_SynchronizePosition = syncPos;
		//	photonTransformView.m_SynchronizeRotation = syncRot;
		//	photonTransformView.m_SynchronizeScale = false;

		//	photonView.ObservedComponents.Add(photonTransformView);
		//}
#endif
	}

#if SIGVERSE_PUN && UNITY_EDITOR
	[CustomEditor(typeof(PunLauncher))]
	public class PunLauncherEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			PunLauncher punLauncher = (PunLauncher)target;

			base.OnInspectorGUI();

			GUILayout.Space(10);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Update Photon Scripts", GUILayout.Width(200), GUILayout.Height(40)))
				{
					Undo.RecordObject(target, "Update Photon Scripts");

					// Remove photon scripts
					RemoveScripts<PhotonTransformView>();
					RemoveScripts<PhotonRigidbodyView>();
					RemoveScripts<PhotonView>();
//					RemoveScripts<PunOwnerChangerForObject>();

					// Add photon scripts
					List<GameObject> roomObjects = new List<GameObject>();

					// GraspableObjects
					foreach (GameObject sourceOfSyncTarget in punLauncher.rootsOfSyncTarget)
					{
						PhotonView photonView = Undo.AddComponent<PhotonView>(sourceOfSyncTarget);
						photonView.OwnershipTransfer = OwnershipOption.Takeover;
						photonView.Synchronization = ViewSynchronization.ReliableDeltaCompressed;
						photonView.observableSearch = PhotonView.ObservableSearch.AutoFindAll;

						Rigidbody[] syncTargetRigidbodies = sourceOfSyncTarget.GetComponentsInChildren<Rigidbody>();

						foreach (Rigidbody syncTargetRigidbody in syncTargetRigidbodies)
						{
							roomObjects.Add(syncTargetRigidbody.gameObject);
						}
					}

					// RoomLayout
					foreach (GameObject rootOfGraspables in punLauncher.rootsOfSyncTarget)
					{
						GameObject rootOfRoomLayout = rootOfGraspables.transform.parent.Find("RoomLayout").gameObject;

						PhotonView photonView = Undo.AddComponent<PhotonView>(rootOfRoomLayout);
						photonView.OwnershipTransfer = OwnershipOption.Takeover;
						photonView.Synchronization = ViewSynchronization.ReliableDeltaCompressed;
						photonView.observableSearch = PhotonView.ObservableSearch.AutoFindAll;

						Rigidbody[] syncTargetRigidbodies = rootOfRoomLayout.GetComponentsInChildren<Rigidbody>();

						foreach (Rigidbody syncTargetRigidbody in syncTargetRigidbodies)
						{
							roomObjects.Add(syncTargetRigidbody.gameObject);
						}
					}

					punLauncher.SetRoomObjects(roomObjects);

					foreach (GameObject roomObject in roomObjects)
					{
						Undo.AddComponent<PhotonTransformView>(roomObject);
					}

					Debug.Log("Updated Photon Scripts");
				}

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
		}

		private void RemoveScripts<T>() where T : Component
		{
			PunLauncher punLauncher = (PunLauncher)target;

			foreach (GameObject sourceOfSyncTarget in punLauncher.rootsOfSyncTarget)
			{
				T[] photonScripts = sourceOfSyncTarget.transform.parent.GetComponentsInChildren<T>();

				foreach (T photonScript in photonScripts)
				{
					Undo.DestroyObjectImmediate(photonScript);
				}
			}
		}
	}
#endif
}
