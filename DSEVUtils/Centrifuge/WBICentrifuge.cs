/*
Source code copyright 2016, by Michael Billard (Angel-125)
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
    public enum CentrifugeStates
    {
        Stopped,
        SpinningUp,
        Spinning,
        SpinningDown,
        Moving,
        MovingArms
    }

    public class WBICentrifuge : PartModule
    {
        [KSPField]
        public string centrifugeName = string.Empty;

        [KSPField]
        public string counterTorqueName = string.Empty;

        [KSPField]
        public string rotationAxis = string.Empty;

        [KSPField]
        public float rotationsPerMinute = 1.0f;

        [KSPField]
        public float centrifugeAcceleration = 1.0f; //In degrees per second, how fast to reach rotations per minute.

        [KSPField]
        public float counterTorqueSpeedMultiplier = 4.0f; //How fast to spin the counter-torque ring in relation to the centrifuge.

        [KSPField]
        public float armRadius = 1.0f;

        //[KSPField(guiActive = true, guiName = "Gravity", guiUnits = "g", guiFormat = "f2")]
        public float gForce = 0.0f;

        [KSPField(guiActive = true, guiName = "Centrifuge")]
        public string status;

        [KSPField]
        public string startCentrifugeName = "Start Centrifuge";

        [KSPField]
        public string stopCentrifugeName = "Stop Centrifuge";

        [KSPField]
        public string centrifugeActionName = "Start/Stop Centrifuge";

        [KSPField]
        public string inputResource = string.Empty;

        [KSPField]
        public float inputAmount = 0.0f;

        [KSPField(isPersistant = true)]
        public bool isRotating = false;

        [KSPField(isPersistant = true)]
        public float currentDegPerSec = 0.0f;

        [KSPField(isPersistant = true)]
        public float currentDegPerSecCounter = 0.0f;

        protected Transform centrifugeTransform;
        protected Transform counterTorqueTransform;
        protected Vector3 rotationAxisVec;
        protected CentrifugeStates centrifugeState;
        protected float maxDegPerSec;
        protected float maxDegPerSecCounter;
        protected ModuleAnimateGenericSFX animation;
        protected WBIIVARotationHelper[] rotationHelpers;
        protected Quaternion orginalExternalRotation;
        protected Quaternion originalInternalRotation;

        [KSPEvent(guiActive = true)]
        public void ToggleArms()
        {
            centrifugeTransform.rotation = orginalExternalRotation;
            this.part.internalModel.transform.rotation = originalInternalRotation;

            if (animation != null)
                animation.Toggle();
            centrifugeState = CentrifugeStates.MovingArms;
            isRotating = true;
            Events["ToggleArms"].guiName = animation.Events["Toggle"].guiName;
            Events["ToggleArms"].guiActive = false;
            Events["ToggleCentrifuge"].guiActive = false;
            updateRotationHelpers(true);
        }

        [KSPEvent(guiActive = true, guiName = "Start Centrifuge")]
        public void ToggleCentrifuge()
        {
            //Update State
            switch (centrifugeState)
            {
                case CentrifugeStates.Stopped:
                    if (animation != null && animation.Events["Toggle"].guiName == animation.startEventGUIName)
                    {
                        animation.Toggle();
                        centrifugeState = CentrifugeStates.Moving;
                        isRotating = true;
                        Events["ToggleArms"].guiActive = false;
                        Events["ToggleArms"].guiName = animation.Events["Toggle"].guiName;
                        Events["ToggleCentrifuge"].guiActive = false;
                        Events["ToggleCentrifuge"].guiName = stopCentrifugeName;
                        updateRotationHelpers(true);
                        break;
                    }
                    centrifugeState = CentrifugeStates.SpinningUp;
                    currentDegPerSec = 0.0f;
                    currentDegPerSecCounter = 0.0f;
                    isRotating = true;
                    Events["ToggleCentrifuge"].guiName = stopCentrifugeName;
                    Events["ToggleArms"].guiActive = false;
                    Events["ToggleArms"].guiName = animation.Events["Toggle"].guiName;
                    break;

                case CentrifugeStates.SpinningUp:
                case CentrifugeStates.Spinning:
                    centrifugeState = CentrifugeStates.SpinningDown;
                    break;

                default:
                    centrifugeState = CentrifugeStates.SpinningDown;
                    break;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            switch (centrifugeState)
            {
                case CentrifugeStates.Stopped:
                    status = "Stopped";
                    break;

                case CentrifugeStates.SpinningUp:
                    status = "Speeding Up";
                    break;

                case CentrifugeStates.SpinningDown:
                    status = "Slowing Down";
                    break;

                case CentrifugeStates.Spinning:
                    status = "Spinning";
                    break;

                case CentrifugeStates.Moving:
                case CentrifugeStates.MovingArms:
                    status = "Moving...";
                    break;

                default:
                    status = "Stopped";
                    break;
            }
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (isRotating == false)
                return;

            //Update the current state
            if (updateState() == false)
                return;

            //Get rotations per frame
            float rotationsPerFrame = currentDegPerSec * TimeWarp.fixedDeltaTime;
            float counterRotationsPerFrame = currentDegPerSecCounter * TimeWarp.fixedDeltaTime * -1.0f;

            //Rotate centrifuge & counter-torque
            centrifugeTransform.Rotate(rotationAxisVec, rotationsPerFrame);
            if (counterTorqueTransform != null)
                counterTorqueTransform.Rotate(rotationAxisVec, counterRotationsPerFrame);

            //Rotate the IVA
            this.part.internalModel.transform.Rotate(rotationAxisVec, -rotationsPerFrame);

            //Calculate G
            calculateGravity();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Deploy/Stow animation
            animation = this.part.FindModuleImplementing<ModuleAnimateGenericSFX>();
            if (animation != null)
            {
            }

            //Get the rotation transforms
            if (string.IsNullOrEmpty(centrifugeName) || string.IsNullOrEmpty(counterTorqueName))
                return;
            centrifugeTransform = this.part.FindModelTransform(centrifugeName);
            counterTorqueTransform = this.part.FindModelTransform(counterTorqueName);
            orginalExternalRotation = centrifugeTransform.rotation;
            originalInternalRotation = this.part.internalModel.transform.rotation;

            //Get the rotation vector
            if (string.IsNullOrEmpty(rotationAxis) == false)
            {
                try
                {
                    string[] axisValues = rotationAxis.Split(new char[] { ',' });
                    rotationAxisVec = new Vector3(float.Parse(axisValues[0]), float.Parse(axisValues[1]), float.Parse(axisValues[2]));
                }
                catch
                {
                    rotationAxisVec = new Vector3(0, 0, 1.0f);
                }
            }

            else
            {
                rotationAxisVec = new Vector3(0, 0, 1.0f);
            }

            //Get rotation helpers
            List<WBIIVARotationHelper> helpers = this.part.FindModulesImplementing<WBIIVARotationHelper>();
            if (helpers.Count > 0)
                rotationHelpers = helpers.ToArray();

            //Calculate max degrees per second
            maxDegPerSec = rotationsPerMinute * 6.0f;
            maxDegPerSecCounter = maxDegPerSec * counterTorqueSpeedMultiplier;
            
            //Set state
            if (isRotating)
                centrifugeState = CentrifugeStates.Spinning;

            //Update GUI
            if (isRotating)
                Events["ToggleCentrifuge"].guiName = stopCentrifugeName;
            else
                Events["ToggleCentrifuge"].guiName = startCentrifugeName;
            Events["ToggleArms"].guiName = animation.Events["Toggle"].guiName;
            if (centrifugeState == CentrifugeStates.Stopped)
                Events["ToggleArms"].guiActive = true;
        }

        protected void calculateGravity()
        {
            float twoPi = 3.14159f * 2.0f;
            float revolutionPeriod = 60.0f / rotationsPerMinute;
            float angularVelocity = twoPi / revolutionPeriod;
            float acceleration = armRadius * (angularVelocity * angularVelocity);

            gForce = acceleration / 9.81f;
        }

        protected void updateRotationHelpers(bool autoAlign)
        {
            if (rotationHelpers == null || rotationHelpers.Length == 0)
                return;

            for (int index = 0; index < rotationHelpers.Length; index++)
            {
                rotationHelpers[index].AlignIVA(autoAlign);
            }
        }

        protected bool updateState()
        {
            //If we are spinning up or spinning down, slerp to max degrees per second
            float accelerationPerFrame = centrifugeAcceleration * TimeWarp.fixedDeltaTime;
            switch (centrifugeState)
            {
                case CentrifugeStates.SpinningUp:
                    currentDegPerSec = Mathf.Lerp(currentDegPerSec, maxDegPerSec, accelerationPerFrame);
                    currentDegPerSecCounter = Mathf.Lerp(currentDegPerSecCounter, maxDegPerSecCounter, accelerationPerFrame * counterTorqueSpeedMultiplier);
                    if ((currentDegPerSec / maxDegPerSec) >= 0.99f)
                        centrifugeState = CentrifugeStates.Spinning;
                    Events["ToggleCentrifuge"].guiActive = true;
                    break;

                case CentrifugeStates.SpinningDown:
                    currentDegPerSec = Mathf.Lerp(currentDegPerSec, 0f, accelerationPerFrame);
                    currentDegPerSecCounter = Mathf.Lerp(currentDegPerSecCounter, 0f, accelerationPerFrame * counterTorqueSpeedMultiplier);
                    if ((currentDegPerSec / maxDegPerSec) <= 0.01f)
                        centrifugeState = CentrifugeStates.Stopped;
                    Events["ToggleCentrifuge"].guiActive = true;
                    break;

                case CentrifugeStates.Spinning:
                    currentDegPerSec = maxDegPerSec;
                    currentDegPerSecCounter = maxDegPerSecCounter;
                    break;

                case CentrifugeStates.Moving:
                    if (animation.aniState == ModuleAnimateGeneric.animationStates.LOCKED || animation.aniState == ModuleAnimateGeneric.animationStates.CLAMPED)
                    {
                        centrifugeState = CentrifugeStates.SpinningUp;
                        currentDegPerSec = 0.0f;
                        currentDegPerSecCounter = 0.0f;
                        Events["ToggleCentrifuge"].guiName = stopCentrifugeName;
                        updateRotationHelpers(false);
                    }
                    else
                    {
                        return false;
                    }
                    break;

                case CentrifugeStates.MovingArms:
                    if (animation.aniState == ModuleAnimateGeneric.animationStates.LOCKED || animation.aniState == ModuleAnimateGeneric.animationStates.CLAMPED)
                    {
                        centrifugeState = CentrifugeStates.Stopped;
                        updateRotationHelpers(false);
                    }
                    break;

                case CentrifugeStates.Stopped:
                    isRotating = false;
                    Events["ToggleCentrifuge"].guiActive = true;
                    Events["ToggleCentrifuge"].guiName = startCentrifugeName;
                    Events["ToggleArms"].guiActive = true;
                    return false;
            }

            if (isRotating && string.IsNullOrEmpty(inputResource) == false && (centrifugeState != CentrifugeStates.SpinningDown && centrifugeState != CentrifugeStates.Stopped))
            {
                double resourcePerTimeTick = inputAmount * TimeWarp.fixedDeltaTime;
                double amountObtained = this.part.RequestResource(inputResource, resourcePerTimeTick, ResourceFlowMode.ALL_VESSEL);

                if (amountObtained / resourcePerTimeTick < 0.999)
                {
                    this.part.RequestResource(inputResource, -amountObtained);
                    centrifugeState = CentrifugeStates.SpinningDown;
                    ScreenMessages.PostScreenMessage("Insufficient " + inputResource + " to power the centrifuge. Shutting down.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    Events["ToggleCentrifuge"].guiActive = false;
                }
            }
            return true;
        }
    }
}
