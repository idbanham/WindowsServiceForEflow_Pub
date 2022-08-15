# WindowsServiceForEFLOWLeaf
Code for creating a Windows Service to run as Leaf Device(s) on the Windows Host VM of an EFLOW Instance

**Aim**

Build a supportable replacement for Windows dependent IoT Edge 1.1 Windows container workloads.

## Architecture to replace

IoT Edge 1.1 Windows on Windows with multiple data extraction containers per IoT Edge Instance. These containers all use the same image but have different configurations set at Module Twin Level.

## Target Architecture

EFLOW running on Azure Windows Server VM. Multiple Windows Services running on the Windows Host acting as "Leaf" devices to EFLOW. All services run from the same executable code but can be configured from the command line at service creation time. (Stretch target - add Device Twin Configuration too)

## Process##

### 1 - Set up Azure VM and Eflow

1.1. Create an Azure VM from a Windows Server 2019 image. I used a Standard D4s v3 to ensure I had enough overhead.

1.2. Make sure you have an IoT Hub with an IoT Edge device created, you'll need the connection string for this device later in the process

1.3. Set up nested virtualisation, it's easiest to do this from Powershell (as Administrator), run these two commands, <computer name> parameter not required if running from the machine you're looking to affect.

>Install-WindowsFeature -Name Hyper-V -ComputerName <computer_name> -IncludeManagementTools -Restart
>Get-WindowsFeature -ComputerName <computer_name>

1.4. Follow https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-virtual-switch?view=iotedge-2020-11 to set up a virtual switch. As this is an Azure VM you cannot create an external switch. You do not need to create a DNS server as the EFLOW Linux VM will be allocated a static IP address later in the process. This IP address will be one between the Start and End IP range values that you set in step 6 of the Create Virtual Switch section.

1.5. Follow https://docs.microsoft.com/en-us/azure/iot-edge/how-to-provision-single-device-linux-on-windows-symmetric?view=iotedge-2020-11&tabs=azure-portal%2Cpowershell#install-iot-edge to install EFLOW. When you get to Step 4, you'll need to run an extended command to set up the static IP Address. It's also worth using the parameters in the green box to give the EFLOW VM some more power.

>Deploy-Eflow -cpuCount 1 -memoryInMB 1024 -vmDiskSize 2 -vswitchType Internal -vswitchName DefaultSwitch -ip4Address xxx.yyy.zzz.aaa -ip4PrefixLength 24 -ip4GatewayAddress <Gateway IP>

1.6. Issue the remaining commands in the document to provision the EFLOW device to your IoT Hub.

### 2 - Create and Deploy CA Certificates (IoT Edge and Windows Host)

As IoT Edge will be running as a transparent gateway, IoT Edge needs to present the "leaf devices" (i.e. Windows service code) with a valid trusted certificate to enable TLS communication between the leaf devices and IoT Edge. This certificate has nothing to do with device authentication and is purely for TLS purposes. By default IoT Edge creates its own certificates for communication with/between modules but as you need a copy of the root certificate on the device its easier to build your own certificates for this purpose. You can also give these certificates a far longer life than the default 90 days of the ones that IoT Edge creates automatically.

To create the certificates for test purposes, Microsoft has supplied a set of convenience scripts that will generate certificates that work… by default the certificate password is insecure and the certificate only has a 30 day life. You can edit the script to improve this.

 The instructions for using these scripts are at https://docs.microsoft.com/en-gb/azure/iot-edge/how-to-create-test-certificates?view=iotedge-2020-11
For this scenario where we’re creating the certificates for TLS communications between IoT Edge and either Modules or Devices you only need to create the certificates specified in the following sections
 
https://docs.microsoft.com/en-gb/azure/iot-edge/how-to-create-test-certificates?view=iotedge-2020-11#create-root-ca-certificate 
https://docs.microsoft.com/en-gb/azure/iot-edge/how-to-create-test-certificates?view=iotedge-2020-11#create-iot-edge-ca-certificates 

For the device name part of the command, DO NOT use the hostname of your IoT Edge Device. Use anything else but that. When I ran the scripts, I used “EdgeDevice” as the device name. You can run these scripts anywhere, it doesn’t affect their usage.

The only certificate files you need at the end of this process are listed below. Note that the middle one is hiding in the “private” directory not “certs”:
 /certs/iot-edge-device-EdgeDevice-full-chain.cert.pem  
 /private/iot-edge-device-EdgeDevice.key.pem    
 /certs/azure-iot-test-only.root.ca.cert.pem 
 
### 2.1 Install Certificates on IoT Edge
 
1.	Login to Eflow device – do this from a Powershell on the Windows machine hosting EFLOW. Command is Connect-EflowVm. You’re now in the Linux environment.

2.	Create some directories for the certificates – I just put them in the eflow-user’s home directory.

  >a.	cd ~
  >b.	mkdir certs
  >c.	cd certs
  >d.	mkdir certs
  >e.	mkdir private
  
