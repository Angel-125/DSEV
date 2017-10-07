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
    public class DSEVSettings : Dialog<DSEVSettings>
    {
        static public float radiatorVolume;
        static public float radiatorSoundVolume = -1.0f;
        static public float arcJetVolume = -1.0f;

        string settingsPath;

        public DSEVSettings() :
        base("DSEV Settings", 300, 150)
        {
            Resizable = false;
            settingsPath = AssemblyLoader.loadedAssemblies.GetPathByType(typeof(DSEVSettings)) + "/Settings.cfg";
            loadSettings();
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("ArcJet Volume");
            GUILayout.Label("Coolant Dump Volume");
            radiatorVolume = GUILayout.HorizontalSlider(radiatorVolume, 0, 1);
            GUILayout.EndVertical();

            WBIHeatRadiator.soundEffectVolume = GameSettings.SHIP_VOLUME * radiatorVolume;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            ConfigNode nodeSettings = new ConfigNode();

            if (newValue)
            {
                loadSettings();
            }

            else
            {
                nodeSettings.name = "SETTINGS";
                nodeSettings.AddValue("radiatorVolume", radiatorVolume.ToString());
                nodeSettings.AddValue("radiatorSoundVolume", WBIHeatRadiator.soundEffectVolume.ToString());
                nodeSettings.Save(settingsPath);
            }
        }

        protected void loadSettings()
        {
            ConfigNode nodeSettings = new ConfigNode();
            string value;

            //Now load settings
            nodeSettings = ConfigNode.Load(settingsPath);
            if (nodeSettings != null)
            {
                value = nodeSettings.GetValue("radiatorVolume");
                if (string.IsNullOrEmpty(value) == false)
                    radiatorVolume = float.Parse(value);
                else
                    radiatorVolume = 1.0f;
            }
            else
            {
                radiatorVolume = 1.0f;
            }
        }

        public static float GetRadiatorVolume()
        {
            if (radiatorSoundVolume != -1.0f)
                return radiatorSoundVolume;

            string path = AssemblyLoader.loadedAssemblies.GetPathByType(typeof(DSEVSettings)) + "/Settings.cfg";
            ConfigNode nodeSettings;

            nodeSettings = ConfigNode.Load(path);
            if (nodeSettings == null)
                return 0f;

            radiatorSoundVolume = float.Parse(nodeSettings.GetValue("radiatorSoundVolume"));

            return radiatorSoundVolume;
        }

        public static float GetArcJetVolume()
        {
            if (arcJetVolume != -1.0f)
                return arcJetVolume;

            string path = AssemblyLoader.loadedAssemblies.GetPathByType(typeof(DSEVSettings)) + "/Settings.cfg";
            ConfigNode nodeSettings;

            nodeSettings = ConfigNode.Load(path);
            if (nodeSettings == null)
                return 0f;
            arcJetVolume = float.Parse(nodeSettings.GetValue("arcJetVolume"));

            return arcJetVolume;
        }
    }
}
