using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;

namespace Game.UI
{
    public class UIScreenManager : MonoBehaviour
    {
        public static UIScreenManager Instance { get; private set; }

        [SerializeField] private Transform _container;
        
        public Canvas canvas;
        public CanvasScaler canvasScaler;
        public UISafeArea SafeArea { get; private set; }

        private List<UIScreen> _screens = new();
        private List<UIScreen> _inactiveScreens = new();
        private List<UIScreen> _screensToDisable = new();
        
        private readonly List<(Type, Action<UIScreen>, Func<bool>, ScreenParameters screenParameters)> _waitingQueue = new();

        private Camera _uiCamera;
        private RectTransform _canvasRect;

        private int _lastSortingOrder;

        private const int SortingOrderOffset = 40;
        private const int SortingOrderOffsetForOverlays = SortingOrderOffset;
        private const int SortingOrderOffsetForFullScreens = SortingOrderOffsetForOverlays + SortingOrderOffset;
        private const int SortingOrderOffsetForBackgrounds = SortingOrderOffsetForFullScreens + SortingOrderOffset;
        public const int SortingOrderOffsetForPopups = SortingOrderOffsetForBackgrounds + SortingOrderOffset;
        private const int SortingOrderOffsetForBusy = SortingOrderOffsetForPopups + SortingOrderOffset;
        private const int SortingOrderOffsetForTopPriority = SortingOrderOffsetForBusy + SortingOrderOffset;
        private readonly ScreenParameters _screenParametersDefault = new();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            
            _uiCamera = Camera.main;
            _canvasRect = canvas.GetComponent<RectTransform>();

            SafeArea = new UISafeArea();
            SafeArea.Calculate(canvasScaler);
        }
        
        private void OnSceneChangeStarted(string _, string _2)
        {
            for (var i = _screens.Count - 1; i >= 0; i--)
            {
                _screensToDisable.Add(_screens[i]);
            }
        }
        
        private void OnSceneChangeEnded(string _)
        {
            for (var i = _screensToDisable.Count - 1; i >= 0; i--)
            {
                HideScreen(_screensToDisable[i], false);
            }
            _screensToDisable.Clear();
        }

#if UNITY_EDITOR
        private string _strPrevious;
        private void LateUpdateDebug()
        {
            var str = $"<color=#00FF00>[Active]</color> ";
            var activeStr = _screens.Aggregate("", (current, screen) => current + (GetScreenInfo(screen) + ", "));
            str += _screens.Count > 0 ? activeStr.TrimEnd(',', ' ') : "None";

            str += "\n<color=#FF0000>[Inactive]</color> ";

            var inactiveStr = _inactiveScreens.Aggregate("", (current, screen) => current + (GetScreenInfo(screen) + ", "));
            str += _inactiveScreens.Count > 0 ? inactiveStr.TrimEnd(',', ' ') : "None";

            str += "\n<color=#0000FF>[WaitingQueue]</color> ";
            
            var waitingQueueStr = _waitingQueue.Aggregate("", (current, screen) => current + (screen.Item1 + ", "));
            str += _waitingQueue.Count > 0 ? waitingQueueStr.TrimEnd(',', ' ') : "None";
            
            if (str != _strPrevious)
            {
                Debug.LogError(str);
                _strPrevious = str;
            }
        }
        
        private string GetScreenInfo(UIScreen screen)
        {
            var color = "";
            switch (screen.ScreenType)
            {
                case UIScreenType.Popup:
                    color = "#d614e6"; // Pink
                    break;
                
                case UIScreenType.FullScreen:
                    color = "#14b0e6"; // Cyan
                    break;
                
                case UIScreenType.Background:
                    color = "#1474e6"; // Blue
                    break;
                
                case UIScreenType.Overlay:
                    color = "#e3e614";  // Yellow
                    break;
                
                default:
                    color = "#FFFFFF";  // White
                    break;
            }
            
            return $"<color={color}>[{screen.ScreenType} {screen.SortingOrder}] {screen.GetType().Name}</color>";
        }
#endif

