# WindowsServiceForEFLOWLeaf
Code for creating a Windows Service to run as Leaf Device(s) on the Windows Host VM of an EFLOW Instance

**Aim**

Build a supportable replacement for Windows dependent IoT Edge 1.1 Windows container workloads.

**Architecture to replace**

IoT Edge 1.1 Windows on Windows with multiple data extraction containers per IoT Edge Instance. These containers all use the same image but have different configurations set at Module Twin Level.

**Target Architecture**

EFLOW running on Azure Windows Server VM. Multiple Windows Services running on the Windows Host acting as "Leaf" devices to EFLOW. All services run from the same executable code but can be configured from the command line at service creation time. (Stretch target - add Device Twin Configuration too)

**Process**

1 - Set up Azure VM and Eflow

1.1 Create an Azure VM from a Windows Server 2019 image. I used a Standard D4s v3 to ensure I had enough overhead.

1.2 Make sure you have an IoT Hub with an IoT Edge device created, you'll need the connection string for this device later in the process

1.3 Set up nested virtualisation, it's easiest to do this from Powershell (as Administrator)

•	Install-WindowsFeature -Name Hyper-V -ComputerName <computer_name> -IncludeManagementTools -Restart
•	Get-WindowsFeature -ComputerName <computer_name>

1.4 Follow https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-virtual-switch?view=iotedge-2020-11 to set up a virtual switch. As this is an Azure VM you cannot create an external switch. You do not need to create a DNS server as the EFLOW Linux VM will be allocated a static IP address later in the process. This IP address will be one between the Start and End IP range values that you set in step 6 of the Create Virtual Switch section.

1.5 Follow https://docs.microsoft.com/en-us/azure/iot-edge/how-to-provision-single-device-linux-on-windows-symmetric?view=iotedge-2020-11&tabs=azure-portal%2Cpowershell#install-iot-edge to install EFLOW. When you get to Step 4, you'll need to run an extended command to set up the static IP Address. It's also worth using the parameters in the green box to give the EFLOW VM some more power.

Deploy-Eflow -cpuCount 1 -memoryInMB 1024 -vmDiskSize 2 -ip4Address xxx.yyy.zzz.aaa -ip4PrefixLength

Deploy-Eflow -vswitchType External -vswitchName EflowExtSwitch -ip4Address 192.168.2.155 -ip4PrefixLength 24 -ip4GatewayAddress 192.168.2.66


2 - Create Windows Service Code

3 - Deploy Windows Service Code

4 - Create and Deploy CA Certificates (IoT Edge and Windows Host)

5 - Create Leaf Devices under IoT Edge

6 - Create Service Instances

7 - Start and Stop Service Instances