3.	Type Exit to go back to the Powershell environment

4.	Make sure you have the certificates listed above that you created copied to the Windows machine

5.	Use the Copy-EflowVMFile command to copy the three certificate files from the Windows environment to the EFLOW VM:

  >a.	Copy-EflowVMFile -fromFile c:\Edgecerts\certs\iot-edge-device-EdgeDevice-full-chain.cert.pem -toFile /home/iotedge-user/certs/certs/iot-edge-device-EdgeDevice-full-chain.cert.pem -pushFile
  
  >b.	Repeat for other two certificate files
  
6.	Connect to the EFLOW VM again and change the permissions of the certificate files and the directories they reside in as the command above copies them across with only root access permissions. I just used sudo chmod 777 *.pem type commands as this was a demo environment. For something more realistic look at using the chown command to change the owner of the files to iotedge-user.

7.	Edit the /etc/iotedge/config.yaml file using the nano text editor. The Yaml file is VERY sensitive to format.
  >a.	sudo nano /etc/iotedge/config.yaml
  
  >b.	scroll down to the certificates section, modify it so it looks like the image below. The word Certificates must have no leading spaces, the lines below all must have two leading spaces
  
  ![image](https://user-images.githubusercontent.com/41492097/184645463-d777e380-b800-4f5f-9946-19e55f06bb9d.png)

 
8.	Save the file (Ctrl X)

9.	If you’ve already had IoT Edge running, delete the contents of these directories :

  >a.	/var/lib/iotedge/hsm certs
  >b.	/var/lib/iotedge/hsm cert_keys
  
10.	Run sudo iotedge check – ensure that the config yaml is well formed and that it doesn’t contain a message like:

**"‼ production readiness: certificates - Warning
The Edge device is using self-signed automatically-generated development certificates.
They will expire in 68 days (at 2022-04-13 10:08:14 UTC) causing module-to-module and downstream device communication to fail on an active deployment."**

11.	If it does, your configuration in config.yaml is incorrect. I’ve seen files that pass the IoTEdge Check “well formed YAML” test but still fail to read this section and so use the internal self-generated certificates. In this case there were too many spaces at the beginning of each line.

12.	Once you’re getting the correct result from iotedge check, Restart IoT Edge – sudo systemctl restart iotedge

13.	Check it’s running – sudo iotedge list

14.	If it’s not running, check the file names and paths are correct and that the iotedge-user can see the certificate files.

 
 
### 2.2 Install Certificate on Windows Host
 
The devices need to trust the certificate that IoT Edge presents to them. To do this you need to install the azure-iot-test-only.root.ca.cert.pem certificate in the Windows Certificate Store for the Machine. Copy the file to the Azure VM hosting the EFLOW instance and then use the Windows Certificate Import Wizard to save the file into the Trusted store. Make sure you do this for the machine, not the current user.

## 3 - Add EFLOW VM IP Address to Windows VM Hosts file
 
1. Check the name of the EFLOW VM by running the Get-EflowVmName command from an administrator Powershell session on the Windows VM
1. Edit the c:\windows\system32\drivers\etc\hosts file to add the EFLOW VM name and IP Address
 
##4. Create Windows Service Code

The code in this repository works and should just need compiling. There are a couple of items to note in the code:

 1. The example was built on top of a public Microsoft example. Most of the core code is still there and the code puts jokes in the log every 10 minutes. With a more skilled coder, this could be removed.
 
 2. WindowsBackgroundService.cs - Line 14. If wanting to test the code directly you can put an IoT Hub connection string in this line and comment out line 15.
 
 3. WindowsBackgroundService.cs - Line 32. This is commented out as it's not required as the certificate is already installed in the trust store. The InstallACert code doesn't work when run in a service as it triggers a UI action that doesn't get surfaced.

## 4 Deploy Windows Service Code
 
Compile and publish the project, zip up the published code and copy to a directory on the Windows Host VM.

## 5 Create Leaf Devices under IoT Edge
 
In the Azure Portal create some devices under your IoT Hub and set them as child devices of your IoT Edge instance. Copy the connection strings for each device.

## 6 Create Service Instances

Copy the ServicesCreateList.txt file from the WindowsConfig directory of this repo to your Windows Host machine. Edit the file to set your device names and connection strings. Note that you need to add a "GatewayHostname" parameter with the name of your EFLOW VM to the end of the connection string. Without that parameter, the device would connect directly to IoT hub without going through IoT Edge.
 
Once edited, change the extension of the file to .bat if you want to run it without copying and pasting individual lines.

## 7 Start and Stop Service Instances

Open the Services tool, the services should be listed but not running. right click on a service to open the context menu and select start, the service should start sending data to IoT Hub via the EFLOW IoT Edge instance. Repeat for the other service instances.
 
You can change other service parameters to set up auto-start and recovery options.


