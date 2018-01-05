using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIEngineCharger: PartModule
    {
        const float kMinimumECToCharge = 1.0f;
        const string kStartEngine = "Start Engine";
        const string kToggleEngine = "Toggle Engine";
        const string kShutdownEngine = "Shutdown Engine";
        const string kStopCharging = "Stop Charging";
        const string kChargingCapacitor = "Charging capacitor before engine start...";
        const string kOutOfEC = "Stopping the reactor, can't get enough electricity to charge.";
        const string kEngineStarted = "Engine started.";

        [KSPField(guiActive = true, guiName = "Capacitor", isPersistant = true)]
        public string capacitorStatus;

        [KSPField(isPersistant = true)]
        public EReactorStates reactorState;

        [KSPField(isPersistant = true)]
        public double currentElectricCharge = 0f;

        [KSPField(isPersistant = true)]
        public bool reactorIsOn;

        [KSPField]
        public float ecNeededToStart = 0f;

        protected ModuleEngines engine;
        protected MultiModeEngine engineSwitcher;
        protected Dictionary<string, ModuleEngines> multiModeEngines = new Dictionary<string, ModuleEngines>();
        public float ecChargePerSec = 0f;

        public override string GetInfo()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(string.Format("<b>EC needed to start:</b> {0:f1}\n", ecNeededToStart));
            return builder.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            setupEngines();
        }

        [KSPEvent(guiActive = false, guiName = "Charge Capacitor")]
        public void ChargeCapacitor()
        {
            this.currentElectricCharge = this.ecNeededToStart;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (reactorState == EReactorStates.None || reactorState == EReactorStates.Off)
                return;

            requestCapacitorCharge();
        }

        #region API
        public void StartEngine()
        {
            reactorState = EReactorStates.Charged;
            currentElectricCharge = 0f;
            Events["ChargeCapacitor"].guiName = kShutdownEngine;

            getCurrentEngine();
            engine.Activate();
            engine.part.force_activate();
        }

        public void ShutdownEngine()
        {
            getCurrentEngine();
            engine.Events["Shutdown"].Invoke();
            engine.currentThrottle = 0;
            engine.requestedThrottle = 0;

            Events["ChargeCapacitor"].guiName = kStartEngine;

            if (reactorState != EReactorStates.Charging)
                currentElectricCharge = 0f;
            reactorState = EReactorStates.Off;
            if (currentElectricCharge == 0f)
                capacitorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart);
            else
                capacitorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart - currentElectricCharge);
        }
        #endregion

        #region Helpers
        public void requestCapacitorCharge()
        {
            if (reactorState == EReactorStates.Charging)
            {
                currentElectricCharge += this.part.RequestResource("ElectricCharge", ecChargePerSec * TimeWarp.fixedDeltaTime);

                //If we can't get the minimum EC required to charge the reactor, then shut off the reactor.
                //This way, the ship won't be starved for power.
                if (currentElectricCharge < kMinimumECToCharge)
                {
                    ScreenMessages.PostScreenMessage(kOutOfEC, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    ShutdownEngine();
                }

                if (currentElectricCharge < ecNeededToStart)
                {
                    capacitorStatus = EReactorStates.Charging.ToString() + String.Format(" {0:F2}%", (currentElectricCharge / ecNeededToStart) * 100);
                }

                //If we have enough charge then we can start the engine.
                StartEngine();
            }
        }

        protected ModuleEngines getCurrentEngine()
        {
            //If we have multiple engines, make sure we have the current one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }

            return engine;
        }

        protected void setupEngines()
        {
            //See if we have multiple engines that we need to support
            engineSwitcher = this.part.FindModuleImplementing<MultiModeEngine>();
            List<ModuleEngines> engines = this.part.FindModulesImplementing<ModuleEngines>();
            ModuleEngines moduleEngine = null;

            //Hide GUI on the switcher
            engineSwitcher.autoSwitch = false;
            engineSwitcher.Events["DisableAutoSwitch"].guiActive = false;
            engineSwitcher.Events["DisableAutoSwitch"].guiActiveEditor = false;
            engineSwitcher.Events["EnableAutoSwitch"].guiActive = false;
            engineSwitcher.Events["EnableAutoSwitch"].guiActiveEditor = false;

            //Find all the engines in the part and record their properties.
            for (int index = 0; index < engines.Count; index++)
            {
                moduleEngine = engines[index];
                multiModeEngines.Add(moduleEngine.engineID, moduleEngine);

                //Hide the GUI
                moduleEngine.Events["Activate"].guiActive = false;
                moduleEngine.Events["Shutdown"].guiActive = false;
                moduleEngine.Events["Activate"].guiActiveEditor = false;
                moduleEngine.Events["Shutdown"].guiActiveEditor = false;
            }

            //Get whichever multimode engine is the active one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }

            //Just get the first engine in the list.
            else if (engines.Count > 0)
            {
                engine = multiModeEngines.Values.ToArray()[0];
            }

            //Setup engine state
            if (reactorState == EReactorStates.Charged)
            {
                capacitorStatus = reactorState.ToString();
                Events["ChargeCapacitor"].guiName = kShutdownEngine;

                //Start your engine
                StartEngine();
            }

            else if (reactorState == EReactorStates.None)
            {
                currentElectricCharge = 0f;
                reactorState = EReactorStates.Off;
                capacitorStatus = EReactorStates.Off + string.Format(" Needs {0:F2} EC", ecNeededToStart);
            }
        }

        #endregion
    }
}
