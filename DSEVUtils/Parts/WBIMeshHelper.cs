using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WildBlueIndustries
{
    public class WBIMeshHelper : PartModule
    {
        [KSPField]
        public string objects = string.Empty;

        [KSPField(isPersistant = true)]
        public int selectedObject;

        [KSPField]
        public string guiNames = string.Empty;

        [KSPField]
        public bool editorOnly;

        [KSPField]
        public bool showGui;

        [KSPField]
        public bool showPrev = true;

        protected readonly List<List<Transform>> objectTransforms = new List<List<Transform>>();
        protected readonly Dictionary<string, int> meshIndexes = new Dictionary<string, int>();
        protected readonly List<string> objectNames = new List<string>();

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next variant",
            active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public virtual void NextMesh()
        {
            if (objectNames.Count == 0)
                return;

            int nextIndex = (selectedObject + 1) % objectNames.Count;
            SetMeshIndex(nextIndex, nextIndex);
            updateSymmetry(nextIndex, nextIndex);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Prev variant",
            active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public virtual void PrevMesh()
        {
            if (objectNames.Count == 0)
                return;

            int nextIndex = (selectedObject - 1 + objectNames.Count) % objectNames.Count;
            SetMeshIndex(nextIndex, nextIndex);
            updateSymmetry(nextIndex, nextIndex);
        }

        public void SetMeshIndex(int index, int guiIndex)
        {
            setObject(index);
            if (guiIndex >= 0 && guiIndex < objectNames.Count)
                Events["NextMesh"].guiName = objectNames[guiIndex];
        }

        public override void OnAwake()
        {
            base.OnAwake();
            if (HighLogic.LoadedSceneIsEditor)
                part.OnEditorAttach += OnEditorAttach;

            if (editorOnly && !HighLogic.LoadedSceneIsEditor)
                showGui = false;

            setEventVisibility();
            if (objectTransforms.Count == 0)
            {
                parseObjectNames();
                setObject(selectedObject);
            }
            updateNextLabel();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            ConfigNode moduleNode = getModuleNode();
            if (moduleNode != null)
                getProtoNodeValues(moduleNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            parseObjectNames();
            setObject(selectedObject);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.SetValue("selectedObject", selectedObject.ToString(), true);
        }

        public virtual void OnEditorAttach()
        {
            setEventVisibility();
        }

        protected virtual void getProtoNodeValues(ConfigNode protoNode)
        {
            int parsedIndex;
            if (int.TryParse(protoNode.GetValue("selectedObject"), out parsedIndex))
                selectedObject = parsedIndex;

            objects = protoNode.GetValue("objects") ?? string.Empty;
            guiNames = protoNode.GetValue("guiNames") ?? string.Empty;
            parseObjectNames();
            setObject(selectedObject);
        }

        protected void setObject(int objectNumber, bool startHidden = true)
        {
            if (objectTransforms.Count == 0)
                return;

            if (startHidden)
            {
                foreach (List<Transform> transforms in objectTransforms)
                {
                    foreach (Transform transform in transforms)
                        setTransformActive(transform, false);
                }
            }

            if (objectNumber < 0 || objectNumber >= objectTransforms.Count)
                return;

            foreach (Transform transform in objectTransforms[objectNumber])
                setTransformActive(transform, true);

            selectedObject = objectNumber;
        }

        protected void setObjects(List<int> visibleObjects)
        {
            setObject(-1);
            foreach (int objectIndex in visibleObjects)
                setObject(objectIndex, false);
        }

        private void parseObjectNames()
        {
            objectTransforms.Clear();
            objectNames.Clear();
            meshIndexes.Clear();

            string[] objectBatches = (objects ?? string.Empty).Split(';');
            for (int batchIndex = 0; batchIndex < objectBatches.Length; batchIndex++)
            {
                if (string.IsNullOrEmpty(objectBatches[batchIndex]))
                    continue;

                List<Transform> transforms = new List<Transform>();
                string[] names = objectBatches[batchIndex].Split(',');
                foreach (string name in names)
                {
                    Transform transform = part.FindModelTransform(name.Trim());
                    if (transform != null)
                        transforms.Add(transform);
                }

                objectTransforms.Add(transforms);
                meshIndexes[objectBatches[batchIndex]] = objectTransforms.Count - 1;
            }

            if (!string.IsNullOrEmpty(guiNames))
                objectNames.AddRange(guiNames.Split(';'));
        }

        private ConfigNode getModuleNode()
        {
            if (part == null || part.partInfo == null || part.partInfo.partConfig == null)
                return null;

            ConfigNode[] moduleNodes = part.partInfo.partConfig.GetNodes("MODULE");
            return moduleNodes.FirstOrDefault(node => node.GetValue("name") == ClassName);
        }

        private void setEventVisibility()
        {
            Events["NextMesh"].active = showGui;
            Events["NextMesh"].guiActive = showGui;
            Events["NextMesh"].guiActiveEditor = showGui;
            Events["NextMesh"].guiActiveUnfocused = showGui;
            Events["PrevMesh"].active = showGui && showPrev;
            Events["PrevMesh"].guiActive = showGui && showPrev;
            Events["PrevMesh"].guiActiveEditor = showGui && showPrev;
            Events["PrevMesh"].guiActiveUnfocused = showGui && showPrev;
        }

        private void updateNextLabel()
        {
            if (objectNames.Count > 0)
                Events["NextMesh"].guiName = objectNames[(selectedObject + 1) % objectNames.Count];
        }

        private void updateSymmetry(int objectIndex, int guiIndex)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            foreach (Part symmetryPart in part.symmetryCounterparts)
            {
                WBIMeshHelper helper = symmetryPart.GetComponent<WBIMeshHelper>();
                if (helper != null)
                    helper.SetMeshIndex(objectIndex, guiIndex);
            }
        }

        private static void setTransformActive(Transform transform, bool isActive)
        {
            transform.gameObject.SetActive(isActive);
            Collider collider = transform.gameObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = isActive;
        }
    }
}
