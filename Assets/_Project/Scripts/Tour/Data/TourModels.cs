using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace OdisseiaVR.Tour.Data
{
    public enum ChallengeKind
    {
        Quiz,
        Video
    }

    /// <summary>
    /// Modelo de domínio que representa um Hotspot Interativo no cenário 360°.
    /// </summary>
    public sealed class HotspotData
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public Vector3 Position { get; }
        public string ImagePath { get; }

        public HotspotData(string id, string title, string description, Vector3 position, string imagePath = null)
        {
            Id = id;
            Title = title;
            Description = description;
            Position = position;
            ImagePath = imagePath;
        }
    }

    /// <summary>
    /// Um passo do tour. Substitui a classe Desafio que misturava quiz e vídeo
    /// (campos de quiz vazios em vídeos, flag IsVideo). Cada tipo carrega só o que usa.
    /// </summary>
    public abstract class Challenge
    {
        public float InitialYRotation { get; }
        public abstract ChallengeKind Kind { get; }

        protected Challenge(float initialYRotation)
        {
            InitialYRotation = initialYRotation;
        }
    }

    public sealed class QuizChallenge : Challenge
    {
        public Material Panorama { get; }
        public string Question { get; }
        public IReadOnlyList<string> Answers { get; }
        public int CorrectAnswerIndex { get; }
        public IReadOnlyList<HotspotData> Hotspots { get; } // Campo adicionado

        public override ChallengeKind Kind => ChallengeKind.Quiz;

        public QuizChallenge(float yRotation, Material panorama, string question,
                             IEnumerable<string> answers, int correctAnswerIndex,
                             IEnumerable<HotspotData> hotspots = null) : base(yRotation)
        {
            Panorama = panorama;
            Question = question;
            Answers = new List<string>(answers ?? new List<string>());
            CorrectAnswerIndex = correctAnswerIndex;
            Hotspots = new List<HotspotData>(hotspots ?? new List<HotspotData>());
        }

        public bool IsCorrect(int answerIndex) => answerIndex == CorrectAnswerIndex;
    }

    public sealed class VideoChallenge : Challenge
    {
        public VideoClip Clip { get; }

        public override ChallengeKind Kind => ChallengeKind.Video;

        public VideoChallenge(float yRotation, VideoClip clip) : base(yRotation)
        {
            Clip = clip;
        }
    }

    /// <summary>Local com todos os assets carregados. Antes: DadosLocal.</summary>
    public sealed class TourLocation
    {
        public string Name { get; }
        public AudioClip BackgroundMusic { get; }
        public IReadOnlyList<Challenge> Challenges { get; }

        public TourLocation(string name, AudioClip backgroundMusic, IReadOnlyList<Challenge> challenges)
        {
            Name = name;
            BackgroundMusic = backgroundMusic;
            Challenges = challenges;
        }
    }

    /// <summary>Dados leves para o menu. Antes: LobbyDadosLocal.</summary>
    public sealed class LocationSummary
    {
        public string Name { get; }
        public Texture MapTexture { get; }

        public LocationSummary(string name, Texture mapTexture)
        {
            Name = name;
            MapTexture = mapTexture;
        }
    }
}