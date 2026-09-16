/*
Source code copyright 2026, by Michael Billard (Angel-125)
License: GPLV3

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Reflection;
using UnityEngine;

namespace WildBlueIndustries
{
    /// <summary>
    /// Enables an optional storage module only after a stock animation has finished deploying.
    /// </summary>
    public class WBIModuleDSEVDeployableStorage : PartModule
    {
        private const string kDefaultStorageModuleName = "WBIOmniStorage";
        private const string kRemoveAllResourcesMethod = "RemoveAllResources";
        private const string kSetContextGUIVisibleMethod = "SetContextGUIVisible";

        /// <summary>
        /// moduleID of the ModuleAnimateGeneric that deploys the storage volume.
        /// </summary>
        [KSPField]
        public string animationModuleID = "storageShieldDeploy";

        /// <summary>
        /// Name of the optional PartModule controlled by the deployment animation.
        /// </summary>
        [KSPField]
        public string storageModuleName = kDefaultStorageModuleName;

        /// <summary>
        /// Animation progress at which the storage module becomes available.
        /// </summary>
        [KSPField]
        public float deployedThreshold = 0.999f;

        /// <summary>
        /// Removes configured resources if the storage volume is folded again in the editor.
        /// </summary>
        [KSPField]
        public bool removeResourcesWhenDisabled = true;

        private ModuleAnimateGeneric deploymentAnimation;
        private PartModule storageModule;
        private MethodInfo removeAllResourcesMethod;
        private MethodInfo setContextGUIVisibleMethod;
        private bool? storageEnabled;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            cacheModules();
            if (deploymentAnimation == null || storageModule == null)
                return;

            deploymentAnimation.OnMoving.Add(onAnimationMoving);
            deploymentAnimation.OnStop.Add(onAnimationStopped);

            if (!deploymentAnimation.enabled)
            {
                setStorageEnabled(false);
                return;
            }

            bool isDeployed = !deploymentAnimation.IsMoving() &&
                deploymentAnimation.Progress >= deployedThreshold;
            setStorageEnabled(isDeployed);
        }

        public void OnDestroy()
        {
            if (deploymentAnimation == null)
                return;

            deploymentAnimation.OnMoving.Remove(onAnimationMoving);
            deploymentAnimation.OnStop.Remove(onAnimationStopped);
        }

        private void onAnimationMoving(float currentPosition, float targetPosition)
        {
            setStorageEnabled(false);
        }

        private void onAnimationStopped(float position)
        {
            setStorageEnabled(position >= deployedThreshold);
        }

        private void cacheModules()
        {
            for (int index = 0; index < part.Modules.Count; index++)
            {
                PartModule candidate = part.Modules[index];

                ModuleAnimateGeneric animation = candidate as ModuleAnimateGeneric;
                if (animation != null && animation.moduleID == animationModuleID)
                    deploymentAnimation = animation;

                if (candidate.moduleName == storageModuleName)
                    storageModule = candidate;
            }

            if (storageModule == null)
            {
                Debug.LogWarning("[WBIModuleDSEVDeployableStorage] Could not find " +
                    storageModuleName + " on " + getPartName() + ".");
                return;
            }

            Type storageType = storageModule.GetType();
            removeAllResourcesMethod = storageType.GetMethod(
                kRemoveAllResourcesMethod,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);
            setContextGUIVisibleMethod = storageType.GetMethod(
                kSetContextGUIVisibleMethod,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(bool) },
                null);

            if (deploymentAnimation == null)
            {
                Debug.LogWarning("[WBIModuleDSEVDeployableStorage] Could not find ModuleAnimateGeneric " +
                    animationModuleID + " on " + getPartName() + ".");
            }
        }

        private void setStorageEnabled(bool isDeployed)
        {
            if (storageModule == null || storageEnabled == isDeployed)
                return;

            if (!isDeployed)
            {
                setContextGUIVisible(false);
                if (removeResourcesWhenDisabled)
                    removeAllResources();
            }

            storageModule.enabled = isDeployed;
            storageModule.isEnabled = isDeployed;
            storageModule.moduleIsEnabled = isDeployed;

            if (isDeployed)
                setContextGUIVisible(true);

            storageEnabled = isDeployed;
            MonoUtilities.RefreshContextWindows(part);
        }

        private void setContextGUIVisible(bool isVisible)
        {
            invokeStorageMethod(setContextGUIVisibleMethod, new object[] { isVisible });
        }

        private void removeAllResources()
        {
            invokeStorageMethod(removeAllResourcesMethod, null);
        }

        private void invokeStorageMethod(MethodInfo method, object[] parameters)
        {
            if (method == null)
                return;

            try
            {
                method.Invoke(storageModule, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError("[WBIModuleDSEVDeployableStorage] Unable to update " +
                    storageModuleName + " on " + getPartName() + ": " + ex);
            }
        }

        private string getPartName()
        {
            return part.partInfo != null ? part.partInfo.name : part.name;
        }
    }
}
