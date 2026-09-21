using UnityEngine;

public class DetectorDePisada : MonoBehaviour
{
    public GerenciadorDePontos gerenciador;
    public AudioSource fonteDeAudio;
    public AudioClip somAcerto;

    private float tempoUltimoPonto = 0f;
    public float tempoDeEspera = 2.0f;

    void Start()
    {
        if (fonteDeAudio == null) fonteDeAudio = gameObject.AddComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider outro)
    {
        if (Time.time - tempoUltimoPonto > tempoDeEspera)
        {
            if (outro.CompareTag("Checkpoint"))
            {
                // Apenas avisa o gerenciador e toca o som. A UI é problema do Gerenciador agora!
                gerenciador.AdicionarPontos(1);
                TocarSom(somAcerto);
                tempoUltimoPonto = Time.time;
            }
        }
    }

    void TocarSom(AudioClip som)
    {
        if (fonteDeAudio != null && som != null) fonteDeAudio.PlayOneShot(som);
    }
}