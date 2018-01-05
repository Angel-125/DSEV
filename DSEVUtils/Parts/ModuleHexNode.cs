using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class ModuleHexNode : WBIMeshHelper
    {
        [KSPField(isPersistant = true)]
        protected bool showSideNodes = true;

        protected List<AttachNode> sideNodes = new List<AttachNode>();

        [KSPEvent(guiActiveEditor = true, guiName = "Toggle Side Nodes")]
        public void ToggleSideNodes()
        {
            showSideNodes = !showSideNodes;
            List<AttachNode> doomedNodes = new List<AttachNode>();

            if (showSideNodes)
            {
                foreach (AttachNode node in sideNodes)
                    this.part.attachNodes.Add(node);
            }

            else
            {
                foreach (AttachNode attachNode in this.part.attachNodes)
                {
                    switch (attachNode.id)
                    {
                        case "front":
                        case "back":
                        case "left":
                        case "right":
                            doomedNodes.Add(attachNode);
                            break;
                    }
                }

                foreach (AttachNode doomed in doomedNodes)
                    this.part.attachNodes.Remove(doomed);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Grab the nodes
            sideNodes.Clear();
            foreach (AttachNode attachNode in this.part.attachNodes)
            {
                switch (attachNode.id)
                {
                    case "front":
                    case "back":
                    case "left":
                    case "right":
                        sideNodes.Add(attachNode);
                        break;
                }
            }
        }

    }
}
