using UnityEngine;

namespace OdisseiaVR.Tour
{
    /// <summary>Encapsula a esfera 360: troca material e rotação. Nada além disso.</summary>
    public sealed class PanoramaPresenter : MonoBehaviour
    {
        [Tooltip("Renderer da esfera que exibe o panorama 360°.")]
        [SerializeField] private Renderer sphereRenderer;

        public void Show(Material material, float yRotation)
        {
            if (material != null) sphereRenderer.sharedMaterial = material;
            sphereRenderer.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        /// <summary>Solta a referência ao material para permitir descarregar o panorama anterior.</summary>
        public void Clear()
        {
            sphereRenderer.sharedMaterial = null;
        }
    }
}
