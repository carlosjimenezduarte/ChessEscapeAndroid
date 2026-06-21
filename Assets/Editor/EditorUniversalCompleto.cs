/*using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true)]
public class EditorUniversalRecolectable : Editor
{
    public override void OnInspectorGUI()
    {
        MonoBehaviour mono = (MonoBehaviour)target;

        var esRecolectable = mono is IObjetoRecoleccionable;
        var esEspecial = mono is IObjetoRecoleccionableEspecial;
        var esFicha = mono is IFicha;
        var esInmovil = mono is IFichaInmovil;

        var tipo = mono.GetType();
        var vieneDelFuturoField = tipo.GetField("vieneDelFuturo");
        var turnoApareceField = tipo.GetField("turnoAparece");
        var tileCoordsFuturosField = tipo.GetField("tileCoordsFuturosInciertos");
        var posicionRealField = tipo.GetField("posicionReal");

        var piecePositioner = mono.GetComponent<PiecePositioner>();
        var movable = mono.GetComponent<MovableTileObject>();
        var conPosicion = mono as IPieceWithPosition;

        // ------------------------------------------------------------------
        // 🟡 OBJETOS RECOLECTABLES ESPECIALES
        // ------------------------------------------------------------------
        if (esRecolectable && esEspecial)
        {
            EditorGUILayout.LabelField("💎 Objeto Recolectable Especial", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Este objeto especial aparece desde el inicio y no puede ser reemplazado por objetos del futuro.", MessageType.Info);

            MostrarCampoCoordenadas(piecePositioner, movable, conPosicion, "📍 Posición Inicial");
            
            if (GUI.changed)
                EditorUtility.SetDirty(mono);

            return;
        }

        // ------------------------------------------------------------------
        // ⭐ OBJETOS RECOLECTABLES (NO especiales)
        // ------------------------------------------------------------------
        if (esRecolectable)
        {
            EditorGUILayout.LabelField("⭐ Configuración de Objeto Recolectable", EditorStyles.boldLabel);

            bool vieneDelFuturo = (bool)vieneDelFuturoField?.GetValue(mono);
            vieneDelFuturo = EditorGUILayout.Toggle("¿Viene del Futuro?", vieneDelFuturo);
            vieneDelFuturoField?.SetValue(mono, vieneDelFuturo);

            if (vieneDelFuturo)
            {
                if (turnoApareceField != null && tileCoordsFuturosField != null && posicionRealField != null)
                {
                    int turno = (int)turnoApareceField.GetValue(mono);
                    turno = EditorGUILayout.IntField("Turno Aparece", turno);
                    turnoApareceField.SetValue(mono, turno);

                    Vector2Int coordsFuturo = (Vector2Int)tileCoordsFuturosField.GetValue(mono);
                    coordsFuturo = EditorGUILayout.Vector2IntField("Posición Futuros Inciertos", coordsFuturo);
                    tileCoordsFuturosField.SetValue(mono, coordsFuturo);

                    Vector2Int posReal = (Vector2Int)posicionRealField.GetValue(mono);
                    posReal = EditorGUILayout.Vector2IntField("Posición Real al aparecer", posReal);
                    posicionRealField.SetValue(mono, posReal);
                }

                if (movable != null)
                {
                    movable.activoEnTablero = false;
                    EditorUtility.SetDirty(movable);
                }
            }
            else
            {
                MostrarCampoCoordenadas(piecePositioner, movable, conPosicion, "📍 Posición en Tablero");

                if (movable != null)
                {
                    movable.activoEnTablero = true;
                    EditorUtility.SetDirty(movable);
                }
            }

            if (GUI.changed)
                EditorUtility.SetDirty(mono);

            return;
        }

        // ------------------------------------------------------------------
        // 🔷 FICHAS (IFicha / IFichaInmovil)
        // ------------------------------------------------------------------
        if (esFicha)
        {
            if (movable == null)
            {
                EditorGUILayout.HelpBox("⚠️ Esta ficha no tiene el componente MovableTileObject.", MessageType.Warning);
                return;
            }

            if (esInmovil)
            {
                if (!movable.esInamovible)
                {
                    movable.esInamovible = true;
                    EditorUtility.SetDirty(movable);
                    Debug.Log($"🔒 {mono.name}: esInamovible fue forzado a TRUE (IFichaInmovil).");
                }
                EditorGUILayout.HelpBox("✅ Este objeto es inamovible por diseño (IFichaInmovil).", MessageType.Info);
            }
            else
            {
                if (movable.esInamovible)
                {
                    movable.esInamovible = false;
                    EditorUtility.SetDirty(movable);
                    Debug.Log($"🔓 {mono.name}: esInamovible fue forzado a FALSE (Ficha movible).");
                }
                EditorGUILayout.HelpBox("ℹ️ Este objeto es una ficha movible (IFicha).", MessageType.Info);
            }

            MostrarCampoCoordenadas(piecePositioner, movable, conPosicion, "📍 Posición Inicial");
            return;
        }

        // 🔚 Si no coincide con ningún caso especial
        DrawDefaultInspector();
    }

    void MostrarCampoCoordenadas(PiecePositioner piecePositioner, MovableTileObject movable, IPieceWithPosition conPosicion, string etiqueta)
    {
        if (piecePositioner == null) return;

        Vector2Int nuevaPos = EditorGUILayout.Vector2IntField(etiqueta, piecePositioner.tileCoords);
        if (nuevaPos != piecePositioner.tileCoords)
        {
            piecePositioner.tileCoords = nuevaPos;
            EditorUtility.SetDirty(piecePositioner);

            if (movable != null)
            {
                movable.tileCoords = nuevaPos;
                EditorUtility.SetDirty(movable);
            }

            if (conPosicion != null)
                conPosicion.SetPosicionActual(nuevaPos);
        }
    }
}
*/