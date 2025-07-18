using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        public virtual UIScreenType ScreenType => UIScreenType.Popup;
        
        [SerializeField] protected Canvas _canvas;
        [SerializeField] protected CanvasGroup _canvasGroup;
        
        [SerializeField] protected RectTransform _safeAreaContainer;
        [SerializeField] protected RectTransform _popupContainer;
        
        public Action OnClose;
        
        public TimeSpan TransitionOnTime { get; set; } = TimeSpan.Zero;
        public TimeSpan TransitionOffTime { get; set; } = TimeSpan.Zero;
        public float TransitionPosition { get; protected set; } = 0;
        public float TransitionAlpha => 1.0f - TransitionPosition;
        public UITransitionState TransitionState { get; protected set; } = UITransitionState.TransitionOn;
        public bool IsExiting { get; protected internal set; } = false;
        public bool IsActive => !_otherViewHasFocus && (TransitionState == UITransitionState.TransitionOn || TransitionState == UITransitionState.Active);
        public bool DestroyOnExit { get; protected internal set; } = false;
        public bool IsBeingSwapped { get; set; } = false;
        public bool IgnoreManager { get; protected internal set; } = false;

        public event Action<UITransitionState> OnTransitionStateChanged;
        protected bool _otherViewHasFocus = false;
        
        public UIScreen ParentScreen { get; set; }

        public int SortingOrder
        {
            get => _canvas.sortingOrder;
            set => _canvas.sortingOrder = value;
        }
        
        public virtual void Init(ScreenParameters screenParameters)
        {
            Init();
        }
        
        public virtual void Init()
        {
            if (_safeAreaContainer != null)
            {
                UIScreenManager.Instance.SafeArea.ApplySafeArea(ref _safeAreaContainer);
            }
        }

        public virtual void UpdateScreen()
        {
        }

        protected virtual bool IsBusy()
        {
            return false;
        }
        
        public virtual void UpdateState(bool otherViewHasFocus, bool coveredByOtherView)
        {
            if (IsBusy())
            {
                return;
            }
            
            _otherViewHasFocus = otherViewHasFocus;

            if (IsExiting)
            {
                TransitionState = UITransitionState.TransitionOff;
                TransitionStateChanged(TransitionState);
                if (!UpdateTransition(TransitionOffTime, -1))
                {
                    ChangeTransitionState(UITransitionState.Inactive);
                }
            }
            else if (coveredByOtherView)
            {
                if (UpdateTransition(TransitionOffTime, -1))
                {
                    ChangeTransitionState(UITransitionState.TransitionOff);
                }
                else
                {
                    ChangeTransitionState(UITransitionState.Hidden);
                }
            }
            else
            {
                if (UpdateTransition(TransitionOnTime, 1))
                {
                    ChangeTransitionState(UITransitionState.TransitionOn);
                }
                else
                {
                    ChangeTransitionState(UITransitionState.Active);
                }
            }
        }

        private bool UpdateTransition(TimeSpan time, int direction)
        {
            float transitionDelta;

            if (time == TimeSpan.Zero)
            {
                transitionDelta = 1;
            }
            else
            {
                transitionDelta = Time.unscaledDeltaTime * 1000.0f / (float)time.TotalMilliseconds;
            }

            TransitionPosition += transitionDelta * direction;

            if ((direction < 0 && TransitionPosition <= 0) || (direction > 0 && TransitionPosition >= 1))
            {
                TransitionPosition = Mathf.Clamp(TransitionPosition, 0, 1);
                return false;
            }

            return true;
        }

        private void ChangeTransitionState(UITransitionState state)
        {
            if (state == TransitionState)
            {
                return;
            }

            TransitionState = state;
            TransitionStateChanged(state);
            OnTransitionStateChanged?.Invoke(state);

            if (TransitionState == UITransitionState.Inactive)
            {
                UIScreenManager.Instance.HideScreen(this);
            }
        }

        protected virtual void TransitionStateChanged(UITransitionState state)
        {
        }

        public void ExitScreen()
        {
            if (TransitionOffTime == TimeSpan.Zero)
            {
                UIScreenManager.Instance.HideScreen(this);
            }
            else
            {
                IsExiting = true;
            }
        }
        
        public void ShowScreenForced()
        {
            UIScreenManager.Instance.ShowScreenForced(this);
        }

        public static float Lerp(float a, float b, float t)
        {
            return (1f - t) * a + t * b;
        }

        public static float InverseLerp(float a, float b, float value)
        {
            return (value - a) / (b - a);
        }

        public static float Remap(float iMin, float iMax, float oMin, float oMax, float value)
        {
            return Lerp(oMin, oMax, InverseLerp(iMin, iMax, value));
        }
        
#if UNITY_EDITOR
        [CustomEditor(typeof(UIScreen), true)]
        public class UIScreenEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var uiScreen = (UIScreen)target;
            
                if (GUILayout.Button("AutoFill inspector"))
                {
                    uiScreen._canvas = GetOrAddComponent<Canvas>(uiScreen);
                    uiScreen._canvasGroup = GetOrAddComponent<CanvasGroup>(uiScreen);
                    uiScreen._canvas.overrideSorting = true;
                    uiScreen._canvas.sortingOrder = 1;
                    GetOrAddComponent<GraphicRaycaster>(uiScreen);

                    
                    foreach (Transform child in uiScreen.transform)
                    {
                        if (child.name == "SafeArea")
                        {
                            uiScreen._safeAreaContainer = child.gameObject.GetComponent<RectTransform>();
                            break;
                        }
                    }

                    if (uiScreen._safeAreaContainer != null)
                    {
                        foreach (Transform child in uiScreen._safeAreaContainer.transform)
                        {
                            if (child.name == "Container")
                            {
                                uiScreen._popupContainer = child.gameObject.GetComponent<RectTransform>();
                                break;
                            }
                        }
                    }
                    
                    if(uiScreen._popupContainer == null)
                    {
                        uiScreen._popupContainer = uiScreen.GetComponent<RectTransform>();
                    }
                    EditorUtility.SetDirty(target);
                }
            }

            private T GetOrAddComponent<T>(UIScreen uiScreen) where T : Component
            {
                var component = uiScreen.GetComponent<T>();
                if (component == null)
                {
                    component = uiScreen.gameObject.AddComponent<T>();
                }
                return component;
            }
        }
#endif
    }
}