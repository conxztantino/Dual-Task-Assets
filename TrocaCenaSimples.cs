using UnityEngine;
using UnityEngine.SceneManagement;

public class TrocaCenaSimples : MonoBehaviour
{
    [SerializeField] private float tempoDeEspera = 3f;

    private void Start()
    {
        // Chama a função "MudarDeCena" após o tempo determinado
        Invoke("MudarDeCena", tempoDeEspera);
    }

    private void MudarDeCena()
    {
        // Pega o número da cena atual e soma + 1 para ir para a próxima
        int proximaCena = SceneManager.GetActiveScene().buildIndex + 1;
        
        SceneManager.LoadScene(proximaCena);
    }
}