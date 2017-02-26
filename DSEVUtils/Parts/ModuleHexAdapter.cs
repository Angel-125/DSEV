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
    class ModuleHexAdapter : WBIMeshHelper
    {
        [KSPField]
        public string styleNames = null;

        [KSPField]
        public string frameNames = null;

        [KSPField(isPersistant = true)]
        public int frameIndex;

        [KSPField(isPersistant = true)]
        public int styleIndex;

        protected string[] frameObjectNames;
        protected string[] styleObjectNames;

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle Frame", active = true)]
        public void ToggleFrame()
        {
            ModuleHexAdapter adapter;

            frameIndex += 1;
            if (frameIndex == frameObjectNames.Length)
                frameIndex = 0;

            ToggleFrame(frameIndex);

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    adapter = symmetryPart.GetComponent<ModuleHexAdapter>();
                    adapter.ToggleFrame(frameIndex);
                }
            }
        }

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle Style", active = true)]
        public void ToggleStyle()
        {
            ModuleHexAdapter adapter;

            styleIndex += 1;
            if (styleIndex == styleObjectNames.Length)
                styleIndex = 0;

            ToggleStyle(styleIndex);

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    adapter = symmetryPart.GetComponent<ModuleHexAdapter>();
                    adapter.ToggleStyle(styleIndex);
                }
            }
        }

        public void ToggleStyle(int index)
        {
            styleIndex = index;

            int nextIndex = styleIndex + 1;
            if (nextIndex == styleObjectNames.Length)
                nextIndex = 0;
            Events["ToggleStyle"].guiName = styleObjectNames[nextIndex];

            setVisibleObjects();
        }

        public void ToggleFrame(int index)
        {
            frameIndex = index;

            int nextIndex = frameIndex + 1;
            if (nextIndex == frameObjectNames.Length)
                nextIndex = 0;
            Events["ToggleFrame"].guiName = frameObjectNames[nextIndex];

            setVisibleObjects();
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            frameNames = protoNode.GetValue("frameNames");

            styleNames = protoNode.GetValue("styleNames");
        }

        protected void parseStyleObjects()
        {
            char[] delimiters = new char[] { ';' };

            if (string.IsNullOrEmpty(frameNames) == false)
                frameObjectNames = frameNames.Split(delimiters);

            if (string.IsNullOrEmpty(styleNames) == false)
                styleObjectNames = styleNames.Split(delimiters);
        }

        public override void OnStart(StartState state)
        {
            int index;
            base.OnStart(state);

            parseStyleObjects();

            Events["NextMesh"].guiActive = false;
            Events["NextMesh"].active = false;
            Events["PrevMesh"].active = false;
            Events["PrevMesh"].guiActive = false;

            index = styleIndex + 1;
            if (index == styleObjectNames.Length)
                index = 0;
            Events["ToggleStyle"].guiName = styleObjectNames[index];


            index = frameIndex + 1;
            if (index == frameObjectNames.Length)
                index = 0;
            Events["ToggleFrame"].guiName = frameObjectNames[index];

            setVisibleObjects();
        }

        public override string GetInfo()
        {
            string info = "Right-click on the part to change style and size.";

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

            if (meshIndexes.ContainsKey(frameObjectNames[frameIndex]))
                visibleObjects.Add(meshIndexes[frameObjectNames[frameIndex]]);

            if (meshIndexes.ContainsKey(styleObjectNames[styleIndex]))
                visibleObjects.Add(meshIndexes[styleObjectNames[styleIndex]]);

            setObjects(visibleObjects);
        }

    }
}
