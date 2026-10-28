using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using OdisseiaVR.Tour.Data;

namespace OdisseiaVR.Tour.Challenges
{
    /// <summary>
    /// Componente anexo ao objeto 3D do Hotspot.
    /// - Hover (apontar): muda a cor, via eventos do XRSimpleInteractable no prefab.
    /// - Acionamento: GATILHO (ação Activate) do controle que está apontando para o hotspot.
    ///   Funciona igual com o controle real e com o XR Device Simulator, porque ambos
    ///   passam pelo mesmo Input Action do interactor.
    /// - Billboard: o ícone sempre olha para a câmera.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class Hotspot : MonoBehaviour
    {
        [SerializeField] private Color hoverColor = Color.yellow;

        private HotspotData data;
        private HotspotManager manager;
        private XRSimpleInteractable interactable;
        private Renderer meshRenderer;
        private Color originalColor;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
        }

        public void Setup(HotspotData hotspotData, HotspotManager managerRef)
        {
            data = hotspotData;
            manager = managerRef;
            transform.localPosition = data.Position;

            meshRenderer = GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        private void Update()
        {
            FaceCamera();
            PollTrigger();
        }

        /// <summary>Garante que o ícone esteja sempre virado para a câmera do usuário (Billboard).</summary>
        private void FaceCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            transform.LookAt(cam.transform);
            transform.Rotate(0f, 180f, 0f);
        }

        /// <summary>
        /// Abre o pop-up quando o gatilho é pressionado por um controle que está apontando para este hotspot.
        /// Lê a ação "Activate" do próprio interactor, sem depender de qual tecla ou botão o simulador usa.
        /// </summary>
        private void PollTrigger()
        {
            if (interactable == null || data == null) return;

            var hovering = interactable.interactorsHovering;
            for (int i = 0; i < hovering.Count; i++)
            {
                if (hovering[i] is XRBaseInputInteractor input && input.activateInput.ReadWasPerformedThisFrame())
                {
                    OpenPopup();
                    return;
                }
            }
        }

        // Ligado no prefab (XRSimpleInteractable > Hover Entered / Hover Exited).
        public void OnHoverEnter()
        {
            if (meshRenderer != null)
                meshRenderer.material.color = hoverColor;
        }

        public void OnHoverExit()
        {
            if (meshRenderer != null)
                meshRenderer.material.color = originalColor;
        }

        // Mantido público e com o mesmo nome: se algum prefab antigo ainda ligar "Select Entered" aqui,
        // o grip também abre o pop-up e nada quebra.
        public void OnSelect() => OpenPopup();

        private void OpenPopup()
        {
            if (manager == null || data == null)
            {
                Debug.LogWarning($"[Hotspot] '{name}' acionado sem dados ou manager (Setup não foi chamado).");
                return;
            }

            manager.ShowPopup(data);
        }

#if UNITY_EDITOR
        // Teste rápido sem controle: botão direito no componente, em Play mode.
        [ContextMenu("DEBUG/Abrir pop-up")]
        private void DebugOpenPopup() => OpenPopup();
#endif
    }
}