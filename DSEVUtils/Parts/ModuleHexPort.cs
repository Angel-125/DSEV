using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using WBIResources;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
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
    public class ModuleHexPort : WBIMeshHelper
    {
        [KSPField(isPersistant = true)]
        public bool showBracing;

        [KSPField(isPersistant = true)]
        public bool showClosedMesh;

        protected string bracingObject;
        protected string openMeshObject;
        protected string closedMeshObject;

        public void SetVisibleObjects()
        {
            List<int> visibleObjects = new List<int>();

            if (showBracing && bracingObject != null)
                visibleObjects.Add(meshIndexes[bracingObject]);

            if (showClosedMesh && closedMeshObject != null)
                visibleObjects.Add(meshIndexes[closedMeshObject]);
            else if (openMeshObject != null)
                visibleObjects.Add(meshIndexes[openMeshObject]);

            setObjects(visibleObjects);

            if (showClosedMesh)
                Events["TogglePressurized"].guiName = "Unpressurized";
            else
                Events["TogglePressurized"].guiName = "Pressurized";
        }

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Toggle bracing")]
        public void ToggleBracing()
        {
            showBracing = !showBracing;

            SetVisibleObjects();

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    ModuleHexPort hexPort = symmetryPart.GetComponent<ModuleHexPort>();

                    hexPort.showBracing = showBracing;
                    hexPort.SetVisibleObjects();
                }
            }
        }

        [KSPEvent(guiActiveEditor = true, guiActive = false, guiName = "Pressurized")]
        public void TogglePressurized()
        {
            showClosedMesh = !showClosedMesh;

            SetVisibleObjects();

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    ModuleHexPort hexPort = symmetryPart.GetComponent<ModuleHexPort>();

                    hexPort.showClosedMesh = showClosedMesh;
                    hexPort.SetVisibleObjects();
                }
            }
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            bracingObject = protoNode.GetValue("bracingObject");
            openMeshObject = protoNode.GetValue("openMeshObject");
            closedMeshObject = protoNode.GetValue("closedMeshObject");
        }

        public override void OnEditorAttach()
        {
            base.OnEditorAttach();

            SetVisibleObjects();

            Events["PrevMesh"].active = false;
            Events["PrevMesh"].guiActive = false;
            Events["PrevMesh"].guiActiveEditor = false;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (this.objectTransforms.Count > 0)
                SetVisibleObjects();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (HighLogic.LoadedSceneIsFlight == false)
                setObject(1);
        }
    }
}
