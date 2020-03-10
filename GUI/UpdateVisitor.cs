/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2010 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.GUI
{
    public class UpdateVisitor : IVisitor
    {
        private bool arduinoStarted = false;

        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            if (!arduinoStarted)
            {
                ArduinoLCD.StartUpdates();
                arduinoStarted = true;
            }
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }

            switch (hardware.HardwareType)
            {
                case HardwareType.CPU:
                    ArduinoLCD.CPU = hardware;
                    break;
                case HardwareType.GpuAti:
                    ArduinoLCD.ATIGPU = hardware;
                    break;
                case HardwareType.GpuNvidia:
                    ArduinoLCD.NVIDIAGPU = hardware;
                    break;
                case HardwareType.Mainboard:
                    ArduinoLCD.Motherboard = hardware;
                    break;
                case HardwareType.SuperIO:
                    ArduinoLCD.SuperIO = hardware;
                    break;
            }

        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
