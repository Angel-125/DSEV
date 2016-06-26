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

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum RackConfigurations
    {
        Unknown,
        None,
        Two,
        Three,
        Six
    }

    public class ModuleHexTruss : WBIMeshHelper
    {
        private const RackConfigurations kDefaultRacks = RackConfigurations.Two;
        private const int kDefaultEndCapIndex = 0;
        private const string kTwoRacks = "Two Racks";
        private const string kThreeRacks = "Three Racks";
        private const string kSixRacks = "Six Racks";
        private const string kNoRacks = "No Racks";

        [KSPField(isPersistant = true)]
        public int endCapIndex = -1;

        [KSPField(isPersistant = true)]
        public int bodyIndex = 0;

        [KSPField(isPersistant = true)]
        public RackConfigurations rackConfiguration = RackConfigurations.Unknown;

        [KSPField(isPersistant = true)]
        public bool centerVestibuleIsVisible = false;

        [KSPField]
        public string fuelTankTransform;

        string[] endCapNames = null;
        string[] rackNames = null;
        string[] bodyNames = null;
        string centerVestibuleName = null;
        WBIResourceSwitcher switcher;

        #region GUI
        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle center vestibule")]
        public void ToggleCenterVestibule()
        {
            if (string.IsNullOrEmpty(centerVestibuleName))
                return;

            centerVestibuleIsVisible = !centerVestibuleIsVisible;

            SetVisibleObjects();
            UpdateSymmetryParts();
        }

        [KSPEvent(guiActiveEditor = true)]
        public void NextEndCap()
        {
            int nextIndex = -1;

            endCapIndex = (endCapIndex + 1) % endCapNames.Length;

            SetVisibleObjects();

            nextIndex = (endCapIndex + 1) % endCapNames.Length;
            Events["NextEndCap"].guiName = endCapNames[nextIndex];

            UpdateSymmetryParts();
        }

        [KSPEvent(guiActiveEditor = true)]
        public void NextBodyType()
        {
            int nextIndex = -1;

            bodyIndex = (bodyIndex + 1) % bodyNames.Length;

            //If there is anything attached to the inner nodes then don't allow a body type switch
            //if the next body type is a fuel tank.

            SetVisibleObjects();

            nextIndex = (bodyIndex + 1) % bodyNames.Length;
            Events["NextBodyType"].guiName = bodyNames[nextIndex];

            UpdateResources();
            UpdateSymmetryParts();
        }

        [KSPEvent(guiActiveEditor = true)]
        public void NextRackConfiguration()
        {
            switch (rackConfiguration)
            {
                case RackConfigurations.None:
                    rackConfiguration = RackConfigurations.Two;
                    Events["NextRackConfiguration"].guiName = kThreeRacks;
                    break;

                case RackConfigurations.Two:
                    rackConfiguration = RackConfigurations.Three;
                    Events["NextRackConfiguration"].guiName = kSixRacks;
                    break;

                case RackConfigurations.Three:
                    rackConfiguration = RackConfigurations.Six;
                    Events["NextRackConfiguration"].guiName = kNoRacks;
                    break;

                case RackConfigurations.Six:
                default:
                    rackConfiguration = RackConfigurations.None;
                    Events["NextRackConfiguration"].guiName = kTwoRacks;
                    break;
            }

            SetVisibleObjects();
            UpdateSymmetryParts();
        }

        #endregion

        #region overrides
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("rackConfiguration"))
                rackConfiguration = (RackConfigurations)Enum.Parse(typeof(RackConfigurations), node.GetValue("rackConfiguration"));
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("rackConfiguration", rackConfiguration.ToString());
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);
            string value = protoNode.GetValue("endCapNames");

            if (string.IsNullOrEmpty(value) == false)
                endCapNames = value.Split(';');
            else
                Events["NextEndCap"].active = false;

            value = protoNode.GetValue("rackNames");
            if (string.IsNullOrEmpty(value) == false)
                rackNames = value.Split(';');

            if (protoNode.HasValue("bodyNames"))
                bodyNames = protoNode.GetValue("bodyNames").Split(';');

            if (protoNode.HasValue("centerVestibuleName"))
                centerVestibuleName = protoNode.GetValue("centerVestibuleName");
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            int nextIndex = -1;

            //Find the switch if any.
            switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();

            //Hide base GUI
            Events["NextMesh"].guiActive = false;
            Events["NextMesh"].active = false;
            Events["PrevMesh"].active = false;
            Events["PrevMesh"].guiActive = false;

            //Set initial values
            Log("rack configuration: " + rackConfiguration);
            if (rackConfiguration == RackConfigurations.Unknown)
                rackConfiguration = kDefaultRacks;
            Log("rack configuration: " + rackConfiguration);

            if (endCapIndex == -1)
                endCapIndex = kDefaultEndCapIndex;

            //Set the visible objects
            SetVisibleObjects();
            if (HighLogic.LoadedSceneIsEditor)
                UpdateResources();

            //Setup the GUI
            if (endCapNames != null)
            {
                nextIndex = (endCapIndex + 1) % endCapNames.Length;
                Events["NextEndCap"].guiName = endCapNames[nextIndex];
            }

            if (bodyNames != null)
            {
                nextIndex = (bodyIndex + 1) % bodyNames.Length;
                Events["NextBodyType"].guiName = bodyNames[nextIndex];
            }

            if (string.IsNullOrEmpty(centerVestibuleName))
            {
                Events["ToggleCenterVestibule"].guiActiveEditor = false;
                Events["ToggleCenterVestibule"].guiActive = false;
                Events["ToggleCenterVestibule"].active = false;
            }
            else
            {
                Events["ToggleCenterVestibule"].guiActiveEditor = true;
                Events["ToggleCenterVestibule"].guiActive = false;
                Events["ToggleCenterVestibule"].active = true;
            }

            switch (rackConfiguration)
            {
                case RackConfigurations.None:
                    Events["NextRackConfiguration"].guiName = kTwoRacks;
                    break;

                case RackConfigurations.Two:
                    Events["NextRackConfiguration"].guiName = kThreeRacks;
                    break;

                case RackConfigurations.Three:
                    Events["NextRackConfiguration"].guiName = kSixRacks;
                    break;

                case RackConfigurations.Six:
                default:
                    Events["NextRackConfiguration"].guiName = kNoRacks;
                    break;
            }

        }
        #endregion

        #region Helpers
        public virtual void UpdateSymmetryParts()
        {
            ModuleHexTruss hexTruss;

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    hexTruss = symmetryPart.GetComponent<ModuleHexTruss>();

                    if (string.IsNullOrEmpty(centerVestibuleName) == false)
                        hexTruss.centerVestibuleIsVisible = this.centerVestibuleIsVisible;
                    hexTruss.endCapIndex = this.endCapIndex;
                    hexTruss.rackConfiguration = this.rackConfiguration;

                    hexTruss.SetVisibleObjects();
                    hexTruss.UpdateResources();
                }
            }
        }

        public void UpdateResources()
        {
            if (string.IsNullOrEmpty(fuelTankTransform) == false && switcher != null)
            {
                if (fuelTankTransform.Contains(bodyNames[bodyIndex]))
                    switcher.ReloadTemplate();
                else
                    switcher.RemoveAllResources();
            }
        }

        public virtual void SetVisibleObjects()
        {
            List<int> visibleObjects = new List<int>();
            string visibleObject = null;

            //Fuel tank GUI
            if (string.IsNullOrEmpty(fuelTankTransform) == false && switcher != null)
            {
                if (fuelTankTransform.Contains(bodyNames[bodyIndex]))
                {
                    switcher.SetGUIVisible(true);
                }
                else
                {
                    switcher.SetGUIVisible(false);
                    switcher.isEnabled = false;
                    switcher.enabled = false;
                }
            }

            //Set visible end caps
            if (endCapNames != null)
            {
                visibleObject = endCapNames[endCapIndex];
                visibleObjects.Add(meshIndexes[visibleObject]);
            }

            //Body type
            if (bodyNames != null)
            {
                visibleObject = bodyNames[bodyIndex];
                if (meshIndexes.ContainsKey(visibleObject))
                    visibleObjects.Add(meshIndexes[visibleObject]);
            }

            //Center vestibule
            if (!string.IsNullOrEmpty(centerVestibuleName))
            {
                if (centerVestibuleIsVisible)
                    visibleObjects.Add(meshIndexes[centerVestibuleName]);
            }

            //Set racks
            switch (rackConfiguration)
            {
                case RackConfigurations.Two:
                    visibleObject = rackNames[0];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[3];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    break;

                case RackConfigurations.Three:
                    visibleObject = rackNames[0];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[2];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[4];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    break;

                case RackConfigurations.Six:
                    visibleObject = rackNames[0];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[1];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[2];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[3];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[4];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    visibleObject = rackNames[5];
                    visibleObjects.Add(meshIndexes[visibleObject]);
                    break;

                default:
                    break;
            }

            setObjects(visibleObjects);
        }
        #endregion
    }
}
