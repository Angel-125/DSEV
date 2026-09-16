using System;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class ModuleFusionReactor : ModuleResourceConverter
    {
        [KSPField(isPersistant = true)]
        public float ecNeededToStart;

        [KSPField(isPersistant = true)]
        public bool reactorIsOn;

        [KSPField(guiActive = true, guiName = "Temperature")]
        public string reactorStatus;

        protected WBIModuleDSEVAnimated lightAnim;
        protected ModuleOverheatDisplay overheatDisplay;

        public override string GetInfo()
        {
            return base.GetInfo() + string.Format("\nRequires {0:F2}ec to start.", ecNeededToStart);
        }

        [KSPAction("Start Reactor", KSPActionGroup.Stage)]
        public void StartReactorStaged(KSPActionParam param)
        {
            if (reactorIsOn == false)
                ToggleReactor();
        }

        [KSPAction("Stop Reactor")]
        public void StopReactorAction(KSPActionParam param)
        {
            if (reactorIsOn)
                ToggleReactor();
        }

        [KSPAction("Toggle Reactor")]
        public void ToggleReactorAction(KSPActionParam param)
        {
            ToggleReactor();
        }

        [KSPEvent(guiName = "Toggle Reactor", guiActive = true)]
        public void ToggleReactor()
        {
            double ecObtained = 0f;

            if (reactorIsOn == false)
            {
                ecObtained = this.part.RequestResource("ElectricCharge", (double)ecNeededToStart);
                if (ecObtained / ecNeededToStart < 0.999)
                {
                    this.part.RequestResource("ElectricCharge", -ecObtained);
                    ScreenMessages.PostScreenMessage("Fully charge the reactor before starting.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }

                //We're good to go.
                reactorIsOn = true;
                this.Activate();
                ScreenMessages.PostScreenMessage("Reactor online.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                Events["ToggleReactor"].guiName = "Reactor Off";
            }

            //Shut off the reactor
            else
            {
                this.Shutdown();
                reactorIsOn = false;
                ScreenMessages.PostScreenMessage("Reactor offline.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                Events["ToggleReactor"].guiName = "Reactor On";
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            reactorStatus = String.Format("{0:#.##}K", this.part.temperature);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            overheatDisplay = this.part.FindModuleImplementing<ModuleOverheatDisplay>();
            lightAnim = this.part.FindModuleImplementing<WBIModuleDSEVAnimated>();
            if (lightAnim != null)
                lightAnim.showGui(false);

            Events["StartResourceConverter"].guiActive = false;
            Events["StartResourceConverter"].guiActiveEditor = false;
            Events["StopResourceConverter"].guiActive = false;
            Events["StopResourceConverter"].guiActiveEditor = false;
            Actions["StopResourceConverterAction"].active = false;
            Actions["StartResourceConverterAction"].active = false;

            if (reactorIsOn)
            {
                this.Activate();
                Events["ToggleReactor"].guiName = "Reactor Off";
            }
            else
            {
                Events["ToggleReactor"].guiName = "Reactor On";
            }
        }

        public void Activate()
        {
            StartResourceConverter();

            if (lightAnim != null && lightAnim.isDeployed == false)
                lightAnim.ToggleAnimation();
        }

        public void Shutdown()
        {
            StopResourceConverter();

            if (lightAnim != null && lightAnim.isDeployed)
                lightAnim.ToggleAnimation();
        }


    }
}
