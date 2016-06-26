using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class ModuleArcJetEngine : WBIMeshHelper
    {
        const string kOutOfFuel = "Engine throttled down, out of electricity!";

        //Amount of ec per second required to run the radiator, if any.
        [KSPField(isPersistant = true)]
        public double ecRequired;

        [KSPField(isPersistant = true)]
        bool showBrackets;

        PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
        PartResourceDefinition electricChargeDef;
        ModuleEnginesFX engine;

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle Brackets", active = true)]
        public void ToggleBrackets()
        {
            ModuleArcJetEngine arcJetEngine;

            showBrackets = !showBrackets;

            setVisibleObjects();

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    arcJetEngine = symmetryPart.GetComponent<ModuleArcJetEngine>();
                    arcJetEngine.ShowBrackets(showBrackets);
                }
            }
        }

        public void ShowBrackets(bool bracketsAreVisible)
        {
            showBrackets = bracketsAreVisible;

            setVisibleObjects();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get the resource definition for electric charge.
            electricChargeDef = definitions["ElectricCharge"];

            //Get the engine module
            engine = this.part.FindModuleImplementing<ModuleEnginesFX>();

            //Hide/show the brackets
            setVisibleObjects();
        }

        public override string GetInfo()
        {
            string info = base.GetInfo();

            info += string.Format("\n<b>-Electric Charge Required (max):</b> {0:f1}EC", ecRequired);

            return info;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (engine.currentThrottle <= 0.001f)
                return;
            if (ecRequired < 0.001)
                return;

            //Do we have enough electricity to run the engine?
            double ecPerTimeTick = ecRequired * TimeWarp.fixedDeltaTime * engine.currentThrottle;
            double ecObtained = this.part.RequestResource("ElectricCharge", ecPerTimeTick, ResourceFlowMode.ALL_VESSEL);

            if (ecObtained / ecPerTimeTick < 0.999)
            {
                engine.currentThrottle = 0;
                engine.requestedThrottle = 0;
                engine.flameout = true;
                engine.Shutdown();
                this.part.RequestResource("ElectricCharge", -ecObtained);
                ScreenMessages.PostScreenMessage(kOutOfFuel, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
        }

        protected virtual void setVisibleObjects()
        {
            List<int> visibleObjects = new List<int>();

            if (showBrackets)
                visibleObjects.Add(0);

            setObjects(visibleObjects);
        }
    }
}
