/*
Source code copyright 2018-2026, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a fictitious entity
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using UnityEngine;

namespace WildBlueIndustries
{
    /// <summary>
    /// Extends the stock animation module with start, loop, and stop sounds and
    /// optional activation of other part modules while the animation is deployed.
    /// </summary>
    public class WBIModuleDSEVAnimated : ModuleAnimateGeneric
    {
        [KSPField]
        public bool debugMode = false;

        [KSPField]
        public string startSoundURL = string.Empty;

        [KSPField]
        public float startSoundPitch = 1.0f;

        [KSPField]
        public float startSoundVolume = 0.5f;

        [KSPField]
        public string loopSoundURL = string.Empty;

        [KSPField]
        public float loopSoundPitch = 1.0f;

        [KSPField]
        public float loopSoundVolume = 0.5f;

        [KSPField]
        public string stopSoundURL = string.Empty;

        [KSPField]
        public float stopSoundPitch = 1.0f;

        [KSPField]
        public float stopSoundVolume = 0.5f;

        [KSPField]
        public string enabledModules = string.Empty;

        // Compatibility alias used by existing DSEV configurations.
        [KSPField]
        public string actionGroupName = string.Empty;

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        [KSPField(isPersistant = true)]
        public bool modulesEnabled = false;

        protected AudioSource loopSound;
        protected AudioSource startSound;
        protected AudioSource stopSound;
        protected bool isMoving;

        /// <summary>
        /// Toggles the animation and immediately updates the requested deployment
        /// state. This provides the small API used by legacy DSEV modules.
        /// </summary>
        public void ToggleAnimation()
        {
            Toggle();
            isDeployed = !isDeployed;
        }

        /// <summary>
        /// Shows or hides the stock animation event in the editor and flight.
        /// </summary>
        public void showGui(bool isVisible)
        {
            Events["Toggle"].guiActive = isVisible;
            Events["Toggle"].guiActiveEditor = isVisible;
            Events["Toggle"].guiActiveUnfocused = isVisible;
        }

        public override void OnStart(StartState state)
        {
            if (!string.IsNullOrEmpty(actionGroupName))
                actionGUIName = actionGroupName;

            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            startSound = createAudioSource(startSoundURL, startSoundPitch, startSoundVolume, false);
            loopSound = createAudioSource(loopSoundURL, loopSoundPitch, loopSoundVolume, true);
            stopSound = createAudioSource(stopSoundURL, stopSoundPitch, stopSoundVolume, false);

            Fields["isDeployed"].guiActive = debugMode;
            Fields["isDeployed"].guiActiveEditor = debugMode;
            Fields["modulesEnabled"].guiActive = debugMode;
            Fields["modulesEnabled"].guiActiveEditor = debugMode;

            setModulesActive(isDeployed);
            modulesEnabled = isDeployed;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (isMoving)
                updateDeployStatus();

            if (isDeployed != modulesEnabled)
            {
                setModulesActive(isDeployed);
                modulesEnabled = isDeployed;
            }

            if (aniState == animationStates.MOVING && !isMoving)
            {
                isMoving = true;
                playStartSounds();
            }
            else if ((aniState == animationStates.LOCKED || aniState == animationStates.CLAMPED) && isMoving)
            {
                isMoving = false;
                playStopSounds();
            }
        }

        protected AudioSource createAudioSource(string soundURL, float pitch, float volume, bool loop)
        {
            if (string.IsNullOrEmpty(soundURL))
                return null;

            AudioClip clip = GameDatabase.Instance.GetAudioClip(soundURL);
            if (clip == null)
            {
                Debug.LogWarning("[WBIModuleDSEVAnimated] Unable to find audio clip: " + soundURL);
                return null;
            }

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = loop;
            audioSource.pitch = pitch;
            audioSource.volume = GameSettings.SHIP_VOLUME * volume;
            return audioSource;
        }

        protected void playStartSounds()
        {
            if (startSound != null)
                startSound.Play();
            if (loopSound != null)
                loopSound.Play();
        }

        protected void playStopSounds()
        {
            if (stopSound != null)
                stopSound.Play();
            if (loopSound != null)
                loopSound.Stop();
        }

        protected void updateDeployStatus()
        {
            if (anim == null || anim[animationName] == null)
                return;

            if (anim[animationName].normalizedTime >= 0.999f)
                isDeployed = true;
            else if (anim[animationName].normalizedTime < 0.001f)
                isDeployed = false;
        }

        protected void setModulesActive(bool isActive)
        {
            if (string.IsNullOrEmpty(enabledModules) || !HighLogic.LoadedSceneIsFlight)
                return;

            for (int index = 0; index < part.Modules.Count; index++)
            {
                PartModule partModule = part.Modules[index];
                if (!enabledModules.Contains(partModule.moduleName))
                    continue;

                partModule.enabled = isActive;
                partModule.isEnabled = isActive;
                partModule.moduleIsEnabled = isActive;
            }
        }
    }
}
