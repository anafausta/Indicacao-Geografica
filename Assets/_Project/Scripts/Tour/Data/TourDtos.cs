using System;
using System.Collections.Generic;

namespace OdisseiaVR.Tour.Data
{
    [Serializable]
    internal class LocalizedTextJson
    {
        public string pt;
        public string en;
        public string es;
    }

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
        public LocalizedTextJson title;
        public LocalizedTextJson description;
        public Vector3Json position;
        public string imagePath;
    }

    [Serializable]
    internal class DesafioJson
    {
        public string panoramaMaterialPath;
        public string videoPath;
        public float initialYRotation;
        public LocalizedTextJson questionText;
        public List<LocalizedTextJson> answers;
        public int correctAnswerIndex;
        public List<HotspotJson> hotspots;
    }

    [Serializable]
    internal class DadosLocalJson
    {
        public LocalizedTextJson locationName;
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