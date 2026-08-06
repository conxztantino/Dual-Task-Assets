using UnityEngine;

public class StepDetector : MonoBehaviour
{
    [Header("Áudio")]
    public AudioSource fonteDeAudio;
    public AudioClip somAcerto; // Som feliz/moeda
    public AudioClip somErro;   // Som de buzzer/erro

    [Header("Pontuação")]
    public int pontosPorAcerto = 10;
    public int penalidadePorErro = 5;

    [Header("Configuração")]
    public PointManager gerenciador;
    public float tempoDeEspera = 0.5f; // Evita contar a mesma pisada várias vezes

    private float tempoUltimaPisada = 0f;

    private void Start()
    {
        // Se esquecer de colocar um AudioSource, o script cria um automaticamente
        if (fonteDeAudio == null)
        {
            fonteDeAudio = gameObject.AddComponent<AudioSource>();
            fonteDeAudio.playOnAwake = false;
        }
    }

    private void OnTriggerEnter(Collider outro)
    {
        // Verifica se já passou o tempo mínimo desde a última pisada
        if (Time.time - tempoUltimaPisada < tempoDeEspera) return;

        if (outro.CompareTag("areabranca"))
        {
            RegistrarAcerto();
        }
        else if (outro.CompareTag("Obstaculo"))
        {
            RegistrarErro();
        }
    }

    private void RegistrarAcerto()
    {
        tempoUltimaPisada = Time.time;

        if (somAcerto != null)
            fonteDeAudio.PlayOneShot(somAcerto);

        if (gerenciador != null)
        {
            gerenciador.pontos += pontosPorAcerto;
            AtualizarTextoDoGerenciador();
        }

        Debug.Log("<color=green>Pisou na parte branca! +Pontos</color>");
    }

    private void RegistrarErro()
    {
        tempoUltimaPisada = Time.time;

        if (somErro != null)
            fonteDeAudio.PlayOneShot(somErro);

        if (gerenciador != null)
        {
            gerenciador.pontos -= penalidadePorErro;
            if (gerenciador.pontos < 0) gerenciador.pontos = 0;
            AtualizarTextoDoGerenciador();
        }

        Debug.Log("<color=red>Pisou no attention/cone! -Pontos</color>");
    }

    private void AtualizarTextoDoGerenciador()
    {
        if (gerenciador.textoDePontos != null)
            gerenciador.textoDePontos.text = "pontos: " + gerenciador.pontos;
    }
}