        private void InvokeScreenFromWaitingQueue()
        {
            if (_waitingQueue.Count <= 0)
            {
                return;
            }

            if (_screens.Any(s => s.ScreenType is UIScreenType.Popup or UIScreenType.FullScreen))
            {
                return;
            }
            
            for (var i = 0; i < _waitingQueue.Count; i++)
            {
                var (screenType, callback, condition, screenParameters) = _waitingQueue[i];

                if (condition == null || condition.Invoke())
                {
                    _waitingQueue.RemoveAt(0);
            
                    var screen = ShowScreenForced(screenType);
                    callback?.Invoke(screen);
                    break;
                }
            }
        }

        public bool IsScreenActive<T>() where T : UIScreen
        {
            return _screens.Any(s => s.GetType() == typeof(T));
        }
        
        public void HideScreen<T>(bool isInvokingScreenFromWaitingQueue = true) where T : UIScreen
        {
            if (!TryGetExistingScreen<T>(out var screen))
            {
                Debug.LogError($"[{nameof(UIScreenManager)}] {nameof(HideScreen)} Couldn't find {screen.GetType().Name}");
            }

            HideScreen(screen, isInvokingScreenFromWaitingQueue);
        }

        public void HideScreen(UIScreen screen, bool isInvokingScreenFromWaitingQueue = true)
        {
            DisableScreen(screen);
            
            if (isInvokingScreenFromWaitingQueue)
            {
                _timeLeftForQueueCheck = 0;
            }
        }

        private void DisableScreen(UIScreen screen)
        {
            if (screen == null)
            {
                return;
            }
            
            if (!screen.IsBeingSwapped && screen.DestroyOnExit)
            {
                if (_screens.Contains(screen))
                {
                    _screens.Remove(screen);
                }
                if (_inactiveScreens.Contains(screen))
                {
                    _inactiveScreens.Remove(screen);
                }
                
                Destroy(screen.gameObject);
            }
            else
            {
                DeactivateScreen(screen);
            }
        }

        public void ShowScreenQueued<T>(Action<UIScreen> waitingCallback, Func<bool> condition, ScreenParameters screenParameters = null) where T : UIScreen
        {
            if (_screens.Any(s => s.ScreenType is UIScreenType.Popup or UIScreenType.FullScreen))
            {
                _waitingQueue.Add((typeof(T), waitingCallback, condition, screenParameters));
            }
            else
            {
                var screen = ShowScreenForced<T>(screenParameters);
                waitingCallback?.Invoke(screen);
            }
        }

        public T ShowScreenForced<T>(ScreenParameters screenParameters = null) where T : UIScreen
        {
            return (T)ShowScreenForced(typeof(T), screenParameters);
        }

        public UIScreen ShowScreenForced(Type screenType, ScreenParameters screenParameters = null)
        {
            var screen = GetOrCreateScreen(screenType);
            return ShowScreenForced(screen, screenParameters);
        }
        
