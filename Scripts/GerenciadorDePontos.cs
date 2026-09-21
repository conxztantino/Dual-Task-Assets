using UnityEngine;
using TMPro;
using System.Collections;

public class GerenciadorDePontos : MonoBehaviour
{
    [Header("Configurações")]
    public int pontuacaoTotal = 0;

    [Header("Interface Visual (VR)")]
    // Essa variável vai guardar o texto que o paciente vai ver
    public TextMeshProUGUI textoPlacar;

    void Start()
    {
        AtualizarInterface();
        AncorarObjetosDoCenario();
    }

    private void AncorarObjetosDoCenario()
    {
        // Procura por objetos chave na cena que precisam ficar estáticos no mundo real
        GameObject obstaculos = GameObject.Find("Obstáculos");
        if (obstaculos != null && obstaculos.GetComponent<OVRSpatialAnchor>() == null)
        {
            obstaculos.AddComponent<OVRSpatialAnchor>();
            Debug.Log("OVRSpatialAnchor adicionado ao grupo de Obstáculos.");
        }

        // Se houver um objeto 'Canvas' flutuante, também ancoramos
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.GetComponent<OVRSpatialAnchor>() == null)
        {
            canvas.AddComponent<OVRSpatialAnchor>();
            Debug.Log("OVRSpatialAnchor adicionado ao Canvas.");
        }
    }

    public void AdicionarPontos(int pontos)
    {
        pontuacaoTotal += pontos;
        AtualizarInterface();
    }

    public void RemoverPontos(int pontos)
    {
        pontuacaoTotal -= pontos;

        // Impede que a pontuação fique negativa (opcional)
        if (pontuacaoTotal < 0)
        {
            pontuacaoTotal = 0;
        }

        AtualizarInterface();
    }

    private void AtualizarInterface()
    {
        if (textoPlacar != null)
        {
            textoPlacar.text = $"Pontos: {pontuacaoTotal}";
        }

        // Aqui você pode depois conectar com um TextMeshPro no VR para o paciente ver!
        Debug.Log($"PONTUAÇÃO ATUAL: {pontuacaoTotal}");
    }
}