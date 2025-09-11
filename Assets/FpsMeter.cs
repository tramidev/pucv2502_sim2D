using TMPro;
using UnityEngine;

public class FpsMeter : MonoBehaviour
{
    private TextMeshProUGUI textmesh;
    
 
    private float _hudRefreshRate = 1f;
    private float _timer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textmesh = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.unscaledTime > _timer)
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            textmesh.text = "FPS: " + fps;
            _timer = Time.unscaledTime + _hudRefreshRate;
        }
    }
}
