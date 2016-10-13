using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum EReactorStates
    {
        None,
        Off,
        Charging,
        Ready,
        Idling,
        Running,
        Overheated
    }

    public class SupernovaController : ExtendedPartModule
    {
        const float kIdleTargetTemp = 300;
        const float kMinimumECToCharge = 1.0f;
        const string kStartEngine = "Start Engine";
        const string kToggleEngine = "Toggle Engine";
        const string kShutdownEngine = "Shutdown Engine";
        const string kStopCharging = "Stop Charging";
        const string kOverheatWarning = "Engine shutdown! Heat is beyond capacity!";
        const string kOutOfFuel = "Engines throttled down, out of reactor fuel!";
        const string kOutOfEC = "Stopping the reactor, can't get enough electricity to charge.";
        const string kChargingCapacitor = "Charging capacitor before engine start...";
        const string kOverheated = "Overheated";
        const string kEngineStarted = "Engine started.";
        const string kPulsedPlasmaEditorNote = "You won't be able to access Pulsed Plasma Mode in flight until you upgrade the engine.";
        const string insufficientResourcesMsg = "Insufficient resources to upgrade the engine.";
        const string kInsufficientScience = "Insufficient Science to upgrade the engine.";
        const string kEngineerNeeded = "You need a highly experienced engineer in order to upgrade the engine.";
        const string kEngineUpgraded = "Engine upgraded.";

        protected MultiModeEngine multiModeEngine;
        protected ModuleEnginesFXWBI primaryEngine;
        protected ModuleEnginesFXWBI secondaryEngine;

        [KSPField(guiActive = true, guiName = "Reactor", isPersistant = true)]
        public string reactorStatus;

        [KSPField(isPersistant = true)]
        public EReactorStates reactorState;

        [KSPField(isPersistant = true)]
        public double currentElectricCharge = 0f;

        [KSPField(isPersistant = true)]
        public float fuelRequest;

        [KSPField(isPersistant = true)]
        public bool reactorIsOn;

        [KSPField(guiActive = true, guiName = "Engine Temperature")]
        string engineTemperature;

        [KSPField(isPersistant = true)]
        public bool engineUpgraded;

        [KSPField(isPersistant = true)]
        public string upgradeResources;

        [KSPField]
        public bool requiresECToStart = true;

        [KSPField]
        public float fuelConsumption;

        [KSPField]
        public string reactorFuel = "FusionPellets";

        [KSPField]
        public float ecNeededToStart = 0f;

        [KSPField]
        public string upgradeSkill = "RepairSkill";

        [KSPField]
        public int upgradeExperienceLevel = 5;

        public string primaryEngineID;
        public float ecChargePerSec = 0f;
        public bool showDebugButton = true;
        public float primaryEngineHeat = 0f;
        public float secondaryEngineHeat = 0;
        protected bool wasRunningPrimary;

        #region Events And Actions
        [KSPEvent(guiActive = true, guiActiveEditor =  false, guiName = "Upgrade Engine", active = true, externalToEVAOnly = true, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public void UpgradeEngine()
        {
            string[] resourcesRequired = upgradeResources.Split(new char[] { ';' });
            string[] resourceInfo;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            double resourceAmount;

            //Make sure we have an experienced engineer.
            if (FlightGlobals.ActiveVessel.isEVA && Utils.IsExperienceEnabled())
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                ProtoCrewMember astronaut = vessel.GetVesselCrew()[0];

                if (astronaut.HasEffect(upgradeSkill) == false || astronaut.experienceTrait.CrewMemberExperienceLevel() < upgradeExperienceLevel)
                {
                    ScreenMessages.PostScreenMessage(kEngineerNeeded, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            } 
            
            //Ok, we have an experienced engineer. Now does the vessel have sufficient resources?
            foreach (string resourceRequired in resourcesRequired)
            {
                //Get the resource info
                resourceInfo = resourceRequired.Split(new char[] { ',' });
                resourceAmount = double.Parse(resourceInfo[1]);

                //Is this a science "resource" ?
                if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX)
                {
                    if (resourceInfo[0] == "Science" && ResearchAndDevelopment.Instance != null)
                    {
                        if (ResearchAndDevelopment.CanAfford((float)resourceAmount) == false)
                        {
                            ScreenMessages.PostScreenMessage(kInsufficientScience, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            return;
                        }

                        //Pay the science cost.
                        ResearchAndDevelopment.Instance.CheatAddScience(-(float)resourceAmount);
                        continue;
                    }
                }

                //Skip this sciency stuff since we're in Sandbox.
                else if (resourceInfo[0] == "Science")
                {
                    continue;
                }

                //Find definition
                resourceDef = definitions[resourceInfo[0]];
                if (resourceDef == null)
                {
                    ScreenMessages.PostScreenMessage(insufficientResourcesMsg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }

                //See if the vessel has the required amount of resources
                double totalResources = ResourceHelper.GetTotalResourceAmount(resourceInfo[0], this.part.vessel);
                if ((totalResources / resourceAmount) < 1.0f)
                {
                    ScreenMessages.PostScreenMessage(insufficientResourcesMsg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }

                //Take the resource
                double resourceObtained = this.part.RequestResource(resourceDef.id, resourceAmount, ResourceFlowMode.ALL_VESSEL);
            }

            //If we got this far then I guess we should just go ahead and upgrade the engine *sigh...* ;)
            multiModeEngine.Events["ModeEvent"].guiActive = true;
            this.Events["UpgradeEngine"].guiActiveUnfocused = false;
            this.Events["UpgradeEngine"].guiActive = false;
            engineUpgraded = true;
            ScreenMessages.PostScreenMessage(kEngineUpgraded, 5.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Toggle Mode", active = true)]
        public void ToggleModeEditor()
        {
            //Toggle the mode.
            multiModeEngine.Events["ModeEvent"].Invoke();

            //In the editor, remind the player that the engine must be upgraded in flight before pulsed plasma mode can be used.
            if (multiModeEngine.mode == multiModeEngine.secondaryEngineID)
                ScreenMessages.PostScreenMessage(kPulsedPlasmaEditorNote, 5.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = false, guiName = "Charge Capacitor")]
        public void ChargeCapacitor()
        {
            this.currentElectricCharge = this.ecNeededToStart;
        }

        [KSPEvent(guiActive = false, guiName = "Debug Reset")]
        public void DebugReset()
        {
            ShutdownReactorAndEngine();
            reactorIsOn = false;
            currentElectricCharge = 0f;
            reactorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart);
        }

        [KSPEvent(guiActive = true, guiName = "Start Engine", active = true)]
        public void ToggleReactor()
        {
            //If we got this far then it means we have the potential to start.
            //First check to see if we have enough electric charge.
            if (reactorState == EReactorStates.Off)
            {
                //If we need ec to start, then set our state to charging
                if ((currentElectricCharge < ecNeededToStart) && requiresECToStart)
                {
                    reactorState = EReactorStates.Charging;
                    reactorIsOn = false;
                    Events["ToggleReactor"].guiName = kStopCharging;
                    ScreenMessages.PostScreenMessage(kChargingCapacitor, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                //Capacitor is fully charged, activate the engine and star idling the reactor
                else
                {
                    reactorState = EReactorStates.Idling;
                    reactorIsOn = true;
                    Events["ToggleReactor"].guiName = kShutdownEngine;

                    //Start your engine
                    StartEngine();
                }

                //Ok, we're done
                return;
            }

            else if (reactorState == EReactorStates.Charging)
            {
                reactorState = EReactorStates.Off;
                reactorStatus = EReactorStates.Off + string.Format(" Needs {0:#.#} EC", ecNeededToStart - currentElectricCharge);
                reactorIsOn = false;
            }

            else if (reactorState == EReactorStates.Running || reactorState == EReactorStates.Idling)
            {
                ShutdownReactorAndEngine();
                reactorIsOn = false;
                reactorState = EReactorStates.Off;
                currentElectricCharge = 0f;
                reactorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart);
            }
        }

        #endregion

        #region Overrides
        public override string GetInfo()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(string.Format("<b>EC needed to start:</b> {0:f1}\n", ecNeededToStart));
            builder.Append("<b>" + reactorFuel + " consumed (running): </b>");
            builder.Append(string.Format("{0:f3}/sec\n", fuelConsumption));
            builder.Append("<b>" + reactorFuel + " consumed (idle): </b>");
            builder.Append(string.Format("{0:f3}/sec\n", fuelConsumption / 10.0f));
            if (string.IsNullOrEmpty(upgradeResources) == false)
                builder.Append(kPulsedPlasmaEditorNote + "\n<b>Required to upgrade:</b> " + upgradeResources);
            else
                builder.Append(kPulsedPlasmaEditorNote);

            return builder.ToString();
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);
            string value;

            value = protoNode.GetValue("fuelConsumption");
            if (string.IsNullOrEmpty(value) == false)
                fuelConsumption = float.Parse(value);

            value = protoNode.GetValue("ecNeededToStart");
            if (string.IsNullOrEmpty(value) == false)
                ecNeededToStart = float.Parse(value);

            value = protoNode.GetValue("ecChargePerSec");
            if (string.IsNullOrEmpty(value) == false)
                ecChargePerSec = float.Parse(value);

            value = protoNode.GetValue("showDebugButtons");
            if (string.IsNullOrEmpty(value) == false)
                showDebugButton = bool.Parse(value);

            value = protoNode.GetValue("primaryEngineHeat");
            if (string.IsNullOrEmpty(value) == false)
                primaryEngineHeat = float.Parse(value);

            value = protoNode.GetValue("secondaryEngineHeat");
            if (string.IsNullOrEmpty(value) == false)
                secondaryEngineHeat = float.Parse(value);

            primaryEngineID = protoNode.GetValue("primaryEngineID");

            reactorFuel = protoNode.GetValue("reactorFuel");
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Hide gui elements that we don't need.
            HideGUI();

            //Setup the engine
            SetEngineStateOnStart();

            Events["DebugReset"].guiActive = showDebugButton;
            Events["ChargeCapacitor"].guiActive = showDebugButton;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //We don't have access to the Update or FixedUpdate or OnUpdate or OnFixedUpdate methods from
            //the engine module. We need to lend a hand.
            if (multiModeEngine.runningPrimary)
            {
                if (!wasRunningPrimary)
                {
                    secondaryEngine.HideParticleEffects();
                    primaryEngine.ShowParticleEffects();
                    wasRunningPrimary = multiModeEngine.runningPrimary;
                }
                primaryEngine.UpdateEngineState();
            }
            else
            {
                if (wasRunningPrimary)
                {
                    secondaryEngine.ShowParticleEffects();
                    primaryEngine.HideParticleEffects();
                    wasRunningPrimary = multiModeEngine.runningPrimary;
                }
                secondaryEngine.UpdateEngineState();
            }

            //Set engine temperature
            engineTemperature = String.Format("{0:#.##}K", this.part.temperature);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            //The logic below doesn't apply unless we're flying
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (reactorState == EReactorStates.None || reactorState == EReactorStates.Off)
                return;

            //If we are charging up, request electric charge and then exit.
            if (ReactorNeedsCharge())
                return;

            //Set status
            if (primaryEngine.currentThrottle > 0f || secondaryEngine.currentThrottle > 0f)
                reactorState = EReactorStates.Running;
            else
                reactorState = EReactorStates.Idling;
            reactorStatus = reactorState.ToString();

            //Consume a small amount of fusion pellets to represent the fusion reactor's operation in NTR mode.
            ConsumeFuel();
        }


        #endregion

        #region Helpers

        public void SetEngineStateOnStart()
        {
            if (reactorIsOn)
            {
                reactorState = EReactorStates.Idling;
                reactorStatus = reactorState.ToString();
                Events["ToggleReactor"].guiName = kShutdownEngine;

                //Start your engine
                StartEngine();
            }

            else if (reactorState == EReactorStates.None)
            {
                currentElectricCharge = 0f;
                reactorState = EReactorStates.Off;
                reactorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart);
            }
        }

        public void HideGUI()
        {
            List<ModuleEnginesFXWBI> engineList = this.part.FindModulesImplementing<ModuleEnginesFXWBI>();

            //Hide multimode switcher gui
            multiModeEngine = this.part.FindModuleImplementing<MultiModeEngine>();
            multiModeEngine.autoSwitch = false;
            multiModeEngine.Events["DisableAutoSwitch"].guiActive = false;
            multiModeEngine.Events["DisableAutoSwitch"].guiActiveEditor = false;
            multiModeEngine.Events["EnableAutoSwitch"].guiActive = false;
            multiModeEngine.Events["EnableAutoSwitch"].guiActiveEditor = false;
            multiModeEngine.Events["ModeEvent"].guiActiveEditor = false;

            //Allow mode switching if the engine has been upgraded, or show the upgrade button.
            if (engineUpgraded == false)
            {
                multiModeEngine.Events["ModeEvent"].guiActive = false;

                //Make sure we're not in pulsed plasma mode
                if (multiModeEngine.runningPrimary == false)
                    multiModeEngine.Events["ModeEvent"].Invoke();
            }
            else
            {
                this.Events["UpgradEngine"].guiActiveUnfocused = false;
            }

            //Hide engine gui
            foreach (ModuleEnginesFXWBI engine in engineList)
            {
                if (engine.engineID == primaryEngineID)
                    primaryEngine = engine;
                else
                    secondaryEngine = engine;

                engine.onActiveDelegate = OnEngineActive;
                engine.Events["Activate"].guiActive = false;
                engine.Events["Shutdown"].guiActive = false;
                engine.Events["Activate"].guiActiveEditor = false;
                engine.Events["Shutdown"].guiActiveEditor = false;
            }
        }

        public bool ReactorNeedsCharge()
        {
            if (reactorState == EReactorStates.Charging)
            {
                currentElectricCharge += this.part.RequestResource("ElectricCharge", ecChargePerSec * TimeWarp.fixedDeltaTime);

                //If we can't get the minimum EC required to charge the reactor, then shut off the reactor.
                //This way, the ship won't be starved for power.
                if (currentElectricCharge < kMinimumECToCharge)
                {
                    ScreenMessages.PostScreenMessage(kOutOfEC, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    ShutdownReactorAndEngine();
                    return true;
                }

                if (currentElectricCharge < ecNeededToStart)
                {
                    reactorStatus = EReactorStates.Charging.ToString() + String.Format(" {0:F2}%", (currentElectricCharge / ecNeededToStart) * 100);
                    return true;
                }

                //If we have enough charge then we can start the engine.
                StartEngine();
            }

            return false;
        }

        public void ShutdownReactorAndEngine()
        {
            primaryEngine.Events["Shutdown"].Invoke();
            primaryEngine.currentThrottle = 0;
            primaryEngine.requestedThrottle = 0;

            secondaryEngine.Events["Shutdown"].Invoke();
            secondaryEngine.currentThrottle = 0;
            secondaryEngine.requestedThrottle = 0;

            Events["ToggleReactor"].guiName = kStartEngine;

            if (reactorState != EReactorStates.Charging)
                currentElectricCharge = 0f;
            reactorState = EReactorStates.Off;
            if (currentElectricCharge == 0f)
                reactorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart);
            else
                reactorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart - currentElectricCharge);
            reactorIsOn = false;
        }

        public void StartEngine()
        {
            reactorIsOn = true;
            currentElectricCharge = 0f;
            Events["ToggleReactor"].guiName = kShutdownEngine;
            if (multiModeEngine.runningPrimary)
            {
                wasRunningPrimary = true;
                primaryEngine.Activate();
                primaryEngine.part.force_activate();
                if (primaryEngine.currentThrottle > 0)
                    primaryEngine.ShowParticleEffects(true);
            }
            else
            {
                wasRunningPrimary = false;
                secondaryEngine.Activate();
                secondaryEngine.part.force_activate();
                secondaryEngine.staged = true;
                if (secondaryEngine.currentThrottle > 0)
                    secondaryEngine.ShowParticleEffects(true);
            }
        }

        public void ConsumeFuel()
        {
            float fuelPerTimeTick = fuelConsumption * TimeWarp.fixedDeltaTime;

            //Adjust fuel consuption for idling
            if (primaryEngine.thrustPercentage == 0f && secondaryEngine.thrustPercentage == 0f)
                fuelPerTimeTick = fuelPerTimeTick / 10.0f;

            if (multiModeEngine.runningPrimary == true)
            {
                //Make sure we reach the minimum threshold
                fuelRequest += fuelPerTimeTick;
                if (fuelRequest >= 0.01f)
                {
                    //Consume fusion pellets
                    float fuelObtained = this.part.vessel.rootPart.RequestResource(reactorFuel, fuelRequest);

                    //If we haven't consumed enough pellets then the reactor cannot be sustained
                    //and the engine flames out.
                    if (fuelObtained < fuelRequest)
                    {
                        ScreenMessages.PostScreenMessage(kOutOfFuel, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        primaryEngine.Events["Shutdown"].Invoke();
                        primaryEngine.currentThrottle = 0;
                        primaryEngine.requestedThrottle = 0;
                        primaryEngine.flameout = true;
                        reactorIsOn = false;
                        Events["ToggleReactor"].guiName = kStartEngine;
                        reactorState = EReactorStates.Off;
                        reactorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart);
                    }

                    //Reset the fuel request
                    fuelRequest = 0f;
                }
            }
        }

        public void OnEngineActive(string engineID, bool isRunning)
        {
            if (reactorIsOn == false && isRunning)
            {
                if (currentElectricCharge >= ecNeededToStart)
                {
                    reactorState = EReactorStates.Off;
                    ToggleReactor();
                    return;
                }

                primaryEngine.Events["Shutdown"].Invoke();
                primaryEngine.currentThrottle = 0;
                primaryEngine.requestedThrottle = 0;

                secondaryEngine.Events["Shutdown"].Invoke();
                secondaryEngine.currentThrottle = 0;
                secondaryEngine.requestedThrottle = 0;

                //If the reactor is off, then then start charging the capacitor.
                if (reactorState != EReactorStates.Charging)
                {
                    ScreenMessages.PostScreenMessage(kChargingCapacitor, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    Events["ToggleReactor"].guiName = kStartEngine;
                    ToggleReactor();
                }
            }
        }
        #endregion
    }
}
