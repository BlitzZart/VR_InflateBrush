using UnityEngine;
using System.Collections;

public class FFT : MonoBehaviour {
    public int samples = 64;
    MeshDeformer deformer;
    AudioSource audio;
    float[] spectrum;

    void Start()
    {
        spectrum = new float[samples];
        audio = GetComponent<AudioSource>();
        if (audio == null) {
            enabled = false;
            return;
        }


        audio.time = 30;
        deformer = GetComponent<MeshDeformer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (audio.isPlaying)
                audio.Pause();
            else
                audio.Play();
        }

        audio.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);


        deformer.ArrayOffests(spectrum);

        //DrawLines(spectrum);
    }

    private void DrawLines(float[] spectrum)
    {
        int i = 1;
        while (i < spectrum.Length - 1)
        {
            Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
            Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
            Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.yellow);
            i++;
        }
    }
}
