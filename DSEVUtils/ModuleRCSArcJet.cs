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
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class ModuleRCSArcJet : ModuleRCSFX
    {
        private const double kDefaultECRequired = 150;

        [KSPField()]
        public double ecRequired = kDefaultECRequired;

        protected FixedUpdateHelper fixedUpdateHelper;

        PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
        PartResourceDefinition electricChargeDef;

        public override string GetInfo()
        {
            string baseInfo = base.GetInfo();

            baseInfo += "- <b>ElectricCharge: </b>" + ecRequired + "/sec. Max.";

            return baseInfo;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Create fixed update helper
            fixedUpdateHelper = this.part.gameObject.AddComponent<FixedUpdateHelper>();
            fixedUpdateHelper.onFixedUpdateDelegate = OnUpdateFixed;
            fixedUpdateHelper.enabled = true;

            //Get the resource definition for electric charge.
            electricChargeDef = definitions["ElectricCharge"];
        }

        public void OnUpdateFixed()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //If RCS is on and enabled, then consume electricity
            if (rcsEnabled && this.part.vessel.ActionGroups[KSPActionGroup.RCS])
            {
                double ecPerTimeTick = ecRequired * TimeWarp.fixedDeltaTime * thrusterPower;
                double ecObtained = this.part.RequestResource("ElectricCharge", ecPerTimeTick, ResourceFlowMode.ALL_VESSEL);

                if (ecObtained / ecPerTimeTick < 0.999)
                {
                    this.part.RequestResource("ElectricCharge", -ecObtained);
                    rcsEnabled = false;
                    return;
                }
            }
        }
    }
}
