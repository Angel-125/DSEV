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
    class ModuleMultiStorageTank : WBIMeshHelper
    {
        [KSPField(isPersistant = true)]
        public bool showSleeves;

        [KSPField(isPersistant = true)]
        public bool showBrackets;

        [KSPField(isPersistant = true)]
        public bool showDecals;

        [KSPField]
        public string sizeObjects = null;

        [KSPField]
        public string decalObjects = null;

        [KSPField]
        public string sizeCapacities = null;

        [KSPField]
        public string sizeMasses = null;

        [KSPField]
        public string sleevesObjects = null;

        [KSPField]
        public string bracketsObjects = null;

        [KSPField(isPersistant = true)]
        public int styleIndex;

        protected string[] sleevesObjectNames;
        protected string[] bracketsObjectNames;
        protected string[] sizeObjectNames;
        protected string[] decalObjectNames;
        protected List<float> sizeObjectCapacities = new List<float>();
        protected List<float> sizeObjMasses = new List<float>();

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle Sleeves", active = true)]
        public void ToggleSleeves()
        {
            ModuleMultiStorageTank storageTank;

            showSleeves = !showSleeves;

            setVisibleObjects();

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    storageTank = symmetryPart.GetComponent<ModuleMultiStorageTank>();
                    storageTank.ShowSleeves(showSleeves);
                }
            }
        }

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle Brackets", active = true)]
        public void ToggleBrackets()
        {
            ModuleMultiStorageTank storageTank;

            showBrackets = !showBrackets;

            setVisibleObjects();

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    storageTank = symmetryPart.GetComponent<ModuleMultiStorageTank>();
                    storageTank.ShowBrackets(showBrackets);
                }
            }
        }

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle Style", active = true)]
        public void ToggleStyle()
        {
            ModuleMultiStorageTank storageTank;
            if (HighLogic.LoadedSceneIsEditor)
            {
                //If there are parts attached then don't allow the switch.
                foreach (Part childPart in this.part.children)
                    if (childPart.attachMode == AttachModes.SRF_ATTACH)
                    {
                        ScreenMessages.PostScreenMessage("Cannot change tank diameter until attached parts are removed.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }

                //Cycle through the size objects
                styleIndex += 1;
                if (styleIndex > sizeObjectCapacities.Count - 1)
                    styleIndex = 0;

                //Now set up the stats and such
                ToggleStyle(styleIndex);

                //Handle symmetrical parts
                if (HighLogic.LoadedSceneIsEditor)
                {
                    foreach (Part symmetryPart in this.part.symmetryCounterparts)
                    {
                        storageTank = symmetryPart.GetComponent<ModuleMultiStorageTank>();
                        storageTank.ToggleStyle(styleIndex);
                    }
                }
            }
        }

        public void ToggleStyle(int index)
        {
            styleIndex = index;

            setVisibleObjects();

            //Get the resource switcher
            WBIResourceSwitcher resourceSwitcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();
            if (resourceSwitcher == null)
                return;

            //Set capacity
            resourceSwitcher.capacityFactor = sizeObjectCapacities[styleIndex];

            //Set mass
            this.part.mass = sizeObjMasses[styleIndex];

            //Now reload resource template
            resourceSwitcher.ReloadTemplate();
        }

        public void ShowBrackets(bool bracketsAreVisible)
        {
            showBrackets = bracketsAreVisible;

            setVisibleObjects();
        }

        public void ShowSleeves(bool sleevesAreVisible)
        {
            showSleeves = sleevesAreVisible;

            setVisibleObjects();
        }

        public void ShowDecals(bool decalsAreVisible)
        {
            showDecals = decalsAreVisible;

            setVisibleObjects();
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            sleevesObjects = protoNode.GetValue("sleevesObjects");

            bracketsObjects = protoNode.GetValue("bracketsObjects");

            sizeObjects = protoNode.GetValue("sizeObjects");

            decalObjects = protoNode.GetValue("decalObjects");

            sizeCapacities = protoNode.GetValue("sizeCapacities");

            sizeMasses = protoNode.GetValue("sizeMasses");
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                parseStyleObjects();
                setVisibleObjects();
            }
        }

        protected void parseStyleObjects()
        {
            char[] delimiters = new char[] { ';' };

            if (string.IsNullOrEmpty(sleevesObjects) == false)
                sleevesObjectNames = sleevesObjects.Split(delimiters);

            if (string.IsNullOrEmpty(sizeObjects) == false)
                sizeObjectNames = sizeObjects.Split(delimiters);

            if (string.IsNullOrEmpty(decalObjects) == false)
                decalObjectNames = decalObjects.Split(delimiters);

            if (string.IsNullOrEmpty(bracketsObjects) == false)
                bracketsObjectNames = bracketsObjects.Split(delimiters);

            string[] capacities;
            if (string.IsNullOrEmpty(sizeCapacities) == false)
            {
                capacities = sizeCapacities.Split(delimiters);
                foreach (string capacity in capacities)
                    sizeObjectCapacities.Add(float.Parse(capacity));
            }

            string[] masses;
            if (string.IsNullOrEmpty(sizeMasses) == false)
            {
                masses = sizeMasses.Split(delimiters);
                foreach (string sizeMass in masses)
                    sizeObjMasses.Add(float.Parse(sizeMass));
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            parseStyleObjects();

            Events["NextMesh"].guiActive = false;
            Events["NextMesh"].active = false;
            Events["PrevMesh"].active = false;
            Events["PrevMesh"].guiActive = false;

            if (string.IsNullOrEmpty(sleevesObjects))
            {
                Events["ToggleSleeves"].guiActive = false;
                Events["ToggleSleeves"].active = false;
            }
            else
            {
                Events["ToggleSleeves"].guiActive = true;
                Events["ToggleSleeves"].active = true;
            }

            if (string.IsNullOrEmpty(bracketsObjects))
            {
                Events["ToggleBrackets"].guiActive = false;
                Events["ToggleBrackets"].active = false;
            }

            if (string.IsNullOrEmpty(decalObjects))
            {
                Events["ToggleDecals"].guiActive = false;
                Events["ToggleDecals"].active = false;
            }

            if (string.IsNullOrEmpty(decalObjects))
            {
                Events["ToggleStyle"].guiActive = false;
                Events["ToggleStyle"].active = false;
            }

            setVisibleObjects();
        }

        public override string GetInfo()
        {
            string info = "Right-click on the part to change resource settings, toggle decals, toggle end cap sleeves and toggle mounting brackets. You can also change the tank size and style.";

            return info;
        }

        public override void OnEditorAttach()
        {
            base.OnEditorAttach();
            setVisibleObjects();
        }

        protected virtual void setVisibleObjects()
        {
            List<int> visibleObjects = new List<int>();

            if (showSleeves)
                visibleObjects.Add(meshIndexes[sleevesObjectNames[styleIndex]]);

            if (showBrackets)
                visibleObjects.Add(meshIndexes[bracketsObjectNames[styleIndex]]);

            if (string.IsNullOrEmpty(sizeObjects) == false)
                visibleObjects.Add(meshIndexes[sizeObjectNames[styleIndex]]);

            if (showDecals)
                visibleObjects.Add(meshIndexes[decalObjectNames[styleIndex]]);

            setObjects(visibleObjects);
        }

    }
}
