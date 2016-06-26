using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum CoolingCycleModes
    {
        closed,  //Heat is actively transferred into the radiator, but it relies on stock game to cool off.
        open     //Heat is actively transferred into the radiator, and coolant is expelled to rapidly cool the radiator.
    }

    public struct CoolantResource
    {
        public string name;
        public ResourceFlowMode flowMode;
        public float ratio;
    }

    public class WBIHeatRadiator : ModuleDeployableRadiator, IOpsView
    {
        public static float soundEffectVolume = GameSettings.SHIP_VOLUME;
        private const float maxSoundDistance = 1.0f;
        private const float kGlowTempOffset = 600f; //My custom Draper Point...
        private const string kDefaultCoolant = "Coolant";

        [KSPField]
        public string coolantDumpEffect;

        [KSPField]
        public string soundFilePath;

        //How many units of coolant to dump overboard during
        //open-cycle cooling.
        [KSPField(isPersistant = true)]
        public float coolantDumpRate;

        //How many units of coolant/sec is lost while the ship is
        //under acceleration
        [KSPField(isPersistant = true)]
        public float lossRateAccelerating;

        //Current cooling cycle mode.
        [KSPField(isPersistant = true)]
        public CoolingCycleModes coolingCycleMode;

        //Amount of ec per second required to run the radiator, if any.
        [KSPField(isPersistant = true)]
        public double ecRequired;

        public FXGroup soundClip = null;

        protected List<CoolantResource> coolantResources = new List<CoolantResource>();
        protected bool outOfElectricCharge = false;
        PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
        PartResourceDefinition electricChargeDef;
        protected FixedUpdateHelper fixedUpdateHelper;

        #region Overrides and API
        [KSPAction("Toggle Cooling Cycle")]
        public void ToggleGoolingModeAction(KSPActionParam param)
        {
            ToggleCoolingMode();
        }

        [KSPAction("Open Cooling Cycle")]
        public void OpenModeAction(KSPActionParam param)
        {
            coolingCycleMode = CoolingCycleModes.open;
            Events["ToggleCoolingMode"].guiName = "Cooling Mode (open)";
        }

        [KSPAction("Closed Cooling Cycle")]
        public void ClosedModeAction(KSPActionParam param)
        {
            coolingCycleMode = CoolingCycleModes.closed;
            Events["ToggleCoolingMode"].guiName = "Cooling Mode (closed)";
        }

        [KSPEvent(guiActive = true, guiName = "Cooling Mode", active = true, externalToEVAOnly = false, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public void ToggleCoolingMode()
        {
            if (coolingCycleMode == CoolingCycleModes.closed)
            {
                coolingCycleMode = CoolingCycleModes.open;
                Events["ToggleCoolingMode"].guiName = "Cooling Mode (open)";
                showParticleEffects(true);
                if (soundClip != null)
                {
                    soundClip.audio.volume = soundEffectVolume;
                    soundClip.audio.Play();
                }
            }

            else
            {
                coolingCycleMode = CoolingCycleModes.closed;
                Events["ToggleCoolingMode"].guiName = "Cooling Mode (closed)";
                showParticleEffects(false);
                if (soundClip != null)
                    soundClip.audio.Stop();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            //ModuleDeployableRadiator

            //Get the resource definition for electric charge.
            electricChargeDef = definitions["ElectricCharge"];

            //Set cooling mode. For now, default is closed.
            coolingCycleMode = CoolingCycleModes.closed;
            Events["ToggleCoolingMode"].guiName = "Cooling Mode (closed)";

            //Dig into the proto part and find the coolant resource nodes.
            getCoolantNodes();

            //Load the sound effects
            LoadSoundFX();

            //Create fixed update helper
            fixedUpdateHelper = this.part.gameObject.AddComponent<FixedUpdateHelper>();
            fixedUpdateHelper.onFixedUpdateDelegate = UpdateState;
            fixedUpdateHelper.enabled = true;
        }

        public override string GetInfo()
        {
            string info = base.GetInfo();

            if (coolantDumpRate > 0.001)
            {
                info += string.Format("<b>\n-Coolant Dump Rate:</b> {0:f2}u/sec", coolantDumpRate);
                info += "\n\nRight-click the radiator to dump coolant and rapidly cool the radiator.";
            }

            return info;
        }

        #endregion

        #region Helpers
        public void UpdateState()
        {
            //Do we have enough electricity to run the radiator?
            if (ecRequired > 0.001 && panelState == panelStates.EXTENDED)
            {
                double ecPerTimeTick = ecRequired * TimeWarp.fixedDeltaTime;
                double ecObtained = this.part.RequestResource("ElectricCharge", ecPerTimeTick, ResourceFlowMode.ALL_VESSEL);

                if (ecObtained / ecPerTimeTick < 0.999)
                {
                    outOfElectricCharge = true;
                    this.part.RequestResource("ElectricCharge", -ecObtained);
                    return;
                }
                outOfElectricCharge = false;
            }

            //For open-cycle cooling, dump coolant resources overboard and adjust thermal energy accordingly.
            if (coolingCycleMode == CoolingCycleModes.open || (lossRateAccelerating > 0f && this.part.vessel.acceleration.magnitude > 0f))
            {
                //Now go through the list of coolants and dump them overboard, carrying heat with them.
                foreach (CoolantResource coolant in coolantResources)
                {
                    if (coolingCycleMode == CoolingCycleModes.open)
                        dumpCoolant(coolant, coolantDumpRate);

                    if (lossRateAccelerating > 0f)
                        dumpCoolant(coolant, lossRateAccelerating);
                }
            }

            //Now set the radiator color
            //The game's built-in shader is working but it doesn't look as nice.
            SetRadiatorColor();
        }

        public void LoadSoundFX()
        {
            if (!GameDatabase.Instance.ExistsAudioClip(soundFilePath))
                return;

            soundClip.audio = part.gameObject.AddComponent<AudioSource>();
            soundClip.audio.volume = soundEffectVolume;
            soundClip.audio.maxDistance = maxSoundDistance;

            soundClip.audio.clip = GameDatabase.Instance.GetAudioClip(soundFilePath);
            soundClip.audio.loop = true;

            soundClip.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            soundClip.audio.panStereo = 1f;
            soundClip.audio.spatialBlend = 1f;
            soundClip.audio.dopplerLevel = 0f;

            soundClip.audio.playOnAwake = false;
        }

        protected void showParticleEffects(bool isVisible)
        {
            if (string.IsNullOrEmpty(coolantDumpEffect))
                return;

            KSPParticleEmitter[] emitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            if (emitters == null)
                return;

            foreach (KSPParticleEmitter emitter in emitters)
            {
                //If the emitter is on the list then show it
                if (emitter.name == coolantDumpEffect)
                {
                    emitter.emit = isVisible;
                    emitter.enabled = isVisible;
                }

                //Emitter is not on the list, hide it.
                else
                {
                    emitter.emit = false;
                    emitter.enabled = false;
                }
            }
        }

        public void SetRadiatorColor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            //Really, *really* want to use the stock glow shader. I don't know how yet though.
            Renderer[] renderers = this.part.FindModelComponents<Renderer>();

            //Account for Draper Point
            float ratio = (float)(this.part.temperature - kGlowTempOffset) / (float)(this.part.maxTemp - kGlowTempOffset);

            if (ratio < 0.0f)
                ratio = 0f;

            //Set the emissive color
            foreach (Renderer renderer in renderers)
                renderer.material.SetColor("_EmissiveColor", new Color(ratio, ratio, ratio));
        }

        protected void dumpCoolant(CoolantResource coolant, double dumpRate)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            double coolantToDump = dumpRate * TimeWarp.fixedDeltaTime;
            double coolantDumped = 0;
            double thermalEnergyCoolant = 0;

            //The the resource definition
            resourceDef = definitions[coolant.name];

            //Now calculate the resource amount dumped and the thermal energy of that slug of resource.
            coolantDumped = this.part.RequestResource(resourceDef.id, coolantToDump * coolant.ratio, coolant.flowMode);
            if (coolantDumped <= 0.001)
            {
                if (soundClip != null)
                    soundClip.audio.Stop();
                showParticleEffects(false);
                ToggleCoolingMode();
                return;
            }
            thermalEnergyCoolant = this.part.temperature * this.part.resourceThermalMass * coolantDumped;

            //Practice conservation of energy...
            if (coolantDumped > 0.001)
                this.part.AddThermalFlux(-thermalEnergyCoolant);
        }

        protected void getCoolantNodes()
        {
            if (this.part.protoPartSnapshot != null)
            {
                if (this.part.protoPartSnapshot.partInfo != null)
                {
                    //Aha! the part's config file!
                    //Now go find the MODULE definition for ModuleRadiator
                    if (this.part.protoPartSnapshot.partInfo.partConfig != null)
                    {
                        string value;
                        ConfigNode[] moduleNodes = this.part.protoPartSnapshot.partInfo.partConfig.GetNodes("MODULE");

                        if (moduleNodes == null)
                            return;

                        //Find our module definition.
                        foreach (ConfigNode moduleNode in moduleNodes)
                        {
                            value = moduleNode.GetValue("name");
                            if (string.IsNullOrEmpty(value))
                                continue;

                            //Aha! found our module definition!
                            //Now get the coolants
                            if (value == this.ClassName)
                            {
                                CoolantResource coolant;
                                ConfigNode[] coolantResourceNodes = moduleNode.GetNodes("INPUT_RESOURCE");
                                foreach (ConfigNode node in coolantResourceNodes)
                                {
                                    coolant = new CoolantResource();
                                    coolant.name = node.GetValue("name");
                                    coolant.flowMode = (ResourceFlowMode)Enum.Parse(typeof(ResourceFlowMode), node.GetValue("flowMode"));
                                    coolant.ratio = float.Parse(node.GetValue("ratio"));
                                    coolantResources.Add(coolant);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region IOpsView
        public List<string> GetButtonLabels()
        {
            throw new NotImplementedException();
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginScrollView(new Vector2(), new GUIStyle(GUI.skin.textArea), new GUILayoutOption[] { GUILayout.Height(480) });

            GUILayout.Label("<color=white>Status: " + status + "</color>");

            if (panelState == panelStates.RETRACTED)
            {
                if (GUILayout.Button(Events["Extend"].guiName))
                    Extend();
            }
            else if (panelState == panelStates.EXTENDED)
            {
                if (GUILayout.Button(Events["Retract"].guiName))
                    Retract();
            }
            
            if (GUILayout.Button(Events["ToggleCoolingMode"].guiName))
                ToggleCoolingMode();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void SetContextGUIVisible(bool isVisible)
        {
        }

        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }
        #endregion
    }
}
