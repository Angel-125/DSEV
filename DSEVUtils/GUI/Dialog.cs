using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Dialogs;

namespace WildBlueIndustries
{
    public interface IManagedWindow
    {
        bool IsVisible();
        void DrawWindow();
    }

    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance;
        private readonly List<IManagedWindow> managedWindows = new List<IManagedWindow>();

        public void Awake()
        {
            Instance = this;
        }

        public void OnGUI()
        {
            for (int index = 0; index < managedWindows.Count; index++)
            {
                if (managedWindows[index].IsVisible())
                    managedWindows[index].DrawWindow();
            }
        }

        public void RegisterWindow(IManagedWindow managedWindow)
        {
            if (!managedWindows.Contains(managedWindow))
                managedWindows.Add(managedWindow);
        }

        public void UnregisterWindow(IManagedWindow managedWindow)
        {
            managedWindows.Remove(managedWindow);
        }
    }

    public abstract class Dialog<T> : IManagedWindow
    {
        private readonly int windowId;
        private readonly string inputLockName;
        private readonly string editorLockName;
        protected bool visible;

        public Rect windowPos;
        public string WindowTitle;
        public bool Resizable { get; set; }
        public bool HideCloseButton { get; set; }

        protected Dialog(string windowTitle, float defaultWidth, float defaultHeight)
        {
            WindowTitle = windowTitle;
            windowId = windowTitle.GetHashCode() + new System.Random().Next(65536) +
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.GetHashCode();
            inputLockName = "DSEVWindowLock" + windowId;
            editorLockName = "DSEVEditorLock" + windowId;
            windowPos = new Rect((Screen.width - defaultWidth) / 2,
                (Screen.height - defaultHeight) / 2, defaultWidth, defaultHeight);
            Resizable = true;
        }

        public bool IsVisible()
        {
            return visible;
        }

        public virtual void SetVisible(bool newValue)
        {
            visible = newValue;

            if (newValue)
            {
                if (DialogManager.Instance != null)
                    DialogManager.Instance.RegisterWindow(this);
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, inputLockName);
                if (HighLogic.LoadedSceneIsEditor)
                {
                    EditorLogic.fetch.Lock(true, true, true, editorLockName);
                    InputLockManager.SetControlLock(ControlTypes.EDITOR_ICON_HOVER |
                        ControlTypes.EDITOR_ICON_PICK |
                        ControlTypes.EDITOR_TAB_SWITCH |
                        ControlTypes.EDITOR_PAD_PICK_PLACE |
                        ControlTypes.EDITOR_PAD_PICK_COPY |
                        ControlTypes.EDITOR_GIZMO_TOOLS |
                        ControlTypes.EDITOR_ROOT_REFLOW |
                        ControlTypes.EDITOR_SYM_SNAP_UI |
                        ControlTypes.EDITOR_UNDO_REDO, editorLockName);
                }
            }
            else
            {
                if (DialogManager.Instance != null)
                    DialogManager.Instance.UnregisterWindow(this);
                InputLockManager.RemoveControlLock(inputLockName);
                InputLockManager.RemoveControlLock(editorLockName);
            }
        }

        public void DrawWindow()
        {
            if (!visible)
                return;

            bool paused = false;
            if (HighLogic.LoadedSceneIsFlight)
            {
                try
                {
                    paused = PauseMenu.isOpen || FlightResultsDialog.isDisplaying;
                }
                catch (Exception)
                {
                }
            }

            if (paused)
                return;

            GUI.skin = HighLogic.Skin;
            windowPos.x = Mathf.Clamp(windowPos.x, 16 - windowPos.width, Screen.width - 16);
            windowPos.y = Mathf.Clamp(windowPos.y, 16 - windowPos.height, Screen.height - 16);
            windowPos = GUILayout.Window(windowId, windowPos, drawWindow,
                WindowTitle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (!visible)
                return;

            Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (windowPos.Contains(mousePosition))
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, inputLockName);
            else
                InputLockManager.RemoveControlLock(inputLockName);
        }

        private void drawWindow(int id)
        {
            DrawWindowContents(id);

            if (!HideCloseButton && GUI.Button(new Rect(windowPos.width - 24, 4, 20, 20), "X"))
                SetVisible(false);

            GUI.DragWindow();
        }

        protected abstract void DrawWindowContents(int windowId);
    }
}
