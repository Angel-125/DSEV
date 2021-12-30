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
    }
}
