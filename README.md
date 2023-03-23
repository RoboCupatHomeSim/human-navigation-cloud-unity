# Human Navigation Cloud Edition

This is the cloud edition of Human Navigation for **administrators**.

## Prerequisites

In addition to the Human Navigation prerequisite, PUN2(Photon Unity Networking 2) is also required.  
For more information on PUN2, please refer to the following page on SIGVerse.  
http://www.sigverse.org/wiki/en/index.php?%28HSR%29Cleanup%20Task%20using%20Cloud%20and%20VR

Photon Cloud is **NOT** used. Launch Photon Server on a Windows machine.  
For more information about Photon Server, please refer to the link above.

## How to Build

### Import unitypackages

1. Download SteamVR Unity Plugin v2.7.3 from the following link.  
https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage
1. Open this project with Unity.
1. Click [**Continue**] in the [Unity Package Manager Error] window.
1. Click [**Ignore**] in the [Enter Safe Mode?] window.
1. Click [Assets]-[Import Package]-[Custom Package...].
1. Select a common unitypackage (e.g. robocup-common.unitypackage) and open the file.
1. Click [Import] button.
1. Click [Assets]-[Import Package]-[Custom Package...].
1. Select the steamvr_2_7_3.unitypackage and open the file.
1. Click [Import] button.
1. Import PUN2 package.
	1. Go to the following page.  
	https://assetstore.unity.com/packages/tools/network/pun-2-free-119922
	1. Download and import the asset.  
	But the following should be unchecked when importing because these conflicts with other libraries.  
		- Photon/PhotonLibs/WebSocket/websocket-sharp.dll
		- Photon/PhotonUnityNetworking/Demos
		- SteamVR
	1. Backup PUN2 package.  
	Only the latest version of PUN2 is distributed in the Asset Store, so make a backup.  
	On Windows, it exists in the following location  
	`C:\Users\accountName\AppData\Roaming\Unity\Asset Store-5.x\Exit Games\ScriptingNetwork\PUN 2 - FREE.unitypackage`
1. Click [Assets]-[**Reimport All**].
1. Click [**Reimport**] button.
1. In the "PUN Setup" window, please [Skip] and then [Close].
1. Click [Edit]-[Project Settings...].
1. Check [XR Plug-in Management]-[Initialize XR on Startup].
1. Check [XR Plug-in Management]-[Plug-in Providers]-[OpenVR Loader].
1. Close [Project Settings] Window.
1. Please confirm that no error occurred in Console window.

### Import executable file and dll for TTS
1. Prepare "ConsoleSimpleTTS.exe" and "Interop.SpeechLib.dll".
2. Copy those files to the [TTS] folder in the same directory as README.md.


### Build for the Server
1. **Uncheck** [Project Settings]-[XR Plug-in Management]-[Initialize XR on Startup] checkbox.
1. Check [Project Settings]-[XR Plug-in Management]-[OpenVR Loader] checkbox.
1. Build.
1. Open the built file "Human Navigation_Data/boot.config".
1. Delete the line "xrsdk-pre-init-library=XRSDKOpenVR".  
(This is related to the following issue  
https://github.com/ValveSoftware/unity-xr-plugin/issues/80)
1. Copy the "TTS" folder into the build folder.

### Build for the Client
1. Check [Project Settings]-[XR Plug-in Management]-[Initialize XR on Startup] checkbox.
1. Check [Project Settings]-[XR Plug-in Management]-[OpenVR Loader] checkbox.
1. Build.
1. Copy the "TTS" folder into the build folder.

## How to Set Up

There are two example config files on GitHub, one for the server (HumanNaviConfig_server-side.json) and one for the client (HumanNaviConfig_client-side.json).  
Please rewrite HumanNaviConfig.json with reference to these files.

Also note that in the competition, recoverUsingScoreFile=true.  
Please refer to another document for detailed configuration of the config file.

### SIGVerseConfig.json of the server side

| key | val|
| --- | --- |
| rosbridgeIP | IP address of the Ubuntu server |

### HumanNaviConfig.json of the client side

| key | val|
| --- | --- |
| punServer | IP address of the PUN server (Windows server) |

## How to Execute

1. Connect Oculus Quest 2 to your local PC.
1. Launch the Oculus software and the SteamVR on your local PC.
1. Start the Photon Server on the Windows server.
1. Start the ROS software on the Ubuntu server.
1. Start the Human Navigation server side on the Windows server.
1. Start the Human Navigation client side on your local PC.
1. Click the [Session Start] button of the Human Navigation client side.

## Notes
- Playback function is not yet completed in this Cloud version.  
If you want to playback, please use the normal version of Human Navigation.
- If you want to use the translation feature, see below.  
https://github.com/RoboCupatHomeSim/human-navigation-ros/wiki/RosMessage#guidance-message  
https://github.com/RoboCupatHomeSim/translation-library-for-human-navi/wiki


## License

This project is licensed under the SIGVerse License - see the LICENSE.txt file for details.
