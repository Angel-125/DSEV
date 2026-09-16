using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using KSP.IO;

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
    public struct WBIHexTrussOption
    {
        public string name;
        public string meshes;
        public bool isFuelTank;
    }

    public class WBIHexTruss : WBIMeshHelper
    {
        public const string kOptionNode = "OPTION";
        public const string kOptionLabel = "Option: ";

        [KSPField]
        public string defaultOption = string.Empty;

        [KSPField(isPersistant = true)]
        public int currentOptionIndex = -1;

        protected string[] optionNames = null;
        Dictionary<string, WBIHexTrussOption> trussOptions = new Dictionary<string, WBIHexTrussOption>();
        Dictionary<string, Transform> trussObjects = new Dictionary<string, Transform>();
        PartModule omniStorage = null;
        MethodInfo removeAllResourcesMethod = null;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            for (int index = 0; index < part.Modules.Count; index++)
            {
                if (part.Modules[index].moduleName != "WBIOmniStorage")
                    continue;

                omniStorage = part.Modules[index];
                removeAllResourcesMethod = omniStorage.GetType().GetMethod(
                    "RemoveAllResources",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);
                break;
            }

            //Hide base GUI
            Events["NextMesh"].guiActive = false;
            Events["NextMesh"].active = false;
            Events["PrevMesh"].active = false;
            Events["PrevMesh"].guiActive = false;

            //Get the mesh options
            getOptionNodes();
            checkDefaultOption();
            Events["ToggleOption"].guiName = kOptionLabel + optionNames[currentOptionIndex];

            //Set up the mesh
            if (currentOptionIndex != -1)
            {
                WBIHexTrussOption trussOption = trussOptions[optionNames[currentOptionIndex]];
                setVisibleObjects(trussOption);
            }
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Toggle Option")]
        public virtual void ToggleOption()
        {
            WBIHexTruss hexTruss = null;
            Part[] symmetryParts;

            //Cycle to the next option
            currentOptionIndex = (currentOptionIndex + 1) % optionNames.Length;
            ToggleOption(currentOptionIndex);

            //Update symmetry parts
            if (this.part.symmetryCounterparts.Count > 0)
            {
                symmetryParts = this.part.symmetryCounterparts.ToArray();
                for (int index = 0; index < symmetryParts.Length; index++)
                {
                    hexTruss = symmetryParts[index].FindModuleImplementing<WBIHexTruss>();
                    hexTruss.currentOptionIndex = this.currentOptionIndex;
                    hexTruss.ToggleOption(currentOptionIndex);
                }
            }
        }

        public virtual void ToggleOption(int optionIndex)
        {
            currentOptionIndex = optionIndex;
            Events["ToggleOption"].guiName = kOptionLabel + optionNames[currentOptionIndex];

            //Get the truss option
            WBIHexTrussOption trussOption = trussOptions[optionNames[currentOptionIndex]];

            //Setup the mesh options
            setVisibleObjects(trussOption);

            //Setup the fuel tank
            if (omniStorage != null)
            {
                omniStorage.isEnabled = trussOption.isFuelTank;
                omniStorage.enabled = trussOption.isFuelTank;
                if (!trussOption.isFuelTank)
                    removeAllResourcesMethod?.Invoke(omniStorage, null);
            }

            MonoUtilities.RefreshContextWindows(this.part);
        }

        protected void setVisibleObjects(WBIHexTrussOption trussOption)
        {
            List<int> visibleObjects = new List<int>();
            string[] meshes = trussOption.meshes.Split(new char[] { ';' });
            string meshName;
            int meshIndex;

            for (int index = 0; index < meshes.Length; index++)
            {
                meshName = meshes[index];
                if (meshIndexes.ContainsKey(meshName))
                {
                    meshIndex = meshIndexes[meshName];
                    visibleObjects.Add(meshIndex);
                }
            }
            if (visibleObjects.Count > 0)
                setObjects(visibleObjects);
        }

        protected void checkDefaultOption()
        {
            if (optionNames == null || optionNames.Length == 0)
                return;
            if (currentOptionIndex == -1 && string.IsNullOrEmpty(defaultOption) == false)
            {
                for (int index = 0; index < optionNames.Length; index++)
                {
                    if (optionNames[index] == defaultOption)
                    {
                        currentOptionIndex = index;
                        ToggleOption(currentOptionIndex);
                        break;
                    }
                }
            }

            if (currentOptionIndex == -1)
            {
                currentOptionIndex = 0;
                ToggleOption(currentOptionIndex);
            }
        }

        public void getOptionNodes(string nodeName = kOptionNode)
        {
            if (this.part.partInfo.partConfig == null)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode trussNode = null;
            ConfigNode node = null;
            string moduleName;
            List<string> optionNamesList = new List<string>();

            //Get the config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        trussNode = node;
                        break;
                    }
                }
            }
            if (trussNode == null)
                return;

            //Get the nodes we're interested in
            nodes = trussNode.GetNodes(nodeName);

            //Get the truss options
            WBIHexTrussOption hexTrussOption;
            string optionName = string.Empty;
            string optionMeshes = string.Empty;
            bool isFuelTank = false;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].HasValue("name"))
                    optionName = nodes[index].GetValue("name");
                else
                    optionMeshes = string.Empty;

                if (nodes[index].HasValue("meshes"))
                    optionMeshes = nodes[index].GetValue("meshes");
                else
                    optionMeshes = string.Empty;

                if (nodes[index].HasValue("isFuelTank"))
                    isFuelTank = bool.Parse(nodes[index].GetValue("isFuelTank"));
                else
                    isFuelTank = false;

                if (!string.IsNullOrEmpty(optionName) && !string.IsNullOrEmpty(optionMeshes))
                {
                    optionNamesList.Add(nodes[index].GetValue("name"));
                    hexTrussOption = new WBIHexTrussOption();
                    hexTrussOption.name = optionName;
                    hexTrussOption.meshes = optionMeshes;
                    hexTrussOption.isFuelTank = isFuelTank;
                    trussOptions.Add(optionName, hexTrussOption);
                }

            }
            if (optionNamesList.Count > 0)
            {
                optionNames = optionNamesList.ToArray();
            }
        }
    }
}
