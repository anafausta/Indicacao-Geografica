using System;
using System.Collections.Generic;

namespace OdisseiaVR.Tour.Data
{
    // DTOs espelham o tour_data.json (que não muda). Só o repositório os conhece.
    // O resto do projeto trabalha com o modelo de domínio (TourModels.cs).

    [Serializable]
    internal class Vector3Json
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    internal class HotspotJson
    {
        public string id;
        public string title;
        public string description;
        public Vector3Json position;
        public string imagePath;
    }

    [Serializable]
    internal class DesafioJson
    {
        public string panoramaMaterialPath;
        public string videoPath;
        public float initialYRotation;
        public string questionText;
        public List<string> answers;
        public int correctAnswerIndex;
        public List<HotspotJson> hotspots; // Campo adicionado para os hotspots
    }

    [Serializable]
    internal class DadosLocalJson
    {
        public string locationName;
        public string backgroundMusicPath;
        public string mapImagePath;
        public List<DesafioJson> desafios;
    }

    [Serializable]
    internal class TourDataJson
    {
        public List<DadosLocalJson> locais;
    }
}