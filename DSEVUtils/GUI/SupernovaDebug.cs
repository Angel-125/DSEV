﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using WBIResources;

namespace WildBlueIndustries
{
    public class SupernovaDebug : Dialog<SupernovaDebug>
    {
        public SupernovaController supernovaController;

        public SupernovaDebug() :
        base("Supernova Debug Menu", 300, 100)
        {
            Resizable = false;
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("Debug Reset"))
                supernovaController.DebugReset();

            if (GUILayout.Button("Charge Capacitor"))
                supernovaController.currentElectricCharge = supernovaController.ecNeededToStart;
            GUILayout.EndVertical();
        }
    }
}
