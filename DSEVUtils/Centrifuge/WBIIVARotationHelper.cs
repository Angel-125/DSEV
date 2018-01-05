/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    public class WBIIVARotationHelper : PartModule
    {
        [KSPField]
        public string ivaTransformName = string.Empty;

        [KSPField]
        public string externalTransformName = string.Empty;

        [KSPField(guiActive = true)]
        public bool alignWithExternalMesh;

        protected Transform ivaTransform;
        protected Transform externalTransform;
        public Quaternion referenceRotation;
        public Quaternion internalRotation;
        public Vector3 referencePosition;
        public Vector3 internalPosition;

        public void AlignIVA(bool autoAlign)
        {
            alignWithExternalMesh = autoAlign;

            if (alignWithExternalMesh)
            {
                ivaTransform = this.part.internalModel.FindModelTransform(ivaTransformName);
                externalTransform = this.part.FindModelTransform(externalTransformName);
                if (ivaTransform == null || externalTransform == null)
                    return;

                Quaternion inverseRotation = conjugate(part.transform.rotation);

                referenceRotation = inverseRotation * externalTransform.rotation;
                referencePosition = inverseRotation * (part.transform.position - externalTransform.position);
                internalRotation = ivaTransform.rotation;
                internalPosition = ivaTransform.position;

                updatePositionRotation();
            }
        }

        public void FixedUpdate()
        {
            if (alignWithExternalMesh == false)
                return;
            if (ivaTransform == null || externalTransform == null)
                return;

            updatePositionRotation();
        }

        protected void updatePositionRotation()
        {
            Quaternion inverseRotation = conjugate(part.transform.rotation);

            //=============================================
            // Update the orientation of the internal model
            //=============================================
            Quaternion currentRotation = inverseRotation * externalTransform.rotation;
            Quaternion rotationDelta = conjugate(currentRotation) * referenceRotation;

            //Swap x and y (bad IVA voodoo) this also adjusts the chirality
            //Use x = -y to rotate proper direction.
//            float tmpY = rotationDelta.y;
//            rotationDelta.y = rotationDelta.z;
//            rotationDelta.z = tmpY;
            float tmpY = rotationDelta.y;
            rotationDelta.y = rotationDelta.x;
            rotationDelta.x = -tmpY;

            ivaTransform.rotation = internalRotation * rotationDelta;

            //==========================================
            // Update the position of the internal model
            //==========================================
            Vector3 currentPosition = inverseRotation * (part.transform.position - externalTransform.position);
            Vector3 positionDelta = referencePosition - currentPosition;
            ivaTransform.position = internalPosition + positionDelta;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            ivaTransform = this.part.internalModel.FindModelTransform(ivaTransformName);
            externalTransform = this.part.FindModelTransform(externalTransformName);
            if (ivaTransform == null || externalTransform == null)
                return;

            Quaternion inverseRotation = conjugate(part.transform.rotation);

            referenceRotation = inverseRotation * externalTransform.rotation;
            referencePosition = inverseRotation * (part.transform.position - externalTransform.position);
            internalRotation = ivaTransform.rotation;
            internalPosition = ivaTransform.position;
            updatePositionRotation();
        }

        /// <summary>
        /// Create the conjugate of the quaternion (reverse rotation)
        /// </summary>
        /// <param name="quat">The quaternion to conjugate</param>
        /// <returns>The conjugated quaternion</returns>
        private Quaternion conjugate(Quaternion quat)
        {
            Quaternion result = quat;
            result.x = -result.x;
            result.y = -result.y;
            result.z = -result.z;
            return result;
        }
    }
}
