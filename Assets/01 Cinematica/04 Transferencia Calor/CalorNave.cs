using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class CalorNave : MonoBehaviour
{
    [Header("Estado térmico inicial")]
    [SerializeField] private float temperaturaK = 30f;

    [Header("Propiedades de la nave")]
    [SerializeField] private float masa = 100f;
    [SerializeField] private float calorEspecifico = 900f;
    [SerializeField] private float areaEfectiva = 10f;
    [Range(0f, 1f)] [SerializeField] private float emisividad = 0.8f;

    [Header("Pérdidas/ganancias base")]
    [SerializeField] private float temperaturaEspacioK = 3f;
    [SerializeField] private float potenciaConstanteW = -50f;
    
    // Stefan–Boltzmann
    private const float Sigma = 5.670374419e-8f; 

    private SpriteRenderer sr;
    
    public List<CalorPlaneta> planetasEnRadio = new List<CalorPlaneta>();

    public float TemperaturaK => temperaturaK;

    public TextMeshPro tempText;
    
    private static CalorNave _instance;

    public float speed;
    
    private Vector2 _velocity;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        _instance = this;
        _velocity = speed*Vector2.right;
    }

    private void Update()
    {
        // 1) Radiación hacia el espacio
        float tShip4 = Pow4(temperaturaK);
        float qdotEspacio = emisividad * Sigma * areaEfectiva * (Pow4(temperaturaEspacioK) - tShip4);

        // 2) Intercambio radiativo solo con los planetas actualmente en radio
        float qdotPlanetas = 0f;
        foreach (var p in planetasEnRadio)
        {
            if (p == null) continue;
            float qdot = p.EficienciaRadiativaHaciaNave(emisividad) * Sigma * areaEfectiva *
                         (Pow4(p.TemperaturaK) - tShip4);
            qdotPlanetas += qdot;
        }

        // 3) Sumatoria total
        float qdotTotal = qdotEspacio + qdotPlanetas + potenciaConstanteW;

        // 4) Integrar temperatura
        float capacidadTermica = Mathf.Max(1e-6f, masa * calorEspecifico);
        temperaturaK = Mathf.Max(0.01f, temperaturaK + (qdotTotal / capacidadTermica) * Time.deltaTime);
        
        
        tempText.text = ((int)temperaturaK).ToString();
        
        //movimiento
        transform.position += (Vector3)_velocity * Time.deltaTime;
    }

    // Agregado por el planeta al entrar
    public void AgregarPlanetaEnRadio(CalorPlaneta planeta)
    {
        if (!planetasEnRadio.Contains(planeta))
            planetasEnRadio.Add(planeta);
    }

    // Removido por el planeta al salir
    public void QuitarPlanetaEnRadio(CalorPlaneta planeta)
    {
        planetasEnRadio.Remove(planeta);
    }

    private static float Pow4(float x)
    {
        float x2 = x * x;
        return x2 * x2;
    }

    public static CalorNave GetInstance()
    {
        return _instance;
    }
}
