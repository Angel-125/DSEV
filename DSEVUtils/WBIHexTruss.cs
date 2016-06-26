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
    public class WBIHexTruss : ExtendedPartModule
    {
        [KSPField(isPersistant = true)]
        public string currentTruss;

        [KSPField]
        public string endCapNames;

        [KSPField]
        public string rackNames;

        [KSPField]
        public string centerVestibuleName;

        protected Dictionary<string, ConfigNode> trussConfigs = new Dictionary<string, ConfigNode>();

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);
            ConfigNode[] trussNodes = protoNode.GetNodes("TRUSS");

            foreach (ConfigNode trussNode in trussNodes)
                trussConfigs.Add(trussNode.GetValue("name"), trussNode);

            if (string.IsNullOrEmpty(currentTruss))
                currentTruss = protoNode.GetValue("currentTruss");
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            configureTruss("HexNode");
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            configureTruss(currentTruss);
        }

        protected void configureTruss(string trussName)
        {
            Log("configureTruss called for truss type: " + trussName);
            ConfigNode node;
            Transform mesh;
            Collider collider;

            if (trussConfigs.ContainsKey(trussName) == false)
                return;
            node = trussConfigs[trussName];

            //Hide all the trusses
            foreach (string key in trussConfigs.Keys)
            {
                mesh = this.part.FindModelTransform(key);
                mesh.gameObject.SetActive(false);
                collider = mesh.gameObject.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = false;
            }

            //Show the current truss
            mesh = this.part.FindModelTransform(trussName);
            if (mesh != null)
            {
                //Setup the mesh transforms
                mesh.gameObject.SetActive(true);
                collider = mesh.gameObject.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = true;

                //Now adjust the nodes
            }
        }

    }
}
