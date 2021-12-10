# Human Navigation Cloud Edition

This is the cloud edition of Human Navigation for administrators.

## How to Build

### Server side
1. Uncheck [Project Settings]-[XR Plug-in Management]-[Initialize XR on Startup] checkbox, and then build.
1. Open the file "Human Navigation_Data/boot.config".
1. Delete the line "xrsdk-pre-init-library=XRSDKOpenVR".

### Client side
1. Check [Project Settings]-[XR Plug-in Management]-[Initialize XR on Startup] checkbox, and then build.

## How to Set Up

### SIGVerseConfig.json of the server side

| key | val|
| --- | --- |
| rosbridgeIP | IP address of the Ubuntu server |

### HumanNaviConfig.json of the client side

| key | val|
| --- | --- |
| punServer | IP address of the Windows server |

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
