using UnityEngine;

public class TrocadorDeRota : MonoBehaviour
{
    [Header("O que deve LIGAR ao passar por aqui?")]
    public GameObject rotaParaLigar;

    [Header("O que deve DESLIGAR ao passar por aqui?")]
    public GameObject rotaParaDesligar;

    void OnTriggerEnter(Collider other)
    {
        // Verifica se foi o paciente que encostou no gatilho invisível
        if (other.CompareTag("Player"))
        {
            if (rotaParaLigar != null)
            {
                rotaParaLigar.SetActive(true);
            }

            if (rotaParaDesligar != null)
            {
                rotaParaDesligar.SetActive(false);
            }
        }
    }
}