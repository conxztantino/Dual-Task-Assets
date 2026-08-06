using UnityEngine;
using TMPro;

public class PointManager : MonoBehaviour
{
    public int pontos = 0;
    public TextMeshProUGUI textoDePontos; 
    
    // Essa variável funciona como um cadeado para evitar pontos duplicados
    private bool jaPontuou = false; 

    void Start()
    {
        AtualizarTexto();
    }

    private void OnTriggerEnter(Collider outroObjeto)
    {
        // Verifica se é o "Player" E se ainda não pontuou nesta área
        if (outroObjeto.CompareTag("Player") && !jaPontuou)
        {
            pontos++; 
            AtualizarTexto(); 
            
            // Tranca o cadeado: ele não ganha mais pontos se continuar encostando
            jaPontuou = true; 
        }
    }

    // (Opcional) Se você quiser que ele possa pontuar de novo caso saia da área e volte
    private void OnTriggerExit(Collider outroObjeto)
    {
        if (outroObjeto.CompareTag("Player"))
        {
            // Abre o cadeado quando o jogador sai da área
            jaPontuou = false; 
        }
    }

    private void AtualizarTexto()
    {
        if (textoDePontos != null)
        {
            textoDePontos.text = "pontos: " + pontos;
        }
    }
}