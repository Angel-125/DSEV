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
    public class ModuleMasscon : ExtendedPartModule, IPartMassModifier
    {
        [KSPField(guiActiveEditor = true, guiName = "Mass", guiFormat = "f3", isPersistant = true)]
        public float partMass = 0f;

        private MassconWindow massconView = new MassconWindow();

        [KSPEvent(guiActiveEditor = true)]
        public void EditMass()
        {
            massconView.SetVisible(true);
        }

        public void OnUpdateMass(float mass)
        {
            ModuleMasscon massconModule;
            UpdateMass(mass);

            foreach (Part symmetryPart in this.part.symmetryCounterparts)
            {
                massconModule = symmetryPart.GetComponent<ModuleMasscon>();
                massconModule.UpdateMass(mass);
            }
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (partMass > 0.001f)
                return partMass;
            else
                return defaultMass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return ModifierChangeWhen.CONSTANTLY;
            else
                return ModifierChangeWhen.FIXED;
        }

        public void UpdateMass(float mass)
        {
            partMass = mass;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            massconView.massUpdateDelegate = OnUpdateMass;

            if (partMass > 0f)
                this.part.mass = partMass;
            else
                partMass = this.part.mass;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (partMass > 0f)
                this.part.mass = partMass;
        }

        public override string GetInfo()
        {
            return "Use the action menu to add or remove Masscon from the module.";
        }
    }
}