        public UIScreen ShowScreenForced(UIScreen screen, ScreenParameters screenParameters = null, bool init = true)
        {
            if (init)
            {
                screen.ParentScreen = GetActiveScreen();
            }
          
            if (init)
            {
                InitScreen(screen, screenParameters);
            }
            screenParameters ??= _screenParametersDefault; 

            switch (screen.ScreenType)
            {
                case UIScreenType.Background:
                    screen.SortingOrder = _lastSortingOrder++ + SortingOrderOffsetForBackgrounds;
                    break;
                
                case UIScreenType.Overlay:
                    screen.SortingOrder = _lastSortingOrder++ + SortingOrderOffsetForOverlays;
                    
                    for (var i = _screens.Count - 1; i >= 0; i--)
                    {
                        var screenToDisable = _screens[i];
                        if (screenToDisable != screen && screenToDisable.ScreenType is UIScreenType.Overlay)
                        {
                            HideScreen(screenToDisable, false);
                        }
                    } 
                    break;
                
                case UIScreenType.Popup:
                    screen.SortingOrder = _lastSortingOrder++ + SortingOrderOffsetForPopups;

                    if (screenParameters.ClosePreviousPopup)
                    {
                        for (var i = _screens.Count - 1; i >= 0; i--)
                        {
                            var screenToDisable = _screens[i];
                            if (screenToDisable != screen && screenToDisable.ScreenType is UIScreenType.Popup)
                            {
                                HideScreen(screenToDisable, false);
                            }
                        }  
                    }
                    break;
                
                case UIScreenType.FullScreen:
                    screen.SortingOrder = _lastSortingOrder++ + SortingOrderOffsetForFullScreens;
                    
                    for (var i = _screens.Count - 1; i >= 0; i--)
                    {
                        var screenToDisable = _screens[i];
                        if (screenToDisable != screen)
                        {
                            HideScreen(screenToDisable, false);
                        }
                    }   
                    break;

                case UIScreenType.Busy:
                default:
                    screen.SortingOrder = _lastSortingOrder++ + SortingOrderOffsetForBusy;
                    break;
            }

            if (screenParameters.Enable)
            {
                ActivateScreen(screen);
            }
            else
            {
                DeactivateScreen(screen);
            }
            
            return screen;
        }

        public T ShowScreenTopPriority<T>(ScreenParameters screenParameters = null) where T : UIScreen
        {
            var screen = GetOrCreateScreen<T>();
            
            InitScreen(screen, screenParameters);
            
            screen.SortingOrder = _lastSortingOrder++ + SortingOrderOffsetForTopPriority;
            
            ActivateScreen(screen);
            
            return screen;
        }

        public T GetOrCreateScreen<T>() where T : UIScreen
        {
            return (T)GetOrCreateScreen(typeof(T));
        }

        private UIScreen GetOrCreateScreen(Type screenType)
        {
            if (TryGetExistingScreen(screenType, out var screen))
            {
                return screen;
            }

            var screenScriptName = screenType.Name;
            var resourcePath = $"UI/Screens/{(screenScriptName.StartsWith("UI") ? screenScriptName.Substring("UI".Length) : screenScriptName)}";
            screen = InstantiateScreen(resourcePath);
            DeactivateScreen(screen);

            return screen;
        }
        
        public bool TryGetExistingScreen<T>(out T screen) where T : UIScreen
        {
            if (TryGetExistingScreen(typeof(T), out var uiScreen))
            {
                screen = (T)uiScreen;
                return true;
            }

            screen = default;
            return false;
        }
        
        public bool TryGetExistingScreen(Type screenType, out UIScreen screen)
        {
            screen = _screens.Find(s => s.GetType() == screenType);
            if (screen != null)
            {
                return true;
            }

            screen = _inactiveScreens.Find(s => s.GetType() == screenType);
            return screen != null;
        }

        private UIScreen InstantiateScreen(string resourcePath)
        {
            var screenResource = Resources.Load<UIScreen>(resourcePath);
            if (screenResource == null)
            {
                Debug.LogError($"[{nameof(UIScreenManager)}] {nameof(InstantiateScreen)} Couldn't find {resourcePath}");
            }
            return Instantiate(screenResource, _container, false);
        }

        private void InitScreen(UIScreen screen, ScreenParameters screenParameters)
        {
            screen.transform.localScale = Vector3.one;
            screen.Init(screenParameters);
        }

