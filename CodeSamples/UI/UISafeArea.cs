using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class UISafeArea
    {
        public float TopOffset { get; private set; }
        public float BottomOffset { get; private set; }

        private CanvasScaler _canvasScaler;

        public void Calculate(CanvasScaler canvasScaler)
        {
            _canvasScaler = canvasScaler;
            var safeArea = Screen.safeArea;
            var resolution = Screen.currentResolution;

            var topPixel = resolution.height - (safeArea.y + safeArea.height);
            var bottomRatio = safeArea.y / resolution.height;
            var topRatio = topPixel / resolution.height;
            var referenceResolution = _canvasScaler.referenceResolution;

            TopOffset = referenceResolution.y * topRatio;
            BottomOffset = referenceResolution.y * bottomRatio;
        }

        public void ApplySafeArea(ref RectTransform target, bool ignoreBottom = false)
        {
            var safeArea = Screen.safeArea;
            var resolution = Screen.currentResolution;

            var topPixel = resolution.height - (safeArea.y + safeArea.height);
            var bottomRatio = safeArea.y / resolution.height;
            var topRatio = topPixel / resolution.height;
            var referenceResolution = _canvasScaler.referenceResolution;

            if(!ignoreBottom)
            {
                target.offsetMin = new Vector2(target.offsetMin.x, referenceResolution.y * bottomRatio);
            }
            target.offsetMax = new Vector2(target.offsetMax.x, -(referenceResolution.y * topRatio));
        }
    }
}