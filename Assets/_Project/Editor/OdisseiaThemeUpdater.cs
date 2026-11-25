using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OdisseiaVR.EditorTools
{
    public class OdisseiaThemeUpdater : EditorWindow
    {
        [Header("Tipografia Opcional (Plus Jakarta Sans SDF)")]
        [SerializeField] private TMP_FontAsset fontBold;
        [SerializeField] private TMP_FontAsset fontSemiBold;
        [SerializeField] private TMP_FontAsset fontRegular;

        [Header("Sprites 9-Slice Opcionais (Deixe vazio para cor pura + borda)")]
        [SerializeField] private Sprite panelRoundedSprite;
        [SerializeField] private Sprite buttonRoundedSprite;

        // =====================================================================
        // PALETA & ESTADOS DE HOVER MAIS ESCUROS
        // =====================================================================
        private static readonly Color32 ColTourPanelBg      = new Color32(18, 22, 30, 244);   // Fundo escuro sólido apenas no Tour (Pop-up / Quiz)
        private static readonly Color32 ColSurfaceVariant   = new Color32(28, 35, 48, 235);   // Cards / Opções de Quiz
        private static readonly Color32 ColSurfaceHoverDark = new Color32(14, 18, 26, 250);   // Hover bem mais escuro

        // Primário (Dourado Âmbar)
        private static readonly Color32 ColPrimary          = new Color32(245, 183, 56, 255); // #F5B738
        private static readonly Color32 ColPrimaryHover     = new Color32(190, 134, 24, 255); // Hover mais escuro
        private static readonly Color32 ColPrimaryPressed   = new Color32(145, 98, 12, 255);
        private static readonly Color32 ColOnPrimary        = new Color32(26, 19, 0, 255);    // #1A1300

        // Secundário (Botões Anterior / Próxima / Menu / Idioma Inativo)
        private static readonly Color32 ColBtnSecondary     = new Color32(32, 40, 54, 235);
        private static readonly Color32 ColBtnSecHover      = new Color32(15, 20, 28, 250);   // Hover mais escuro
        private static readonly Color32 ColBtnSecPressed    = new Color32(8, 11, 16, 255);

        // Idioma Ativo (Ciano XR)
        private static readonly Color32 ColLangActive       = new Color32(0, 184, 212, 235);
        private static readonly Color32 ColLangActiveHover  = new Color32(0, 128, 152, 250);  // Hover mais escuro
        private static readonly Color32 ColLangActivePress  = new Color32(0, 92, 110, 255);

        // Fechar / Erro (Vermelho Coral)
        private static readonly Color32 ColError            = new Color32(204, 51, 26, 240);  // #CC331A
        private static readonly Color32 ColErrorHover       = new Color32(145, 28, 12, 255);  // Hover mais escuro
        private static readonly Color32 ColErrorPressed     = new Color32(105, 18, 8, 255);

        // Tipografia e Bordas
        private static readonly Color32 ColOnSurface        = new Color32(240, 243, 250, 255);// #F0F3FA
        private static readonly Color32 ColOnSurfaceVariant = new Color32(205, 214, 226, 255);// #CDD6E2
        private static readonly Color32 ColSecondaryAccent  = new Color32(0, 217, 245, 255);  // #00D9F5
        private static readonly Color32 ColBorderGlass      = new Color32(255, 255, 255, 45);
        private static readonly Color32 ColScrollTrack      = new Color32(0, 0, 0, 95);
        private static readonly Color32 ColScrollThumb      = new Color32(0, 217, 245, 150);

        [MenuItem("OdisseiaVR/Configurar e Aplicar Tema Visual")]
        public static void ShowWindow()
        {
            var window = GetWindow<OdisseiaThemeUpdater>("OdisseiaVR UI");
            window.minSize = new Vector2(400, 310);
        }

        [MenuItem("OdisseiaVR/Aplicar Tema Rápido na Cena Atual")]
        public static void ApplyQuick()
        {
            ApplyThemeToCurrentScene(null, null, null, null, null);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("OdisseiaVR — Atualizador de Layout & Style Guide", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Remove o fundo preto do Lobby, corrige a visibilidade do texto no Pop-up, " +
                "escurece o hover dos botões e alinha nome e sobrenome na mesma linha nos Créditos.",
                MessageType.Info);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Fontes TextMeshPro (Opcional)", EditorStyles.boldLabel);
            fontBold = (TMP_FontAsset)EditorGUILayout.ObjectField("Plus Jakarta Bold", fontBold, typeof(TMP_FontAsset), false);
            fontSemiBold = (TMP_FontAsset)EditorGUILayout.ObjectField("Plus Jakarta SemiBold", fontSemiBold, typeof(TMP_FontAsset), false);
            fontRegular = (TMP_FontAsset)EditorGUILayout.ObjectField("Plus Jakarta Regular", fontRegular, typeof(TMP_FontAsset), false);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Sprites 9-Slice (Opcional)", EditorStyles.boldLabel);
            panelRoundedSprite = (Sprite)EditorGUILayout.ObjectField("Sprite Painel Arredondado", panelRoundedSprite, typeof(Sprite), false);
            buttonRoundedSprite = (Sprite)EditorGUILayout.ObjectField("Sprite Botão Arredondado", buttonRoundedSprite, typeof(Sprite), false);

            GUILayout.Space(16);
            GUI.backgroundColor = new Color(0.96f, 0.72f, 0.22f);
            if (GUILayout.Button("APLICAR AJUSTES NA CENA ATUAL", GUILayout.Height(40)))
            {
                ApplyThemeToCurrentScene(fontBold, fontSemiBold, fontRegular, panelRoundedSprite, buttonRoundedSprite);
            }
            GUI.backgroundColor = Color.white;
        }

        public static void ApplyThemeToCurrentScene(
            TMP_FontAsset fBold,
            TMP_FontAsset fSemiBold,
            TMP_FontAsset fRegular,
            Sprite panelSprite,
            Sprite btnSprite)
        {
            int updatedCount = 0;

            // 1. Remove fundos pretos/cinzas das telas do Lobby (mantendo UI flutuando limpa no céu)
            RemoveLobbyDarkBackgrounds();

            // 2. Corrige a estrutura do Pop-up (Viewport Mask, Rotação Y=180, Z=-5000, Wrapping)
            FixHotspotPopupStructure(fBold, fRegular);

            // 3. Estiliza Images restantes (Scrollbars, Painéis do Tour, etc.)
            foreach (var img in FindAllInScene<Image>())
            {
                if (img.GetComponent<Button>() != null) continue;

                string n = img.gameObject.name.ToLowerInvariant();
                bool isInsideLobby = IsInsideLobbyCanvas(img.transform);

                // Não mexe na foto do ponto turístico dentro do Content do Pop-up
                if (img.transform.parent != null && img.transform.parent.name.ToLowerInvariant() == "content")
                {
                    Undo.RecordObject(img, "Preserve Popup Content Image");
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);
                    continue;
                }

                // Viewport precisa ter Alpha = 255 para o Mask funcionar, mas com Show Mask Graphic desligado!
                if (n == "viewport")
                {
                    Undo.RecordObject(img, "Fix Viewport Mask Image");
                    img.color = Color.white;
                    Mask mask = img.GetComponent<Mask>();
                    if (mask != null)
                    {
                        Undo.RecordObject(mask, "Hide Mask Graphic");
                        mask.showMaskGraphic = false;
                        EditorUtility.SetDirty(mask);
                    }
                    EditorUtility.SetDirty(img);
                    updatedCount++;
                    continue;
                }

                Undo.RecordObject(img, "OdisseiaVR Image Theme");

                // Containers transparentes (Header, Scroll View, Content, e qualquer painel do Lobby)
                if (isInsideLobby || n.Contains("header") || n.Contains("canvas_language") ||
                    n == "content" || n.Contains("scroll view") || n.Contains("scrollview"))
                {
                    img.color = new Color(0, 0, 0, 0);
                    EditorUtility.SetDirty(img);
                    updatedCount++;
                    continue;
                }

                // Barra de Rolagem Espacial (.vr-scroll)
                if (n.Contains("scrollbar"))
                {
                    img.sprite = null;
                    img.color = ColScrollTrack;
                    EditorUtility.SetDirty(img);
                    updatedCount++;
                    continue;
                }
                if (n == "handle")
                {
                    img.sprite = null;
                    img.color = ColScrollThumb;
                    EditorUtility.SetDirty(img);
                    updatedCount++;
                    continue;
                }

                // Painéis do Tour (CanvasHotspotPopup -> Panel, PainelQuiz -> Panel)
                if (n.Contains("panel") || n.Contains("fundo"))
                {
                    ApplySpriteOrFlat(img, panelSprite);
                    img.color = ColTourPanelBg;
                    ApplySubtleOutline(img.gameObject, ColBorderGlass);
                    EditorUtility.SetDirty(img);
                    updatedCount++;
                }
            }

            // 4. Protege RawImages (prévia do mapa no Lobby e logo GPIA no chão)
            foreach (var raw in FindAllInScene<RawImage>())
            {
                Undo.RecordObject(raw, "OdisseiaVR RawImage Preserve");
                raw.color = Color.white;
                EditorUtility.SetDirty(raw);
            }

            // 5. Estiliza todos os Botões com HOVER MAIS ESCURO
            foreach (var btn in FindAllInScene<Button>())
            {
                Image img = btn.GetComponent<Image>();
                if (img == null) continue;

                Undo.RecordObject(btn, "OdisseiaVR Button Theme");
                Undo.RecordObject(img, "OdisseiaVR Button Image");

                string n = btn.gameObject.name.ToLowerInvariant();
                ApplySpriteOrFlat(img, btnSprite);
                img.color = Color.white;

                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = btn.colors;
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.12f;

                // A. Botão Fechar (Pop-up) -> Vermelho Coral com Hover mais escuro
                if (n.Contains("close") || n.Contains("fechar"))
                {
                    cb.normalColor = ColError;
                    cb.highlightedColor = ColErrorHover;
                    cb.pressedColor = ColErrorPressed;
                    cb.selectedColor = ColError;
                    btn.colors = cb;

                    ApplySubtleOutline(btn.gameObject, new Color32(255, 140, 130, 120));
                    StyleButtonTexts(btn, ColOnSurface, fBold, 30f, FontStyles.Bold);
                }
                // B. Botão Principal (Botao_Mapa / Iniciar) -> Dourado com Hover mais escuro
                else if (n.Contains("mapa") || n.Contains("iniciar") || n.Contains("start") || n.Contains("primary"))
                {
                    cb.normalColor = ColPrimary;
                    cb.highlightedColor = ColPrimaryHover;
                    cb.pressedColor = ColPrimaryPressed;
                    cb.selectedColor = ColPrimary;
                    btn.colors = cb;

                    ApplySubtleOutline(btn.gameObject, new Color32(255, 230, 150, 120));
                    StyleButtonTexts(btn, ColOnPrimary, fSemiBold != null ? fSemiBold : fBold, 20f, FontStyles.Bold);
                }
                // C. Botões de Idioma (BtnPT, BtnEn, BtnEs) -> Hover mais escuro
                else if (n.Contains("btnpt") || n.Contains("btnen") || n.Contains("btnes") || n.Contains("lang"))
                {
                    bool isDefaultPt = n.Contains("pt");
                    cb.normalColor = isDefaultPt ? ColLangActive : ColBtnSecondary;
                    cb.highlightedColor = isDefaultPt ? ColLangActiveHover : ColBtnSecHover;
                    cb.pressedColor = isDefaultPt ? ColLangActivePress : ColBtnSecPressed;
                    cb.selectedColor = isDefaultPt ? ColLangActive : ColBtnSecondary;
                    btn.colors = cb;

                    ApplySubtleOutline(btn.gameObject, isDefaultPt ? ColSecondaryAccent : ColBorderGlass);
                    StyleButtonTexts(btn, ColOnSurface, fSemiBold, 18f, FontStyles.Bold);
                }
                // D. Opções de Quiz (Opcao 1, Opcao 2, Opcao 3) -> Hover mais escuro
                else if (n.Contains("opcao") || n.Contains("option") || n.Contains("resposta") || n.Contains("alt"))
                {
                    cb.normalColor = ColSurfaceVariant;
                    cb.highlightedColor = ColSurfaceHoverDark;
                    cb.pressedColor = ColBtnSecPressed;
                    cb.selectedColor = ColSurfaceVariant;
                    btn.colors = cb;

                    ApplySubtleOutline(btn.gameObject, ColBorderGlass);
                    StyleButtonTexts(btn, ColOnSurface, fSemiBold, 18f, FontStyles.Normal);
                }
                // E. Botões Secundários (Anterior, Próxima, Menu) -> Hover mais escuro
                else
                {
                    cb.normalColor = ColBtnSecondary;
                    cb.highlightedColor = ColBtnSecHover;
                    cb.pressedColor = ColBtnSecPressed;
                    cb.selectedColor = ColBtnSecondary;
                    btn.colors = cb;

                    ApplySubtleOutline(btn.gameObject, ColBorderGlass);
                    StyleButtonTexts(btn, ColOnSurface, fSemiBold, 16f, FontStyles.Bold);
                }

                EditorUtility.SetDirty(btn);
                EditorUtility.SetDirty(img);
                updatedCount++;
            }

            // 6. Estiliza Tipografia e corrige Créditos (Nome e Sobrenome na mesma linha)
            foreach (var tmp in FindAllInScene<TextMeshProUGUI>())
            {
                if (tmp.GetComponentInParent<Button>() != null) continue;

                Undo.RecordObject(tmp, "OdisseiaVR Typography");
                string n = tmp.gameObject.name.ToLowerInvariant();

                if (n.Contains("titulo") || n.Contains("title") || n.Contains("pergunta") || n.Contains("question") || n.Contains("txtmapa"))
                {
                    tmp.color = ColOnSurface;
                    tmp.fontStyle |= FontStyles.Bold;
                    if (fBold != null) tmp.font = fBold;
                }
                else if (n.Contains("sub") || n.Contains("tag") || n.Contains("badge") || n.Contains("categoria"))
                {
                    tmp.color = ColSecondaryAccent;
                    if (fSemiBold != null) tmp.font = fSemiBold;
                }
                else
                {
                    tmp.color = ColOnSurfaceVariant;
                    if (fRegular != null) tmp.font = fRegular;
                }

                EditorUtility.SetDirty(tmp);
                updatedCount++;
            }

            FixCreditsSingleLineNames(fBold, fRegular);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=#00D9F5><b>[OdisseiaVR UI]</b></color> Ajustes aplicados com sucesso em <b>{updatedCount}</b> componentes! Pressione <b>Ctrl + S</b> para salvar.");
        }

        /// <summary>
        /// Remove fundos pretos/cinzas criados nos Canvas do Lobby para manter a UI limpa sobre o céu.
        /// </summary>
        private static void RemoveLobbyDarkBackgrounds()
        {
            string[] lobbyCanvasNames = { "Canvas_FrontPanel", "Canvas_LeftPanel", "Canvas_RightPanel" };
            foreach (string canvasName in lobbyCanvasNames)
            {
                GameObject canvasObj = GameObject.Find(canvasName);
                if (canvasObj == null) continue;

                Transform bg = canvasObj.transform.Find("Fundo_Painel");
                if (bg != null)
                {
                    Undo.DestroyObjectImmediate(bg.gameObject);
                }

                foreach (var img in canvasObj.GetComponentsInChildren<Image>(true))
                {
                    if (img.GetComponent<Button>() != null) continue;
                    string n = img.gameObject.name.ToLowerInvariant();
                    if (n.Contains("panel") || n.Contains("fundo") || n.Contains("header") || n.Contains("canvas_language"))
                    {
                        Undo.RecordObject(img, "Transparent Lobby Panel");
                        img.color = new Color(0, 0, 0, 0);
                        Outline outl = img.GetComponent<Outline>();
                        if (outl != null) Undo.DestroyObjectImmediate(outl);
                        EditorUtility.SetDirty(img);
                    }
                }
            }
        }

        /// <summary>
        /// Corrige todos os problemas que impediam o texto e a imagem do CanvasHotspotPopup de aparecerem:
        /// - Zera Rotation Y=180 e Pos Z=-5000 no Scroll View / Content / Descrição
        /// - Garante Viewport com Mask visível (Image Alpha=255 + showMaskGraphic=false)
        /// - Garante LayoutElement FlexibleHeight=1 no Scroll View e TextWrapping ativo na Descrição
        /// </summary>
        private static void FixHotspotPopupStructure(TMP_FontAsset fBold, TMP_FontAsset fRegular)
        {
            GameObject popupCanvas = GameObject.Find("CanvasHotspotPopup");
            if (popupCanvas == null) return;

            Transform panel = popupCanvas.transform.Find("Panel");
            if (panel == null) return;

            ResetRectTransformFlat(panel.GetComponent<RectTransform>());
            VerticalLayoutGroup panelVlg = panel.GetComponent<VerticalLayoutGroup>();
            if (panelVlg != null)
            {
                Undo.RecordObject(panelVlg, "Fix Popup Panel VLG");
                panelVlg.padding = new RectOffset(24, 24, 20, 20);
                panelVlg.spacing = 14f;
                panelVlg.childControlWidth = true;
                panelVlg.childControlHeight = true;
                panelVlg.childForceExpandWidth = true;
                panelVlg.childForceExpandHeight = false;
                EditorUtility.SetDirty(panelVlg);
            }

            // HeaderLayout
            Transform header = panel.Find("HeaderLayout");
            if (header != null)
            {
                ResetRectTransformFlat(header.GetComponent<RectTransform>());
                HorizontalLayoutGroup hlg = header.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    Undo.RecordObject(hlg, "Fix Popup Header HLG");
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.spacing = 16f;
                    EditorUtility.SetDirty(hlg);
                }

                LayoutElement headerLe = header.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(header.gameObject);
                headerLe.preferredHeight = 64f;
                headerLe.flexibleHeight = 0f;

                foreach (Transform child in header)
                {
                    ResetRectTransformFlat(child.GetComponent<RectTransform>());
                    LayoutElement le = child.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(child.gameObject);
                    if (child.name.ToLowerInvariant().Contains("close") || child.name.ToLowerInvariant().Contains("fechar"))
                    {
                        le.preferredWidth = 56f;
                        le.preferredHeight = 56f;
                        le.flexibleWidth = 0f;
                    }
                    else
                    {
                        le.flexibleWidth = 1f;
                    }
                }
            }

            // Scroll View
            Transform scrollView = panel.Find("Scroll View");
            if (scrollView == null) return;

            ResetRectTransformFlat(scrollView.GetComponent<RectTransform>());
            LayoutElement svLe = scrollView.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(scrollView.gameObject);
            Undo.RecordObject(svLe, "Fix ScrollView LayoutElement");
            svLe.flexibleHeight = 1f;
            svLe.flexibleWidth = 1f;
            EditorUtility.SetDirty(svLe);

            ScrollRect sr = scrollView.GetComponent<ScrollRect>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Fix ScrollRect");
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;
                EditorUtility.SetDirty(sr);
            }

            // Viewport
            Transform viewport = scrollView.Find("Viewport");
            if (viewport == null) return;

            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            ResetRectTransformFlat(vpRt);
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = new Vector2(-18f, 0f); // Espaço para a scrollbar

            Image vpImg = viewport.GetComponent<Image>();
            if (vpImg != null)
            {
                Undo.RecordObject(vpImg, "Fix Viewport Alpha");
                vpImg.color = Color.white; // Alpha 255 obrigatório para o Mask não esconder os filhos!
                EditorUtility.SetDirty(vpImg);
            }
            Mask vpMask = viewport.GetComponent<Mask>();
            if (vpMask != null)
            {
                Undo.RecordObject(vpMask, "Fix Viewport ShowMaskGraphic");
                vpMask.showMaskGraphic = false; // Oculta o gráfico branco do Viewport sem cortar os filhos
                EditorUtility.SetDirty(vpMask);
            }

            // Content
            Transform content = viewport.Find("Content");
            if (content == null) return;

            RectTransform contentRt = content.GetComponent<RectTransform>();
            ResetRectTransformFlat(contentRt);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup contentVlg = content.GetComponent<VerticalLayoutGroup>() ?? Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);
            Undo.RecordObject(contentVlg, "Fix Content VLG");
            contentVlg.padding = new RectOffset(16, 16, 12, 20);
            contentVlg.spacing = 16f;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            EditorUtility.SetDirty(contentVlg);

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>() ?? Undo.AddComponent<ContentSizeFitter>(content.gameObject);
            Undo.RecordObject(csf, "Fix Content Size Fitter");
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(csf);

            // Filhos do Content (Imagem opcional e Descrição)
            foreach (Transform item in content)
            {
                ResetRectTransformFlat(item.GetComponent<RectTransform>());

                Image itemImg = item.GetComponent<Image>();
                if (itemImg != null)
                {
                    Undo.RecordObject(itemImg, "Fix Popup Image");
                    itemImg.color = Color.white;
                    itemImg.preserveAspect = true;
                    LayoutElement imgLe = item.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(item.gameObject);
                    imgLe.preferredHeight = 220f;
                    imgLe.flexibleWidth = 1f;
                    EditorUtility.SetDirty(itemImg);
                }

                TextMeshProUGUI descTmp = item.GetComponent<TextMeshProUGUI>();
                if (descTmp != null)
                {
                    Undo.RecordObject(descTmp, "Fix Popup Description TMP");
                    descTmp.color = ColOnSurface;
                    descTmp.textWrappingMode = TextWrappingModes.Normal;
                    descTmp.overflowMode = TextOverflowModes.Overflow;
                    descTmp.alignment = TextAlignmentOptions.TopLeft;
                    if (fRegular != null) descTmp.font = fRegular;
                    descTmp.fontSize = 22f;
                    descTmp.lineSpacing = 16f;

                    LayoutElement descLe = item.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(item.gameObject);
                    descLe.ignoreLayout = false;
                    descLe.flexibleWidth = 1f;
                    descLe.preferredHeight = -1f; // Altura calculada automaticamente pelo texto
                    EditorUtility.SetDirty(descTmp);
                }
            }
        }

        /// <summary>
        /// Garante que cada nome nos Créditos (ALMIR CESAR, ANA MARIA, DANIELA SANTANA) fique na mesma linha.
        /// </summary>
        private static void FixCreditsSingleLineNames(TMP_FontAsset fBold, TMP_FontAsset fRegular)
        {
            GameObject rightPanel = GameObject.Find("Canvas_RightPanel");
            if (rightPanel == null) return;

            foreach (var tmp in rightPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Undo.RecordObject(tmp, "Fix Credits Single Line");
                RectTransform rt = tmp.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Undo.RecordObject(rt, "Widen Credits RectTransform");
                    if (rt.sizeDelta.x < 850f)
                        rt.sizeDelta = new Vector2(920f, Mathf.Max(rt.sizeDelta.y, 480f));
                    EditorUtility.SetDirty(rt);
                }

                string text = tmp.text;
                if (!string.IsNullOrEmpty(text))
                {
                    // Junta nome e sobrenome caso estejam separados por quebra de linha
                    text = Regex.Replace(text, @"ALMIR\s*\r?\n\s*CESAR", "ALMIR CESAR", RegexOptions.IgnoreCase);
                    text = Regex.Replace(text, @"ANA\s*\r?\n\s*MARIA", "ANA MARIA", RegexOptions.IgnoreCase);
                    text = Regex.Replace(text, @"DANIELA\s*\r?\n\s*SANTANA", "DANIELA SANTANA", RegexOptions.IgnoreCase);
                    tmp.text = text;
                }

                string objName = tmp.gameObject.name.ToLowerInvariant();
                if (objName.Contains("titulo") || tmp.text.Trim().ToUpperInvariant().StartsWith("CRÉDITOS"))
                {
                    tmp.color = ColOnSurface;
                    tmp.fontStyle |= FontStyles.Bold;
                    if (fBold != null) tmp.font = fBold;
                }
                else
                {
                    tmp.color = ColOnSurfaceVariant;
                    tmp.enableAutoSizing = false;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap; // Impede quebrar nome e sobrenome
                    if (tmp.fontSize > 38f || tmp.fontSize < 24f)
                        tmp.fontSize = 34f;
                    tmp.lineSpacing = 22f;
                    if (fRegular != null) tmp.font = fRegular;
                }

                EditorUtility.SetDirty(tmp);
            }
        }

        private static void ResetRectTransformFlat(RectTransform rt)
        {
            if (rt == null) return;
            Undo.RecordObject(rt, "Reset RectTransform Flat");
            Vector3 pos = rt.localPosition;
            pos.z = 0f;
            rt.localPosition = pos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            EditorUtility.SetDirty(rt);
        }

        private static bool IsInsideLobbyCanvas(Transform t)
        {
            Transform curr = t;
            while (curr != null)
            {
                if (curr.name == "Canvas_FrontPanel" || curr.name == "Canvas_LeftPanel" || curr.name == "Canvas_RightPanel")
                    return true;
                curr = curr.parent;
            }
            return false;
        }

        private static void ApplySpriteOrFlat(Image img, Sprite sprite)
        {
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            else
            {
                img.sprite = null;
            }
        }

        private static void ApplySubtleOutline(GameObject target, Color32 borderColor)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(target);
            else
                Undo.RecordObject(outline, "OdisseiaVR Outline");

            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            EditorUtility.SetDirty(outline);
        }

        private static void StyleButtonTexts(Button btn, Color32 textColor, TMP_FontAsset font, float minSize, FontStyles style)
        {
            foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Undo.RecordObject(tmp, "OdisseiaVR Button Text");
                tmp.color = textColor;
                tmp.fontStyle = style;
                tmp.alignment = TextAlignmentOptions.Center;
                if (font != null) tmp.font = font;
                if (tmp.fontSize < minSize) tmp.fontSize = minSize;
                EditorUtility.SetDirty(tmp);
            }
        }

        private static T[] FindAllInScene<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>(true);
#endif
        }
    }
}