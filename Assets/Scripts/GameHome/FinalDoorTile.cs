using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(LevelTile))]
public class FinalDoorTile : MonoBehaviour, IPointerClickHandler
{
    [Header("Identificadores")]
    public const int BUILD_OFFSET = 14; // mismo offset que usas
    [Tooltip("BuildIndex real de la escena 204. Si no lo pones, se calcula como 204 + BUILD_OFFSET.")]
    public int buildIndex = -1;
    [Tooltip("Número lógico del nivel final (204).")]
    public int nivelLogico = 204;

    [Header("Iconos de la casilla")]
    public GameObject doorOpenIcon;   // “Door”
    public GameObject doorClosedIcon; // “CloseDoor”

    [Header("Panel que aparece cuando falta algo")]
    public GameObject panelDoorClosed;      // MasterKeyComplete (o contenedor del panel)
    public TMP_Text panelMessage;          // opcional (el texto grande del panel)

    [Header("Íconos del panel: se activan solo si el requisito está completo")]
    // Trofeos (4)
    public GameObject trophyGreen;
    public GameObject trophyBlue;
    public GameObject trophyRed;
    public GameObject trophyGold;

    // Condecoraciones (6)
    public GameObject medal1;
    public GameObject medal2;
    public GameObject medal3;
    public GameObject medal4;
    public GameObject medal5;
    public GameObject medal6;

    // Master Keys (3)
    public GameObject crownOfTheKey;
    public GameObject soulColumn;
    public GameObject toothOfTheKingdom;

    private LevelTile tile;
    private string slotId;

    void Awake()
    {
        tile = GetComponent<LevelTile>();
        slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        if (buildIndex < 0) buildIndex = nivelLogico + BUILD_OFFSET;
    }

    void Start()
    {
        RefreshVisual();
    }

    // ============================
    //  RANGOS → MEDALLAS (fallback por logros)
    // ============================
    private bool HasRank_R40()  => AchievementsManager.IsUnlocked(slotId, AchievementId.Rank_ElegidoEstrellas);
    private bool HasRank_R60()  => AchievementsManager.IsUnlocked(slotId, AchievementId.Rank_VencedorTiempo);
    private bool HasRank_R100() => AchievementsManager.IsUnlocked(slotId, AchievementId.Rank_MaestroSilencio);
    private bool HasRank_R130() => AchievementsManager.IsUnlocked(slotId, AchievementId.Rank_GuardianUmbral);
    private bool HasRank_R170() => AchievementsManager.IsUnlocked(slotId, AchievementId.Rank_ReyEstrellas);
    private bool HasRank_R200() => AchievementsManager.IsUnlocked(slotId, AchievementId.Rank_ReyLibre);

    private int CountMedalsFromRanks()
    {
        int c = 0;
        if (HasRank_R40())  c++;
        if (HasRank_R60())  c++;
        if (HasRank_R100()) c++;
        if (HasRank_R130()) c++;
        if (HasRank_R170()) c++;
        if (HasRank_R200()) c++;
        return c;
    }

    // Llama GameHomeManager al cargar progreso
    public void RefreshVisual()
    {
        bool hasAll = HasAllRequirements();

        if (doorOpenIcon) doorOpenIcon.SetActive(hasAll);
        if (doorClosedIcon) doorClosedIcon.SetActive(!hasAll);

        if (panelDoorClosed) panelDoorClosed.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (HasAllRequirements())
        {
            PlayerPrefs.SetInt("nivelActivo", nivelLogico);
            PlayerPrefs.Save();
            SceneManager.LoadScene(buildIndex);
        }
        else
        {
            MarcarPanel();
            if (panelDoorClosed) panelDoorClosed.SetActive(true);
            if (panelMessage)
                panelMessage.text = "No podrás acceder a esta sala hasta no tener las 3 partes de la Llave Legendaria, " +
                                    "todas las condecoraciones, todos los trofeos, todos los pergaminos, " +
                                    "todos los diamantes y todas las llaves del juego.";
        }
    }

