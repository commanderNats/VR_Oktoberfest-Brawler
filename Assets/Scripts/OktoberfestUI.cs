using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Un solo controlador para los 5 paneles de OKB_BarMap.
// No modifica tus scripts CubePool, Rotation ni UIController.
public class OktoberfestUI : MonoBehaviour
{
    [Header("Paneles (hijos del Canvas XR)")]
    public GameObject menuPanel;
    public GameObject hudPanel;
    public GameObject settingsPanel;
    public GameObject winPanel;
    public GameObject failPanel;

    [Header("Elementos de interfaz")]
    public Slider barraVida;
    public TMP_Text textoTimer;           // Usa UI > Legacy > Text
    public Slider sliderVolumen;
    public GameObject botonReiniciarSettings; // Opcional

    [Header("Valores del juego")]
    [Min(1f)] public float vidaMaxima = 100f;
    [Min(1f)] public float duracionPartida = 120f;
    public bool perderCuandoAcabaTiempo = true;

    private enum Pantalla { Menu, Jugando, Settings, Win, Fail }
    private Pantalla pantallaActual;
    private Pantalla pantallaAnterior;
    private float vidaActual;
    private float tiempoRestante;
    private static bool empezarAlRecargar = false;

    // Para comunicar otros scripts con este controlador:
    // OktoberfestUI.Instancia.RecibirDanio(10f);
    public static OktoberfestUI Instancia { get; private set; }

    private void Awake()
    {
        Instancia = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        vidaActual = vidaMaxima;
        tiempoRestante = duracionPartida;

        float volumen = PlayerPrefs.GetFloat("OKB_VolumenGeneral", 1f);
        AudioListener.volume = volumen;
        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.wholeNumbers = false;
            sliderVolumen.SetValueWithoutNotify(volumen);
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
        }

        ActualizarHUD();
        Mostrar(empezarAlRecargar ? Pantalla.Jugando : Pantalla.Menu);
        empezarAlRecargar = false;
    }

    private void Update()
    {
        if (pantallaActual != Pantalla.Jugando) return;

        tiempoRestante = Mathf.Max(0f, tiempoRestante - Time.deltaTime);
        ActualizarHUD();
        if (tiempoRestante <= 0f)
        {
            if (perderCuandoAcabaTiempo) Perder();
            else Ganar();
        }
    }

    private void Mostrar(Pantalla nueva)
    {
        pantallaActual = nueva;
        if (menuPanel != null) menuPanel.SetActive(nueva == Pantalla.Menu);
        if (hudPanel != null) hudPanel.SetActive(nueva == Pantalla.Jugando);
        if (settingsPanel != null) settingsPanel.SetActive(nueva == Pantalla.Settings);
        if (winPanel != null) winPanel.SetActive(nueva == Pantalla.Win);
        if (failPanel != null) failPanel.SetActive(nueva == Pantalla.Fail);

        // Detiene timer y fisicas cuando aparece cualquier menu.
        Time.timeScale = nueva == Pantalla.Jugando ? 1f : 0f;
    }

    private void ActualizarHUD()
    {
        if (barraVida != null)
        {
            barraVida.minValue = 0f;
            barraVida.maxValue = vidaMaxima;
            barraVida.SetValueWithoutNotify(vidaActual);
        }
        if (textoTimer != null)
        {
            int segundos = Mathf.CeilToInt(tiempoRestante);
            textoTimer.text = $"{segundos / 60:00}:{segundos % 60:00}";
        }
    }

    // Boton START
    public void Empezar()
    {
        vidaActual = vidaMaxima;
        tiempoRestante = duracionPartida;
        ActualizarHUD();
        Mostrar(Pantalla.Jugando);
    }

    // Boton SETTINGS, tanto desde menu como desde HUD
    public void AbrirSettings()
    {
        pantallaAnterior = pantallaActual;
        if (botonReiniciarSettings != null)
            botonReiniciarSettings.SetActive(pantallaAnterior == Pantalla.Jugando);
        Mostrar(Pantalla.Settings);
    }

    // Boton BACK de SETTINGS
    public void CerrarSettings()
    {
        PlayerPrefs.Save();
        Mostrar(pantallaAnterior);
    }

    // Evento del slider; controla todo el audio, musica y efectos.
    public void CambiarVolumen(float volumen)
    {
        volumen = Mathf.Clamp01(volumen);
        AudioListener.volume = volumen;
        PlayerPrefs.SetFloat("OKB_VolumenGeneral", volumen);
    }

    // Llamar desde el script que detecte golpes al jugador.
    public void RecibirDanio(float cantidad)
    {
        if (pantallaActual != Pantalla.Jugando) return;
        vidaActual = Mathf.Clamp(vidaActual - Mathf.Max(0f, cantidad), 0f, vidaMaxima);
        ActualizarHUD();
        if (vidaActual <= 0f) Perder();
    }

    // Llamar desde tu mecanica de victoria.
    public void Ganar()
    {
        if (pantallaActual == Pantalla.Jugando) Mostrar(Pantalla.Win);
    }

    // Se activa si la vida o el tiempo llegan a cero (segun configuracion).
    public void Perder()
    {
        if (pantallaActual == Pantalla.Jugando) Mostrar(Pantalla.Fail);
    }

    // Botones PLAY AGAIN / TRY AGAIN / RESTART
    public void Reiniciar()
    {
        PlayerPrefs.Save();
        empezarAlRecargar = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Botones MAIN MENU
    public void VolverAlMenu()
    {
        PlayerPrefs.Save();
        empezarAlRecargar = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Boton QUIT. En el editor detiene Play; en el build cierra la app.
    public void Salir()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