        private void ActivateScreen(UIScreen screen)
        {
#if UNITY_EDITOR
            screen.name = $"[Active {screen.SortingOrder}] {screen.GetType().Name}";
#endif
            screen.IsExiting = false;
            screen.gameObject.SetActive(true);
            
            if (_inactiveScreens.Contains(screen))
            {
                _inactiveScreens.Remove(screen);
            }
            if (!_screens.Contains(screen))
            {
                _screens.Add(screen);
            }
            _screens = _screens.OrderBy(s => s.SortingOrder).ToList();
        }
        
        private void DeactivateScreen(UIScreen screen)
        {
#if UNITY_EDITOR
            screen.name = $"[Inactive {screen.SortingOrder}] {screen.GetType().Name}";
#endif
            screen.IsBeingSwapped = false;
            screen.gameObject.SetActive(false);
            
            if (!_inactiveScreens.Contains(screen))
            {
                _inactiveScreens.Add(screen);
            }

            if (_screens.Contains(screen))
            {
                _screens.Remove(screen);
            }
        }

        private void Update()
        {
            var otherScreenHasFocus = false;
            var coveredByOtherScreen = false;

            var hasScreenOver = false;
            for (var i = _screens.Count - 1; i >= 0; i--)
            {
                var screen = _screens[i];
                screen.UpdateState(otherScreenHasFocus, coveredByOtherScreen);

                if (screen.TransitionState is UITransitionState.TransitionOn or UITransitionState.Active or UITransitionState.TransitionOff)
                {
                    if (!otherScreenHasFocus)
                    {
                        screen.UpdateScreen();

                        otherScreenHasFocus = true;
                    }

                    if (screen.ScreenType is UIScreenType.Popup or UIScreenType.FullScreen)
                    {
                        if (hasScreenOver)
                        {
                            coveredByOtherScreen = true;
                        }
                        
                        hasScreenOver = true;
                    }
                }
            }
        }

        private const float TimeBetweenQueueCheck = 30f;
        private float _timeLeftForQueueCheck = TimeBetweenQueueCheck * 3;
        
        
        private void LateUpdate()
        {
            _timeLeftForQueueCheck -= Time.deltaTime;
            if (_timeLeftForQueueCheck <= 0)
            {
                InvokeScreenFromWaitingQueue();
                
                _timeLeftForQueueCheck = TimeBetweenQueueCheck;
            }

#if UNITY_EDITOR
            LateUpdateDebug();
#endif
        }

        public Vector2 WorldToCanvasPosition(Vector3 worldPosition, Vector3 anchoredPosition)
        {
            if (_uiCamera == null)
            {
                _uiCamera = Camera.main;
            }
            
            var idealViewportPosition = _uiCamera.WorldToViewportPoint(worldPosition);
            var actualViewportPosition = idealViewportPosition - anchoredPosition;
            var scale = _canvasRect.sizeDelta;

            return Vector3.Scale(actualViewportPosition, scale);
        }

        public Vector2 GetPositionRelativeTo(RectTransform childRectTransform, RectTransform targetRectTransform)
        {
            var childPositionInWorldSpace = childRectTransform.parent.TransformPoint(childRectTransform.localPosition);
            var childPositionInTargetSpace = targetRectTransform.parent.InverseTransformPoint(childPositionInWorldSpace);
            var adjustedPosition = new Vector2(childPositionInTargetSpace.x - targetRectTransform.rect.width * (targetRectTransform.pivot.x - 0.5f),
                childPositionInTargetSpace.y - targetRectTransform.rect.height * (targetRectTransform.pivot.y - 0.5f));

            return adjustedPosition;
        }

        public UIScreen GetActiveScreen()
        {
            return _screens.Count > 0 ? _screens[^1] : null;
        }
        
        public void RestorePreviousScreen(UIScreen currentScreen)
        {
            var screenToRestore = currentScreen?.ParentScreen;
            if (screenToRestore != null)
            {
                HideScreen(currentScreen);
                ShowScreenForced(screenToRestore, null, false);
            }
        }
    }
}

public class ScreenParameters
{
    public bool Enable = true;
    public bool ClosePreviousPopup = true;
}