    private bool HasAllRequirements()
    {
        // Totales
        int trophiesTotal   = PlayerPrefs.GetInt($"{slotId}_trophiesTotal", 0); // 4
        int medalsTotal     = PlayerPrefs.GetInt($"{slotId}_medalsTotal", 0);   // 6
        int parchmentsTotal = PlayerPrefs.GetInt($"{slotId}_parchmentsTotal", 0); // 10
        int masterKeysTotal = PlayerPrefs.GetInt($"{slotId}_masterKeysTotal", 0); // 3
        int keysTotal       = PlayerPrefs.GetInt($"{slotId}_keysTotal", 0);     // 500
        int diamondsTotal   = PlayerPrefs.GetInt($"{slotId}_diamondsTotal", 0); // 200

        // Fallback MasterKeys por logros
        bool mk1 = AchievementsManager.IsUnlocked(slotId, AchievementId.MasterKey1);
        bool mk2 = AchievementsManager.IsUnlocked(slotId, AchievementId.MasterKey2);
        bool mk3 = AchievementsManager.IsUnlocked(slotId, AchievementId.MasterKey3);
        if (masterKeysTotal < 3 && mk1 && mk2 && mk3) masterKeysTotal = 3;

        // Fallback Condecoraciones por logros de rango
        int medalsByRanks = CountMedalsFromRanks();
        if (medalsByRanks > medalsTotal) medalsTotal = medalsByRanks;

        bool okTrophies   = trophiesTotal   >= 4;
        bool okMedals     = medalsTotal     >= 6;
        bool okParchments = parchmentsTotal >= 10;
        bool okMasterKeys = masterKeysTotal >= 3;
        bool okKeys       = keysTotal       >= 500;
        bool okDiamonds   = diamondsTotal   >= 200;

        return okTrophies && okMedals && okParchments && okMasterKeys && okKeys && okDiamonds;
    }

    private void MarcarPanel()
    {
        int trophiesTotal   = PlayerPrefs.GetInt($"{slotId}_trophiesTotal", 0);
        int medalsTotal     = PlayerPrefs.GetInt($"{slotId}_medalsTotal", 0);
        int parchmentsTotal = PlayerPrefs.GetInt($"{slotId}_parchmentsTotal", 0);
        int masterKeysTotal = PlayerPrefs.GetInt($"{slotId}_masterKeysTotal", 0);
        int keysTotal       = PlayerPrefs.GetInt($"{slotId}_keysTotal", 0);
        int diamondsTotal   = PlayerPrefs.GetInt($"{slotId}_diamondsTotal", 0);

        bool mk1 = AchievementsManager.IsUnlocked(slotId, AchievementId.MasterKey1);
        bool mk2 = AchievementsManager.IsUnlocked(slotId, AchievementId.MasterKey2);
        bool mk3 = AchievementsManager.IsUnlocked(slotId, AchievementId.MasterKey3);

        // Trofeos
        SetActiveSafe(trophyGreen, trophiesTotal >= 1);
        SetActiveSafe(trophyBlue,  trophiesTotal >= 2);
        SetActiveSafe(trophyRed,   trophiesTotal >= 3);
        SetActiveSafe(trophyGold,  trophiesTotal >= 4);

        // Condecoraciones (usa logros de rango como base)
        int medalsByRanks = CountMedalsFromRanks();
        if (medalsByRanks > medalsTotal) medalsTotal = medalsByRanks;

        SetActiveSafe(medal1, HasRank_R40()   || medalsTotal >= 1);
        SetActiveSafe(medal2, HasRank_R60()   || medalsTotal >= 2);
        SetActiveSafe(medal3, HasRank_R100()  || medalsTotal >= 3);
        SetActiveSafe(medal4, HasRank_R130()  || medalsTotal >= 4);
        SetActiveSafe(medal5, HasRank_R170()  || medalsTotal >= 5);
        SetActiveSafe(medal6, HasRank_R200()  || medalsTotal >= 6);

        // Master Keys
        SetActiveSafe(crownOfTheKey, mk1 || masterKeysTotal >= 1);
        SetActiveSafe(soulColumn, mk2 || masterKeysTotal >= 2);
        SetActiveSafe(toothOfTheKingdom, mk3 || masterKeysTotal >= 3);
    }

    private void SetActiveSafe(GameObject go, bool on)
    {
        if (go) go.SetActive(on);
    }

    public void ClosePanel()
    {
        if (panelDoorClosed) panelDoorClosed.SetActive(false);
    }

    public void OnAllRightClick() => ClosePanel();
}
