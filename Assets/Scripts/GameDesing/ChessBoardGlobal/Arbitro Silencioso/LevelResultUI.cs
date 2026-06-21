using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelResultUI : MonoBehaviour
{
    public static LevelResultUI Instance { get; private set; }

    [Header("Donaciones")]
    public GameObject donationPanel;

    [Header("Referencias UI")]
    public GameObject resultPanel;

    [Header("Llave Maestra (niveles 201–203)")]
    public GameObject MasterKey;
    public GameObject MasterKeyEnabled;
    public GameObject MasterKeyTextObtenida;
    public GameObject MasterKeyTextNoObtenida;

    [Header("Condecoracion")]
    public GameObject Condecoracion;
    public GameObject CondecoracionEnabled;
    public GameObject CondecorationTextObtenida;
    public GameObject CondecorationTextNoObtenida;

    [Header("Ganar o perder")]
    public GameObject youWinText;
    public GameObject youLoseText;

    [Header("Pergamino")]
    public GameObject ParchmentText;
    public GameObject Parchment2Text;
    public GameObject Pergamino;
    public GameObject PergaminoEnabled;

    [Header("Trofeo")]
    public GameObject Trophy;
    public GameObject TrophyEnabled;
    public GameObject TrophyTextObtenido;
    public GameObject TrophyTextNoObtenido;

    [Header("Llaves")]
    public TMP_Text keysText;
    public GameObject key1;
    public GameObject key2;
    public GameObject key3;
    public GameObject key1Enabled;
    public GameObject key2Enabled;
    public GameObject key3Enabled;

    [Header("Diamantes")]
    public GameObject diamond;
    public GameObject diamondEnabled;
    public GameObject diamond2;
    public GameObject diamond2Enabled;


    [Header("Vidas")]
    public TMP_Text livesText;
    public GameObject up;
    public GameObject upEnabled;

    [Header("Score")]
    public TMP_Text scoreText;

    [Header("Tiempo")]
    public TMP_Text timeLabelText;
    public TMP_Text timeValueText;

    [Header("Botones")]
    public GameObject nextLevelButton;
    public GameObject tryAgainButton;
    public GameObject backToHomeButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        resultPanel.SetActive(false);
    }

    // 🔹 Ahora diamondsCollected es int
    public void ShowResults(int keysCollected, int diamondsCollected, int livesRemaining, int totalScore)
    {
        gameObject.SetActive(true);

        if (livesRemaining <= 0)
        {
            StartCoroutine(MostrarResultadosConRetraso(keysCollected, diamondsCollected, livesRemaining, totalScore, 0.0001f));
        }
        else
        {
            MostrarResultadosSegunNivel(keysCollected, diamondsCollected, livesRemaining, totalScore);
        }
    }

    private IEnumerator MostrarResultadosConRetraso(int keysCollected, int diamondsCollected, int livesRemaining, int totalScore, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        MostrarResultadosSegunNivel(keysCollected, diamondsCollected, livesRemaining, totalScore);
    }

    private void MostrarResultadosSegunNivel(int keysCollected, int diamondsCollected, int livesRemaining, int totalScore)
{
    const int OFFSET_NIVELES = 14;

    string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
    int buildIndex = SceneManager.GetActiveScene().buildIndex;
    int nivelLogico = buildIndex - OFFSET_NIVELES;

    Debug.Log($"[LevelResultUI] Resultados para Nivel lógico {nivelLogico} (BuildIndex={buildIndex})");

    // 🧠 Saber si hubo victoria real (vidas > 0)
    bool gano = livesRemaining > 0;

    // 🔍 Leer si antes ya estaba marcado como completado
    string levelCompletedKey = $"{slotActivo}_level_{nivelLogico}_completed";
    int prevCompleted = PlayerPrefs.GetInt(levelCompletedKey, 0);

    // … 👇 se mantiene la lógica de pergamino, trofeo, etc.
    if (LevelProgress.Instance != null && LevelProgress.Instance.esNivelPergamino)
    {
        Debug.Log("📜 Cargando resultados de PERGAMINO...");
        ShowParchmentResult(totalScore, livesRemaining, LevelProgress.Instance.hasParchment);
        ProgressSaver.GuardarNivelPergamino(slotActivo, nivelLogico, totalScore, LevelProgress.Instance.hasParchment);
    }
    else if (LevelProgress.Instance != null && LevelProgress.Instance.esNivelTrofeo)
    {
        Debug.Log("🏆 Cargando resultados de TROFEO...");
        ShowTrophyResult(totalScore, livesRemaining, LevelProgress.Instance.hasTrophy);
        ProgressSaver.GuardarNivelTrofeo(slotActivo, nivelLogico, totalScore, LevelProgress.Instance.hasTrophy);
    }
    else if (LevelProgress.Instance != null && LevelProgress.Instance.esNivelMedalla)
    {
        Debug.Log("🎖 Cargando resultados de CONDECORACIÓN...");
        ShowMedalResult(totalScore, livesRemaining, LevelProgress.Instance.hasMedal);
        ProgressSaver.GuardarNivelMedalla(slotActivo, nivelLogico, totalScore, LevelProgress.Instance.hasMedal);
    }
    else if (LevelProgress.Instance != null && LevelProgress.Instance.esNivelMasterKey)
    {
        Debug.Log("🗝️ Cargando resultados de MASTER KEY...");
        ShowMasterKeyResult(totalScore, livesRemaining, LevelProgress.Instance.hasMasterKey3);
        ProgressSaver.GuardarNivelMasterKey(slotActivo, nivelLogico, totalScore, LevelProgress.Instance.hasMasterKey3);
    }
    else
    {
        Debug.Log("🔑 Cargando resultados de NIVEL NORMAL...");
        int keys = keysCollected;
        int diamonds = diamondsCollected;

        MostrarResultadosInmediatos(keys, diamonds, livesRemaining, totalScore);
        ProgressSaver.GuardarNivelNormal(slotActivo, nivelLogico, totalScore, keys, diamonds);
    }

    // 🔹 Recuperar configuración de este nivel desde PlayerPrefs
    int maxKeys = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_maxKeys",
                                     LevelProgress.Instance != null ? LevelProgress.Instance.maxKeys : 3);
    int maxDiamonds = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_maxDiamonds",
                                         LevelProgress.Instance != null ? LevelProgress.Instance.maxDiamonds : 1);

    // ✅ Usar la misma validación que GameHomeManager (para el DIAMANTICO del mapa)
    bool obtuvoObjetoClave = GameHomeManager.Instance.ValidarObjetoClave(
        keysCollected,
        diamondsCollected,
        maxKeys,
        maxDiamonds,
        LevelProgress.Instance != null && LevelProgress.Instance.hasParchment,
        LevelProgress.Instance != null && LevelProgress.Instance.hasTrophy,
        LevelProgress.Instance != null && LevelProgress.Instance.hasMedal,
        LevelProgress.Instance != null && LevelProgress.Instance.hasMasterKey3
    );

    Debug.Log($"[LevelResultUI] Verificación -> Keys={keysCollected}, Diamonds={diamondsCollected}, ObtuvoObjetoClave={obtuvoObjetoClave}, Gano={gano}");

    // 🎯 REGLA CENTRAL:
    //   • Si ya estaba completado antes (prevCompleted == 1), nunca lo descompletes.
    //   • Si antes NO estaba completado, solo marcarlo completado si hubo victoria.
    if (prevCompleted == 1)
    {
        PlayerPrefs.SetInt(levelCompletedKey, 1);
    }
    else
    {
        PlayerPrefs.SetInt(levelCompletedKey, gano ? 1 : 0);
    }

    // 🧱 Si NO hubo victoria, NO se desbloquea el siguiente nivel
    if (!gano)
    {
        PlayerPrefs.Save();
        // En derrota solo se muestra la UI, pero no se toca nivelMax ni el mapa.
        return;
    }

    // 🏁 A partir de aquí, sólo entra si hubo victoria (Rey vivo en 7,7)

    int nivelMaxAntes = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);
    if (nivelLogico + 1 > nivelMaxAntes)
        PlayerPrefs.SetInt(slotActivo + "_nivelMax", nivelLogico + 1);

    // (Opcional) marcar “diamond” de perfección si quieres:
    // if (obtuvoObjetoClave)
    //     PlayerPrefs.SetInt(slotActivo + "_level_" + nivelLogico + "_diamond", 1);

    PlayerPrefs.Save();

    // ✅ Reflejar en GameHome (sólo cuando GANÓ)
    if (GameHomeManager.Instance != null)
    {
        GameHomeManager.Instance.MarcarNivelCompletado(nivelLogico, obtuvoObjetoClave);
    }
}





    private void MostrarResultadosInmediatos(int keysCollected, int diamondsCollected, int livesRemaining, int totalScore)
    {
        float tiempoJugado = FindFirstObjectByType<ChessGameManager>().GetTiempoNivelAcumulado();
        int minutos = Mathf.FloorToInt(tiempoJugado / 60f);
        int segundos = Mathf.FloorToInt(tiempoJugado % 60f);

        timeValueText.text = $"{minutos:D2}:{segundos:D2}";

        key1Enabled.SetActive(true);
        key2Enabled.SetActive(true);
        key3Enabled.SetActive(true);

        key1.SetActive(keysCollected >= 1);
        key2.SetActive(keysCollected >= 2);
        key3.SetActive(keysCollected >= 3);

        // 🔹 Reset diamantes
        if (diamond != null) diamond.SetActive(false);
        if (diamondEnabled != null) diamondEnabled.SetActive(false);
        if (diamond2 != null) diamond2.SetActive(false);
        if (diamond2Enabled != null) diamond2Enabled.SetActive(false);

        // 🔹 Leer cuántos diamantes debería tener este nivel
        const int OFFSET_NIVELES = 14;
        string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        int nivelLogico = buildIndex - OFFSET_NIVELES;

        int maxDiamonds = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_maxDiamonds",
                                             LevelProgress.Instance != null ? LevelProgress.Instance.maxDiamonds : 1);

        if (maxDiamonds == 1)
        {
            // Nivel de 1 diamante
            if (diamondEnabled != null) diamondEnabled.SetActive(true);
            if (diamond != null) diamond.SetActive(diamondsCollected >= 1);
        }
        else if (maxDiamonds == 2)
        {
            // Nivel de 2 diamantes
            if (diamondEnabled != null) diamondEnabled.SetActive(true);
            if (diamond != null) diamond.SetActive(diamondsCollected >= 1);

            if (diamond2Enabled != null) diamond2Enabled.SetActive(true);
            if (diamond2 != null) diamond2.SetActive(diamondsCollected >= 2);
        }

        upEnabled.SetActive(true);
        up.SetActive(livesRemaining > 0);
        livesText.text = $"{livesRemaining}";

        scoreText.text = $"{totalScore}";

        bool gano = livesRemaining > 0;
        youWinText.SetActive(gano);
        youLoseText.SetActive(!gano);

        if (gano)
        {
            MostrarBoton(nextLevelButton, true);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);
        }
        else
        {
            MostrarBoton(nextLevelButton, false);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);
        }

        resultPanel.SetActive(true);
        Debug.Log($"🎉 Resultados -> Llaves: {keysCollected}, Diamantes: {diamondsCollected}/{maxDiamonds}, Vidas: {livesRemaining}, Score: {totalScore}, Tiempo: {minutos:D2}:{segundos:D2}");
    }



    // … 👇 aquí permanecen ShowParchmentResult, ShowTrophyResult, ShowMedalResult, ShowMasterKeyResult, etc.
    public void ShowMedalResult(int totalScore, int livesRemaining, bool medalObtenida)
    {
        // 🔹 Ocultar lo que no aplica
        key1.SetActive(false);
        key2.SetActive(false);
        key3.SetActive(false);
        key1Enabled.SetActive(false);
        key2Enabled.SetActive(false);
        key3Enabled.SetActive(false);

        Pergamino.SetActive(false);
        PergaminoEnabled.SetActive(false);
        ParchmentText.SetActive(false);
        Parchment2Text.SetActive(false);

        Trophy.SetActive(false);
        TrophyEnabled.SetActive(false);


        // 🔹 Mostrar condecoración según el estado
        CondecoracionEnabled.SetActive(true);
        Condecoracion.SetActive(medalObtenida);                 // Solo visible si la obtuvo
        CondecorationTextObtenida.SetActive(medalObtenida);
        CondecorationTextNoObtenida.SetActive(!medalObtenida);

        // 🔹 Tiempo
        float tiempoJugado = FindFirstObjectByType<ChessGameManager>().GetTiempoNivelAcumulado();
        int minutos = Mathf.FloorToInt(tiempoJugado / 60f);
        int segundos = Mathf.FloorToInt(tiempoJugado % 60f);
        timeValueText.text = $"{minutos:D2}:{segundos:D2}";

        // 🔹 Vidas
        upEnabled.SetActive(true);
        up.SetActive(livesRemaining > 0);
        livesText.text = $"{livesRemaining}";

        // 🔹 Score
        scoreText.text = $"{totalScore}";

        // 🔹 Ganó o perdió
        bool gano = livesRemaining > 0;
        youWinText.SetActive(gano);
        youLoseText.SetActive(!gano);

        // 🔹 Botones (igual que antes)
        if (gano)
        {
            MostrarBoton(nextLevelButton, true);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            nextLevelButton.transform.SetSiblingIndex(0);
            tryAgainButton.transform.SetSiblingIndex(1);
            backToHomeButton.transform.SetSiblingIndex(2);
        }
        else
        {
            MostrarBoton(nextLevelButton, false);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            tryAgainButton.transform.SetAsFirstSibling();
            backToHomeButton.transform.SetSiblingIndex(1);
            nextLevelButton.transform.SetSiblingIndex(2);
        }

        resultPanel.SetActive(true);

        Debug.Log($"🎖 Resultado Condecoración -> {(medalObtenida ? "Obtenida" : "No obtenida")}, Vidas: {livesRemaining}, Score: {totalScore}, Tiempo: {minutos:D2}:{segundos:D2}");
    }

    public void ShowParchmentResult(int totalScore, int livesRemaining, bool pergaminoObtenido)
    {
        // 🔹 Ocultar lo que no aplica
        key1.SetActive(false);
        key2.SetActive(false);
        key3.SetActive(false);
        key1Enabled.SetActive(false);
        key2Enabled.SetActive(false);
        key3Enabled.SetActive(false);

        Trophy.SetActive(false);
        TrophyEnabled.SetActive(false);


        Condecoracion.SetActive(false);
        CondecoracionEnabled.SetActive(false);


        // 🔹 Mostrar pergamino según el estado
        PergaminoEnabled.SetActive(true);
        Pergamino.SetActive(pergaminoObtenido);     // Solo visible si lo obtuvo
        ParchmentText.SetActive(pergaminoObtenido); // Texto “obtenido”
        Parchment2Text.SetActive(!pergaminoObtenido); // Texto “no obtenido”

        // 🔹 Tiempo
        float tiempoJugado = FindFirstObjectByType<ChessGameManager>().GetTiempoNivelAcumulado();
        int minutos = Mathf.FloorToInt(tiempoJugado / 60f);
        int segundos = Mathf.FloorToInt(tiempoJugado % 60f);
        timeValueText.text = $"{minutos:D2}:{segundos:D2}";

             

        // 🔹 Vidas
        upEnabled.SetActive(true);
        up.SetActive(livesRemaining > 0);
        livesText.text = $"{livesRemaining}";

        // 🔹 Score
        scoreText.text = $"{totalScore}";

        // 🔹 Ganó o perdió
        bool gano = livesRemaining > 0;
        youWinText.SetActive(gano);
        youLoseText.SetActive(!gano);

        // 🔹 Botones (igual que antes)
        if (gano)
        {
            MostrarBoton(nextLevelButton, true);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            nextLevelButton.transform.SetSiblingIndex(0);
            tryAgainButton.transform.SetSiblingIndex(1);
            backToHomeButton.transform.SetSiblingIndex(2);
        }
        else
        {
            MostrarBoton(nextLevelButton, false);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            tryAgainButton.transform.SetAsFirstSibling();
            backToHomeButton.transform.SetSiblingIndex(1);
            nextLevelButton.transform.SetSiblingIndex(2);
        }

        resultPanel.SetActive(true);

        Debug.Log($"📜 Resultado Pergamino -> {(pergaminoObtenido ? "Obtenido" : "No obtenido")}, Vidas: {livesRemaining}, Score: {totalScore}, Tiempo: {minutos:D2}:{segundos:D2}");
    }

     public void ShowTrophyResult(int totalScore, int livesRemaining, bool trophyObtenido)
    {
        // 🔹 Ocultar lo que no aplica
        key1.SetActive(false);
        key2.SetActive(false);
        key3.SetActive(false);
        key1Enabled.SetActive(false);
        key2Enabled.SetActive(false);
        key3Enabled.SetActive(false);

        Pergamino.SetActive(false);
        PergaminoEnabled.SetActive(false);
        ParchmentText.SetActive(false);
        Parchment2Text.SetActive(false);

        Condecoracion.SetActive(false);
        CondecoracionEnabled.SetActive(false);


        // 🔹 Mostrar trofeo según el estado
        TrophyEnabled.SetActive(true);
        Trophy.SetActive(trophyObtenido);              // Solo visible si lo obtuvo
        TrophyTextObtenido.SetActive(trophyObtenido);
        TrophyTextNoObtenido.SetActive(!trophyObtenido);



        // 🔹 Tiempo
        float tiempoJugado = FindFirstObjectByType<ChessGameManager>().GetTiempoNivelAcumulado();
        int minutos = Mathf.FloorToInt(tiempoJugado / 60f);
        int segundos = Mathf.FloorToInt(tiempoJugado % 60f);
        timeValueText.text = $"{minutos:D2}:{segundos:D2}";
          

        // 🔹 Vidas
        upEnabled.SetActive(true);
        up.SetActive(livesRemaining > 0);
        livesText.text = $"{livesRemaining}";

        // 🔹 Score
        scoreText.text = $"{totalScore}";

        // 🔹 Ganó o perdió
        bool gano = livesRemaining > 0;
        youWinText.SetActive(gano);
        youLoseText.SetActive(!gano);

        // 🔹 Botones (igual que antes)
        if (gano)
        {
            MostrarBoton(nextLevelButton, true);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            nextLevelButton.transform.SetSiblingIndex(0);
            tryAgainButton.transform.SetSiblingIndex(1);
            backToHomeButton.transform.SetSiblingIndex(2);
        }
        else
        {
            MostrarBoton(nextLevelButton, false);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            tryAgainButton.transform.SetAsFirstSibling();
            backToHomeButton.transform.SetSiblingIndex(1);
            nextLevelButton.transform.SetSiblingIndex(2);
        }

        resultPanel.SetActive(true);

        Debug.Log($"🏆 Resultado Trofeo -> {(trophyObtenido ? "Obtenido" : "No obtenido")}, Vidas: {livesRemaining}, Score: {totalScore}, Tiempo: {minutos:D2}:{segundos:D2}");
    }

    public void ShowMasterKeyResult(int totalScore, int livesRemaining, bool masterKeyObtenida)
    {
        // 🔹 Ocultar lo que no aplica
        key1.SetActive(false);
        key2.SetActive(false);
        key3.SetActive(false);
        key1Enabled.SetActive(false);
        key2Enabled.SetActive(false);
        key3Enabled.SetActive(false);

        Pergamino.SetActive(false);
        PergaminoEnabled.SetActive(false);
        ParchmentText.SetActive(false);
        Parchment2Text.SetActive(false);

        Trophy.SetActive(false);
        TrophyEnabled.SetActive(false);
        TrophyTextObtenido.SetActive(false);
        TrophyTextNoObtenido.SetActive(false);

        Condecoracion.SetActive(false);
        CondecoracionEnabled.SetActive(false);
        CondecorationTextObtenida.SetActive(false);
        CondecorationTextNoObtenida.SetActive(false);

        diamondEnabled.SetActive(false);
        diamond.SetActive(false);

        // 🔹 Mostrar MasterKey
        MasterKeyEnabled.SetActive(true);
        MasterKey.SetActive(masterKeyObtenida);
        MasterKeyTextObtenida.SetActive(masterKeyObtenida);
        MasterKeyTextNoObtenida.SetActive(!masterKeyObtenida);

        // 🔹 Tiempo
        float tiempoJugado = FindFirstObjectByType<ChessGameManager>().GetTiempoNivelAcumulado();
        int minutos = Mathf.FloorToInt(tiempoJugado / 60f);
        int segundos = Mathf.FloorToInt(tiempoJugado % 60f);
        timeValueText.text = $"{minutos:D2}:{segundos:D2}";

        // 🔹 Vidas
        upEnabled.SetActive(true);
        up.SetActive(livesRemaining > 0);
        livesText.text = $"{livesRemaining}";

        // 🔹 Score
        scoreText.text = $"{totalScore}";

        // 🔹 Ganó o perdió
        bool gano = livesRemaining > 0;
        youWinText.SetActive(gano);
        youLoseText.SetActive(!gano);

        // 🔹 Botones
        if (gano)
        {
            MostrarBoton(nextLevelButton, true);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            nextLevelButton.transform.SetSiblingIndex(0);
            tryAgainButton.transform.SetSiblingIndex(1);
            backToHomeButton.transform.SetSiblingIndex(2);
        }
        else
        {
            MostrarBoton(nextLevelButton, false);
            MostrarBoton(tryAgainButton, true);
            MostrarBoton(backToHomeButton, true);

            tryAgainButton.transform.SetAsFirstSibling();
            backToHomeButton.transform.SetSiblingIndex(1);
            nextLevelButton.transform.SetSiblingIndex(2);
        }

        resultPanel.SetActive(true);

        Debug.Log($"🗝️ Resultado MasterKey -> {(masterKeyObtenida ? "Parte obtenida" : "No obtenida")}, Vidas: {livesRemaining}, Score: {totalScore}, Tiempo: {minutos:D2}:{segundos:D2}");
    }
    private void MostrarBoton(GameObject boton, bool visible)
    {
        if (boton.TryGetComponent(out CanvasGroup cg))
        {
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
    }

    public void OnTryAgainClicked() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void OnBackToHomeClicked() => SceneManager.LoadScene(4);
    public void OnNextLevelClicked() => donationPanel.SetActive(true);
}
