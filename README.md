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

2 - Create Windows Service Code

3 - Deploy Windows Service Code

4 - Create and Deploy CA Certificates (IoT Edge and Windows Host)

5 - Create Leaf Devices under IoT Edge

6 - Create Service Instances

7 - Start and Stop Service Instances


