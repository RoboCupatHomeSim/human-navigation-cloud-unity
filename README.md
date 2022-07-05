# Human Navigation Cloud Edition

This is the cloud edition of Human Navigation for **administrators**.

## Prerequisites

In addition to the Human Navigation prerequisite, PUN2(Photon Unity Networking 2) is also required.  
For more information on PUN2, please refer to the following page on SIGVerse.  
http://www.sigverse.org/wiki/en/index.php?%28HSR%29Cleanup%20Task%20using%20Cloud%20and%20VR

## How to Build

### Import unitypackages

1. Follow the normal Human Navigation procedure to import unitypackages.  
https://github.com/RoboCupatHomeSim/human-navigation-unity#import-unitypackages  
1. Import PUN2 package.
	1. Go to the following page.  
	https://assetstore.unity.com/packages/tools/network/pun-2-free-119922
	1. Download and import the asset.  
	But the following should be unchecked when importing because these conflicts with other libraries.  
		- Photon/PhotonLibs/WebSocket/websocket-sharp.dll
		- Photon/PhotonUnityNetworking/Demos
		- SteamVR
1. Click [Assets]-[Reimport All].
1. Click [Reimport] button.
1. Backup PUN2 package.  
Only the latest version of PUN2 is distributed in the Asset Store, so make a backup.  
On Windows, it exists in the following location  
`C:\Users\accountName\AppData\Roaming\Unity\Asset Store-5.x\Exit Games\ScriptingNetwork\PUN 2 - FREE.unitypackage`

### Import executable file and dll for TTS
1. Prepare "ConsoleSimpleTTS.exe" and "Interop.SpeechLib.dll".
2. Copy those files to the [TTS] folder in the same directory as README.md.


### Build for the Server
1. **Uncheck** [Project Settings]-[XR Plug-in Management]-[Initialize XR on Startup] checkbox.
1. Check [Project Settings]-[XR Plug-in Management]-[OpenVR Loader] checkbox.
1. Build.
1. Open the built file "Human Navigation_Data/boot.config".
1. Delete the line "xrsdk-pre-init-library=XRSDKOpenVR".
1. Copy the "TTS" folder into the build folder.

### Build for the Client
1. Check [Project Settings]-[XR Plug-in Management]-[Initialize XR on Startup] checkbox.
1. Check [Project Settings]-[XR Plug-in Management]-[OpenVR Loader] checkbox.
1. Build.
1. Copy the "TTS" folder into the build folder.

## How to Set Up

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

## License

This project is licensed under the SIGVerse License - see the LICENSE.txt file for details.
