using System;
using UnityEngine;
using UnityEngine.XR;

namespace OdisseiaVR.Tour
{
    /// <summary>
    /// Traduz os botões do controle direito em eventos de domínio:
    /// A (primary) => SkipRequested, B (secondary) => ExitRequested.
    /// O TourManager não sabe nada de XR.
    /// </summary>
    public sealed class XrTourInput : MonoBehaviour
    {
        public event Action SkipRequested;
        public event Action ExitRequested;

        private bool wasAPressed;
        private bool wasBPressed;

        private void Update()
        {
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightHand.isValid) return;

            Poll(rightHand, CommonUsages.primaryButton, ref wasAPressed, SkipRequested);
            Poll(rightHand, CommonUsages.secondaryButton, ref wasBPressed, ExitRequested);
        }

        // Dispara apenas na borda de subida (press), não enquanto o botão está segurado.
        private static void Poll(InputDevice device, InputFeatureUsage<bool> usage, ref bool wasPressed, Action onPressed)
        {
            if (!device.TryGetFeatureValue(usage, out bool isPressed)) return;

            if (isPressed && !wasPressed) onPressed?.Invoke();
            wasPressed = isPressed;
        }
    }
}
