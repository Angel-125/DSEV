using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildBlueIndustries.Parts
{
    public class WBIDynamicHeatShield: PartModule
    {
        [KSPField()]
        public float maxTemp = 3400f;

        /*
        /// <summary>
        /// The activation switch.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Magneto Plasma Shield", guiActive = true)]
        [UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
        public bool isActivated = true;
        */

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            int count = part.vessel.parts.Count;
            for (int index = 0; index < count; index++)
            {
                part.vessel.parts[index].maxTemp = maxTemp;
                part.vessel.parts[index].skinMaxTemp = maxTemp;
            }
        }

        private ConfigNode getPartConfigNode()
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return null;
            if (this.part.partInfo.partConfig == null)
                return null;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode partConfigNode = null;
            ConfigNode node = null;
            string moduleName;

            //Get the switcher config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        partConfigNode = node;
                        break;
                    }
                }
            }

            return partConfigNode;
        }
    }
}
