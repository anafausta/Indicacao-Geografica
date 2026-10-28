using System.Collections.Generic;
using UnityEngine;
using OdisseiaVR.Tour.Data;

namespace OdisseiaVR.Tour.Challenges
{
    /// <summary>
    /// Gerencia a instanciação, exibição e destruição dos Hotspots 3D 
    /// no cenário panorâmico 360°, além de integrar a UI do Pop-up informativo.
    /// </summary>
    public class HotspotManager : MonoBehaviour
    {
        [Header("Prefabs e UI")]
        [SerializeField] private GameObject hotspotPrefab;
        [SerializeField] private HotspotPopupUI popupUI;

        private readonly List<GameObject> activeHotspotObjects = new List<GameObject>();

        /// <summary>
        /// Instancia e posiciona os Hotspots no cenário a partir da lista fornecida pelo desafio.
        /// </summary>

        public void SpawnHotspots(IReadOnlyList<HotspotData> hotspotsData, float panoramaYaw)
        {
            ClearHotspots();
            transform.rotation = Quaternion.Euler(0f, panoramaYaw, 0f);
            if (hotspotsData == null || hotspotsData.Count == 0) return;

            foreach (var data in hotspotsData)
            {
                if (hotspotPrefab == null)
                {
                    Debug.LogWarning("[HotspotManager] Prefab de Hotspot não configurado no Inspector.");
                    return;
                }

                GameObject obj = Instantiate(hotspotPrefab, transform);
                Hotspot hotspotComponent = obj.GetComponent<Hotspot>();

                if (hotspotComponent != null)
                {
                    hotspotComponent.Setup(data, this);
                }
                else
                {
                    Debug.LogWarning("[HotspotManager] O Prefab instanciado não possui o componente 'Hotspot'.");
                }

                activeHotspotObjects.Add(obj);
            }
        }

        /// <summary>
        /// Exibe o Pop-up com os dados do Hotspot clicado.
        /// </summary>
        public void ShowPopup(HotspotData data)
        {
            if (popupUI != null)
            {
                popupUI.DisplayInfo(data, OnPopupClosed);
            }
            else
            {
                Debug.LogWarning("[HotspotManager] Referência da UI 'HotspotPopupUI' não atribuída.");
            }
        }

        /// <summary>
        /// Callback executado ao fechar o Pop-up informativo.
        /// </summary>
        private void OnPopupClosed()
        {
            // Ações secundárias ao fechar o pop-up (ex: reativar áudio, log de métrica)
        }

        /// <summary>
        /// Remove e destrói todos os Hotspots atualmente visíveis no cenário.
        /// </summary>
        public void ClearHotspots()
        {
            foreach (var obj in activeHotspotObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            activeHotspotObjects.Clear();

            if (popupUI != null)
            {
                popupUI.Close();
            }
        }

        private void OnDisable()
        {
            ClearHotspots();
        }
    }
}