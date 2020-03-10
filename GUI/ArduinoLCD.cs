using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using OpenHardwareMonitor.Hardware;
using System.Diagnostics;

namespace OpenHardwareMonitor
{
    public static class ArduinoLCD
    {
        private static SerialPort currentPort;

        private static bool portFound;

        private static int AMDGPU;

        private static int sysFanCount;

        private static int deferUpdate;

        private static BackgroundWorker bgw;

        public static IHardware CPU { get; set; }

        public static IHardware ATIGPU { get; set; }

        public static IHardware NVIDIAGPU { get; set; }

        public static IHardware Motherboard{ get; set; }

        public static IHardware SuperIO { get; set; }

        public static void StartUpdates()
        {
            bgw = new BackgroundWorker();
            bgw.WorkerSupportsCancellation = true;
            bgw.DoWork += Bgw_DoWork;
            bgw.RunWorkerAsync();
        }

        private static void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!bgw.CancellationPending)
            {
                UpdateOccured();
                Thread.Sleep(1000);
            }
        }

        public static void UpdateOccured()
        {
            // Get the fan data
            byte[] cpuFans = new byte[2];
            byte[] sysFan1 = new byte[2];
            byte[] sysFan2 = new byte[2];
            byte[] sysFan3 = new byte[2];
            byte[] sysFan4 = new byte[2];
            if (SuperIO != null)
            {
                for (int i = 0; i < SuperIO.Sensors.Length; i++)
                {
                    if (SuperIO.Sensors[i].SensorType == SensorType.Fan && SuperIO.Sensors[i].Name == "Fan #1")
                    {
                        float cpuFanFloat = SuperIO.Sensors[i].Value ?? 0;
                        cpuFans = BitConverter.GetBytes(Convert.ToInt16(cpuFanFloat));
                    }
                    else if (SuperIO.Sensors[i].SensorType == SensorType.Fan && SuperIO.Sensors[i].Name == "Fan #2")
                    {
                        float sf1 = SuperIO.Sensors[i].Value ?? 0;
                        sysFan1 = BitConverter.GetBytes(Convert.ToInt16(sf1));
                        sysFanCount = 1;
                    }
                    else if (SuperIO.Sensors[i].SensorType == SensorType.Fan && SuperIO.Sensors[i].Name == "Fan #3")
                    {
                        float sf2 = SuperIO.Sensors[i].Value ?? 0;
                        sysFan2 = BitConverter.GetBytes(Convert.ToInt16(sf2));
                        sysFanCount = 2;
                    }
                    else if (SuperIO.Sensors[i].SensorType == SensorType.Fan && SuperIO.Sensors[i].Name == "Fan #4")
                    {
                        float sf3 = SuperIO.Sensors[i].Value ?? 0;
                        sysFan3 = BitConverter.GetBytes(Convert.ToInt16(sf3));
                        sysFanCount = 3;
                    }
                    else if (SuperIO.Sensors[i].SensorType == SensorType.Fan && SuperIO.Sensors[i].Name == "Fan #5")
                    {
                        float sf4 = SuperIO.Sensors[i].Value ?? 0;
                        sysFan4 = BitConverter.GetBytes(Convert.ToInt16(sf4));
                        sysFanCount = 4;
                    }
                }
            }

            // Get the CPU temps and load
            byte[] cpuData = new byte[4];
            if (CPU != null)
            {
                bool cpuTempFound = false;
                for (int i = 0; i < CPU.Sensors.Length; i++)
                {
                    if (CPU.Sensors[i].SensorType == SensorType.Temperature && CPU.Sensors[i].Name == "CPU Package")
                    {
                        float cpuTemp = CPU.Sensors[i].Value ?? 0;
                        cpuData[0] = Convert.ToByte(cpuTemp);

                        float cpuMax = CPU.Sensors[i].Max ?? 0;
                        cpuData[1] = Convert.ToByte(cpuMax);
                        cpuTempFound = true;
                    }
                    else if (CPU.Sensors[i].SensorType == SensorType.Load && CPU.Sensors[i].Name == "CPU Total")
                    {
                        float cpuLoad = CPU.Sensors[i].Value ?? 0;
                        cpuData[2] = Convert.ToByte(cpuLoad);
                    }
                    else if (CPU.Sensors[i].SensorType == SensorType.Power && CPU.Sensors[i].Name == "CPU Package")
                    {
                        float cpuPower = CPU.Sensors[i].Value ?? 0;
                        cpuData[3] = Convert.ToByte(cpuPower);
                    }
                    else if (CPU.Sensors[i].SensorType == SensorType.Temperature && CPU.Sensors[i].Name == "CPU Core #1" && !cpuTempFound)
                    {
                        float cpuTemp = CPU.Sensors[i].Value ?? 0;
                        cpuData[0] = Convert.ToByte(cpuTemp);

                        float cpuMax = CPU.Sensors[i].Max ?? 0;
                        cpuData[1] = Convert.ToByte(cpuMax);
                        cpuTempFound = true;
                    }
                }
            }

            // Get the GPU data
            byte[] gpuFan = new byte[2];
            byte[] gpuData = new byte[3];
            if (ATIGPU != null)
            {
                AMDGPU = 1;
                for (int i = 0; i < ATIGPU.Sensors.Length; i++)
                {
                    if (ATIGPU.Sensors[i].SensorType == SensorType.Fan)
                    {
                        float gpuFanFloat = ATIGPU.Sensors[i].Value ?? 0;
                        gpuFan = BitConverter.GetBytes(Convert.ToInt16(gpuFanFloat));
                    }
                    else if (ATIGPU.Sensors[i].SensorType == SensorType.Temperature && ATIGPU.Sensors[i].Name == "GPU Core")
                    {
                        float gpuTemp = ATIGPU.Sensors[i].Value ?? 0;
                        gpuData[0] = Convert.ToByte(gpuTemp);

                        float cpuMax = ATIGPU.Sensors[i].Max ?? 0;
                        gpuData[1] = Convert.ToByte(cpuMax);
                    }
                    else if (ATIGPU.Sensors[i].SensorType == SensorType.Load)
                    {
                        float gpuLoad = ATIGPU.Sensors[i].Value ?? 0;
                        gpuData[2] = Convert.ToByte(gpuLoad);
                    }
                }
            }
            else if (NVIDIAGPU != null)
            {
                AMDGPU = 0;
                for (int i = 0; i < NVIDIAGPU.Sensors.Length; i++)
                {
                    if (NVIDIAGPU.Sensors[i].SensorType == SensorType.Fan)
                    {
                        float gpuFanFloat = NVIDIAGPU.Sensors[i].Value ?? 0;
                        gpuFan = BitConverter.GetBytes(Convert.ToInt16(gpuFanFloat));
                    }
                    else if (NVIDIAGPU.Sensors[i].SensorType == SensorType.Temperature && NVIDIAGPU.Sensors[i].Name == "GPU Core")
                    {
                        float gpuTemp = NVIDIAGPU.Sensors[i].Value ?? 0;
                        gpuData[0] = Convert.ToByte(gpuTemp);

                        float cpuMax = NVIDIAGPU.Sensors[i].Max ?? 0;
                        gpuData[1] = Convert.ToByte(cpuMax);
                    }
                    else if (NVIDIAGPU.Sensors[i].SensorType == SensorType.Load)
                    {
                        float gpuLoad = NVIDIAGPU.Sensors[i].Value ?? 0;
                        gpuData[2] = Convert.ToByte(gpuLoad);
                    }
                }
            }

            if (deferUpdate == 1)
            {
                // Has the Arduino been connected yet?
                if (portFound)
                {
                    string retVal = string.Empty;

                    //The below setting are for the Hello handshake
                    byte[] buffer = new byte[25];
                    buffer[0] = Convert.ToByte(127);
                    buffer[1] = cpuFans[0];
                    buffer[2] = cpuFans[1];
                    buffer[3] = cpuData[0];
                    buffer[4] = cpuData[1];
                    buffer[5] = cpuData[2];
                    buffer[6] = cpuData[3];
                    buffer[7] = gpuFan[0];
                    buffer[8] = gpuFan[1];
                    buffer[9] = gpuData[0];
                    buffer[10] = gpuData[1];
                    buffer[11] = gpuData[2];
                    buffer[12] = Convert.ToByte(DateTime.Now.Hour);
                    buffer[13] = Convert.ToByte(DateTime.Now.Minute);
                    buffer[14] = Convert.ToByte(DateTime.Now.Second);
                    buffer[15] = Convert.ToByte(sysFanCount);
                    buffer[16] = Convert.ToByte(AMDGPU);
                    buffer[17] = sysFan1[0];
                    buffer[18] = sysFan1[1];
                    buffer[19] = sysFan2[0];
                    buffer[20] = sysFan2[1];
                    buffer[21] = sysFan3[0];
                    buffer[22] = sysFan3[1];
                    buffer[23] = sysFan4[0];
                    buffer[24] = sysFan4[1];
                    int intReturnASCII = 0;
                    char charReturnValue = (Char)intReturnASCII;
                    currentPort.Write(buffer, 0, 25);
                }
                else
                {
                    SetComPort();
                }
            }
            else
            {
                deferUpdate++;
            }
        }

        /// <summary>
        /// Set the arduino com port by scan and detection
        /// </summary>
        public static bool SetComPort()
        {
            bool retVal = false;
            try
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    currentPort = new SerialPort(port, 115200);
                    if (DetectArduino())
                    {
                        portFound = retVal = true;                        
                        break;
                    }
                    else
                    {
                        portFound = retVal = false;
                    }
                }
            }
            catch (Exception e)
            {
            }

            return retVal;
        }

        private static bool DetectArduino()
        {
            try
            {

                //The below setting are for the Hello handshake
                byte[] buffer = new byte[4];
                buffer[0] = Convert.ToByte(128);
                buffer[1] = Convert.ToByte(11);
                buffer[2] = Convert.ToByte(sysFanCount);
                buffer[3] = Convert.ToByte(AMDGPU);

                int intReturnASCII = 0;
                char charReturnValue = (Char)intReturnASCII;
                currentPort.Open();
                currentPort.Write(buffer, 0, 4);
                Thread.Sleep(500);
                int count = currentPort.BytesToRead;
                string returnMessage = "";
                while (count > 0)
                {
                    intReturnASCII = currentPort.ReadByte();
                    returnMessage = returnMessage + Convert.ToChar(intReturnASCII);
                    count--;
                }
                if (returnMessage.Contains("HELLO FROM ARDUINO"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static void Disconnect()
        {
            bgw.CancelAsync();
            if (portFound)
            {
                byte[] buffer = new byte[4];
                buffer[0] = Convert.ToByte(129);
                buffer[1] = Convert.ToByte(0);
                buffer[2] = Convert.ToByte(0);
                buffer[3] = Convert.ToByte(0);
                currentPort.Write(buffer, 0, 4);
                currentPort.Close();
            }
        }
    }
}
