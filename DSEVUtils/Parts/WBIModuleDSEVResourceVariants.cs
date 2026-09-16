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
    /// Coordinates stock part variants with optional resource-storage modules.
    /// </summary>
    public class WBIModuleDSEVResourceVariants : PartModule
    {
        private const string kDefaultStorageModuleName = "WBIOmniStorage";
        private const string kDefaultStorageEnabledKey = "isFuelTank";
        private const string kRemoveAllResourcesMethod = "RemoveAllResources";

        /// <summary>
        /// Name of the optional storage PartModule controlled by the selected variant.
        /// </summary>
        [KSPField]
        public string storageModuleName = kDefaultStorageModuleName;

        /// <summary>
        /// EXTRA_INFO key whose Boolean value determines whether storage is enabled.
        /// </summary>
        [KSPField]
        public string storageEnabledKey = kDefaultStorageEnabledKey;

        private PartModule storageModule;
        private MethodInfo removeAllResourcesMethod;

        public override void OnAwake()
        {
            base.OnAwake();
            GameEvents.onVariantApplied.Add(onVariantApplied);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            cacheStorageModule();

            ModulePartVariants variants = part.FindModuleImplementing<ModulePartVariants>();
            if (variants != null && variants.SelectedVariant != null)
                applyVariant(variants.SelectedVariant);
        }

        public void OnDestroy()
        {
            GameEvents.onVariantApplied.Remove(onVariantApplied);
        }

        private void onVariantApplied(Part variantPart, PartVariant variant)
        {
            if (variantPart != part || variant == null)
                return;

            applyVariant(variant);
        }

        private void applyVariant(PartVariant variant)
        {
            cacheStorageModule();
            if (storageModule == null)
                return;

            bool storageEnabled;
            string storageEnabledValue = variant.GetExtraInfoValue(storageEnabledKey);
            if (!bool.TryParse(storageEnabledValue, out storageEnabled))
                storageEnabled = false;

            if (!storageEnabled)
                removeAllResources();

            storageModule.enabled = storageEnabled;
            storageModule.isEnabled = storageEnabled;
            storageModule.moduleIsEnabled = storageEnabled;

            MonoUtilities.RefreshContextWindows(part);
        }

        private void cacheStorageModule()
        {
            if (storageModule != null)
                return;

            for (int index = 0; index < part.Modules.Count; index++)
            {
                PartModule candidate = part.Modules[index];
                if (candidate.moduleName != storageModuleName)
                    continue;

                storageModule = candidate;
                removeAllResourcesMethod = candidate.GetType().GetMethod(
                    kRemoveAllResourcesMethod,
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);
                break;
            }
        }

        private void removeAllResources()
        {
            if (removeAllResourcesMethod == null)
            {
                Debug.LogWarning("[WBIModuleDSEVResourceVariants] " + storageModuleName +
                    " does not provide " + kRemoveAllResourcesMethod + " on " + part.partInfo.name + ".");
                return;
            }

            try
            {
                removeAllResourcesMethod.Invoke(storageModule, null);
            }
            catch (Exception ex)
            {
                Debug.LogError("[WBIModuleDSEVResourceVariants] Unable to clear resources on " +
                    part.partInfo.name + ": " + ex);
            }
        }
    }
}
