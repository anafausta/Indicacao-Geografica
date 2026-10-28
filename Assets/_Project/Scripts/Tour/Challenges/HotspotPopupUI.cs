using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OdisseiaVR.Tour.Data;

namespace OdisseiaVR.Tour.Challenges
{
    /// <summary>
    /// Controla a interface visual do Pop-up informativo (estilo Wikipédia).
    /// </summary>
    public class HotspotPopupUI : MonoBehaviour
    {
        private const float DistanceFromUser = 1.5f;
        private const float MinHorizontalMagnitude = 0.001f;

        [Header("Elementos de UI")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image optionalImage;
        [SerializeField] private Button closeButton;

        private Action onCloseCallback;

        // Evita que o Awake esconda o pop-up quando ele roda por causa do próprio DisplayInfo
        // (isso acontece se o objeto estiver inativo no Editor: o Awake só roda na primeira ativação).
        private bool isShowing;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (!isShowing)
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void DisplayInfo(HotspotData data, Action onClose)
        {
            if (data == null)
            {
                Debug.LogWarning("[HotspotPopupUI] DisplayInfo chamado com dados nulos.");
                return;
            }

            onCloseCallback = onClose;

            if (titleText != null)
                titleText.text = data.Title;

            if (descriptionText != null)
                descriptionText.text = data.Description;

            // Carrega imagem opcional da pasta Resources se o caminho estiver preenchido
            if (optionalImage != null)
            {
                if (!string.IsNullOrWhiteSpace(data.ImagePath))
                {
                    Sprite loadedSprite = Resources.Load<Sprite>(data.ImagePath);
                    optionalImage.sprite = loadedSprite;
                    optionalImage.gameObject.SetActive(loadedSprite != null);
                }
                else
                {
                    optionalImage.gameObject.SetActive(false);
                }
            }

            PositionInFrontOfUser();

            isShowing = true;
            gameObject.SetActive(true);
        }

        public void Close()
        {
            isShowing = false;
            gameObject.SetActive(false);
            onCloseCallback?.Invoke();
        }

        /// <summary>Posiciona o pop-up à frente do usuário, na altura dos olhos, virado para ele.</summary>
        private void PositionInFrontOfUser()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Transform head = cam.transform;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);

            if (forward.sqrMagnitude < MinHorizontalMagnitude)
            {
                // Olhando quase na vertical: o "topo da cabeça" indica para onde o rosto está virado.
                Vector3 headUp = head.forward.y > 0f ? -head.up : head.up;
                forward = Vector3.ProjectOnPlane(headUp, Vector3.up);
            }

            if (forward.sqrMagnitude < MinHorizontalMagnitude)
                forward = Vector3.forward;

            forward.Normalize();

            transform.position = head.position + forward * DistanceFromUser;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}