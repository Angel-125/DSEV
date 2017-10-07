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

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public delegate void MassUpdateDelegate(float amount);
    public class MassconWindow : Dialog<MassconWindow>
    {
        public MassUpdateDelegate massUpdateDelegate;
        private string massAmount = "0";

        public MassconWindow() :
        base("Enter exact amount", 300, 150)
        {
            Resizable = false;
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (IsVisible() == false)
                return;
            GUILayout.BeginVertical();

            try
            {
                if (string.IsNullOrEmpty(massAmount))
                    GUILayout.Label("Mass:");
                else if (massAmount == ".")
                    GUILayout.Label("Mass: 0.");
                else
                    GUILayout.Label(string.Format("Mass: {0:f3} tonnes", float.Parse(massAmount)));
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                GUILayout.Label("Mass: Invalid!");
            }

            GUILayout.Label("Amount:");
            massAmount = GUILayout.TextField(massAmount);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                if (massUpdateDelegate != null)
                    massUpdateDelegate(float.Parse(massAmount));
                SetVisible(false);
                return;
            }

            if (GUILayout.Button("Cancel"))
                SetVisible(false);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
