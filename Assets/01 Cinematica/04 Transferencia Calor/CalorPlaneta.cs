using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class CalorPlaneta : MonoBehaviour
{
    // ===== Intercambio térmico =====
    //private static readonly List<PlanetaTemperatura> planetasActivos = new List<PlanetaTemperatura>();
    //public static IReadOnlyList<PlanetaTemperatura> PlanetasActivos => planetasActivos;

    [Header("Estado térmico")]
    [Tooltip("Temperatura del planeta en Kelvin.")]
    [SerializeField] private float temperaturaK = 500f;

    [Header("Propiedades Planeta")]
    [Range(0f, 20f)] [SerializeField] private float radio = 1f;
    
    [Header("Propiedades radiativas")]
    [Range(0f, 1f)] [SerializeField] private float emisividad = 0.9f;
    [SerializeField] private float areaEfectiva = 1000f;

    [Header("Influencia térmica (detección de nave)")]
    [Tooltip("Radio donde el planeta afecta térmicamente a la nave.")]
    [SerializeField] private float radioInfluencia = 8f;

    // ===== Visual de anillos en gameplay =====
    [Header("Anillos térmicos (gameplay, no Gizmos)")]
    [Tooltip("Cantidad de vértices por anillo (más = círculo más suave).")]
    [SerializeField] private int segmentos = 96;
    [Tooltip("Ancho de línea de cada anillo (unidades mundo).")]
    [SerializeField] private float grosorLinea = 0.06f;

    [Tooltip("Transparencia global de los anillos.")]
    [Range(0f, 1f)] [SerializeField] private float alpha = 0.35f;

    [Tooltip("Colores de cada anillo de interior a exterior (4). Si está vacío, se usa Blanco→Amarillo→Naranja→Rojo.")]
    [SerializeField] private Color[] coloresAnillos = new Color[0];

    [Tooltip("Material para los anillos. Si se deja vacío, se crea uno con 'Sprites/Default'.")]
    [SerializeField] private Material materialAnillos;

    public LineRenderer[] anillos = new LineRenderer[4];
    public SpriteRenderer spritePlaneta;
    private CalorNave nave;
    private bool naveDentro = false;

    public float TemperaturaK => temperaturaK;
    public float RadioInfluencia => radioInfluencia;

    public TextMeshPro[] tempText;

    private void OnEnable()
    {
        CrearOActualizarAnillos();
    }

    private void Update()
    {
        if (nave == null)
        {
            nave = CalorNave.GetInstance();
            if (nave == null) return;
        }

        float dist = Vector2.Distance(transform.position, nave.transform.position);

        if (dist <= radioInfluencia && !naveDentro)
        {
            naveDentro = true;
            nave.AgregarPlanetaEnRadio(this);
        }
        else if (dist > radioInfluencia && naveDentro)
        {
            naveDentro = false;
            nave.QuitarPlanetaEnRadio(this);
        }

        // --- Mantener anillos en caso de que cambie el radio o el transform ---
        // (Recalcula si cambias radioInfluencia, segmentos o grosor en runtime)
        //ActualizarGeometriaAnillos();
    }

    // =======================
    //  Radiación hacia la nave
    // =======================
    public float EficienciaRadiativaHaciaNave(float emisividadNave)
    {
        float acoplamiento = Mathf.Clamp01(emisividad * emisividadNave);
        float escalaArea = Mathf.Max(1f, areaEfectiva * 0.001f);
        return acoplamiento * escalaArea;
    }
    
    
    public float TempOnDistance(float distance)
    {
        distance = Mathf.Max(0.001f, distance);

        // 1) Radio emisivo real del sprite en UNIDADES DE MUNDO, no size.x (que depende del draw mode)
        float rSprite = GetSpriteWorldRadius(); // ~ mitad del mayor lado del bounds

        // 2) Intensidad radiativa ∝ (R/d)^2  ->  T_eff ∝ (intensidad)^(1/4) = (R/d)^(1/2)
        float intensidad = (rSprite * rSprite) / (distance * distance); // (R/d)^2

        // 3) Máscara de influencia (1 en el centro, 0 en/beyond el borde), con suavizado
        float realInfluenceRadius = radioInfluencia + rSprite; // borde térmico = influencia + “radio físico”
        float t = Mathf.Clamp01((realInfluenceRadius - distance) / realInfluenceRadius); // 1 en d=0, 0 en d>=R
        float maskSuave = Mathf.SmoothStep(0f, 1f, t);

        // 4) Temperatura efectiva (aplica máscara en potencia cuarta para mantener consistencia energética)
        float tempEfectiva = temperaturaK * Mathf.Pow(intensidad * maskSuave, 0.25f);

        return tempEfectiva;
    }
    
    private float GetSpriteWorldRadius()
    {
        if (spritePlaneta == null)
            spritePlaneta = GetComponent<SpriteRenderer>();

        if (spritePlaneta != null)
        {
            Bounds b = spritePlaneta.bounds;                
            float r = 0.5f * Mathf.Max(b.size.x, b.size.y);
            return Mathf.Max(0.01f, r);
        }
        return 0.1f;
    }
    

    private void CrearOActualizarAnillos()
    {
        ActualizarGeometriaAnillos();
    }
    

    private void ActualizarGeometriaAnillos()
    {
        int count = Mathf.Max(16, segmentos);
        float twoPI = Mathf.PI * 2f;
        float planetRadius = spritePlaneta.bounds.size.x/2;
        float ringNumber = 4;
        float ringFactor =  radioInfluencia/ringNumber;

        for (int i = 0; i < anillos.Length; i++)
        {
            var lr = anillos[i];
            if (lr == null) continue;

            // Radio del anillo i
            float r = planetRadius + (i * ringFactor);
            
            //Texto
            if (i > 0)
            {
                tempText[i].text = ((int)TempOnDistance(r)).ToString();
                tempText[i].transform.localPosition = new Vector3(r, 0, 0);
            }
            else
            {
                tempText[i].text = ((int)temperaturaK).ToString();
            }

            // Generar círculo en espacio local
            for (int j = 0; j < count; j++)
            {
                float t = (j / (float)count) * twoPI;
                float x = Mathf.Cos(t) * r;
                float y = Mathf.Sin(t) * r;
                lr.SetPosition(j, new Vector3(x, y, 0f));
            }
        }
    }

    private void ActualizarPlaneta()
    {
        Vector2 spriteSize = spritePlaneta.sprite.bounds.size;
        
        float escalaDeseada = radio / spriteSize.x;;
        
        spritePlaneta.transform.localScale = new Vector3(escalaDeseada, escalaDeseada, 1f);
    }

#if UNITY_EDITOR
    // Vista rápida en editor al cambiar sliders (sin usar Gizmos para dibujar)
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if(spritePlaneta==null)
            spritePlaneta = GetComponent<SpriteRenderer>();
        ActualizarPlaneta();
        CrearOActualizarAnillos();
    }
#endif
}
