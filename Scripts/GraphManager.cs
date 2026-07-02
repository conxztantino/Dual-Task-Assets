using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RealTimeGraphManager : MonoBehaviour
{
    [Header("Configuração do Gráfico")]
    public RectTransform containerGrafico;
    public Sprite pontoSprite;
    public Color corLinha = Color.green;
    
    [Header("Ajustes Visuais")]
    [Range(10, 200)] public int maxPontosVisiveis = 50;
    public float alturaMaximaGrafico = 300f;
    public float escalaValorMaximo = 2.0f; // Representa o limite máximo em metros (ex: passo de até 2m)

    private List<float> historicoValores = new List<float>();
    private List<GameObject> elementosGraficos = new List<GameObject>();

    void Start()
    {
        if (containerGrafico == null)
        {
            Debug.LogError("Por favor, atribua o RectTransform do Container do Gráfico no painel Inspector.");
        }
    }

    // Método que será chamado pelo SlimeDataReceiver para injetar novos dados de marcha
    public void AdicionarNovoValor(float valor)
    {
        historicoValores.Add(valor);

        // Mantém o gráfico andando para o lado se estourar o limite de amostragem
        if (historicoValores.Count > maxPontosVisiveis)
        {
            historicoValores.RemoveAt(0);
        }

        AtualizarGrafico();
    }

    private void AtualizarGrafico()
    {
        // Limpa a renderização anterior
        foreach (GameObject obj in elementosGraficos)
        {
            Destroy(obj);
        }
        elementosGraficos.Clear();

        if (historicoValores.Count < 2) return;

        float larguraContainer = containerGrafico.sizeDelta.x;
        float espacamentoX = larguraContainer / (maxPontosVisiveis - 1);

        Vector2 ultimaPosicaoConectada = Vector2.zero;

        for (int i = 0; i < historicoValores.Count; i++)
        {
            // Calcula a coordenada X proporcional ao índice do ponto
            float xPos = i * espacamentoX;
            
            // Normaliza o valor Y baseado na escala estipulada
            float yPos = (historicoValores[i] / escalaValorMaximo) * alturaMaximaGrafico;
            yPos = Mathf.Clamp(yPos, 0f, alturaMaximaGrafico);

            Vector2 posicaoAtual = new Vector2(xPos, yPos);

            // Cria o nó visual (ponto do gráfico)
            CriarPontoVisual(posicaoAtual);

            // Desenha a linha conectando ao ponto anterior
            if (i > 0)
            {
                CriarLinhaConexao(ultimaPosicaoConectada, posicaoAtual);
            }

            ultimaPosicaoConectada = posicaoAtual;
        }
    }

    private void CriarPontoVisual(Vector2 posicao)
    {
        GameObject gameObject = new GameObject("ponto", typeof(Image));
        gameObject.transform.SetParent(containerGrafico, false);
        
        Image imagem = gameObject.GetComponent<Image>();
        imagem.sprite = pontoSprite;
        imagem.color = corLinha;

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = posicao;
        rectTransform.sizeDelta = new Vector2(6, 6);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);

        elementosGraficos.Add(gameObject);
    }

    private void CriarLinhaConexao(Vector2 pontoA, Vector2 pontoB)
    {
        GameObject gameObject = new GameObject("linha", typeof(Image));
        gameObject.transform.SetParent(containerGrafico, false);
        
        Image imagem = gameObject.GetComponent<Image>();
        imagem.color = new Color(corLinha.r, corLinha.g, corLinha.b, 0.5f); // Linha semi-transparente

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 direcao = (pontoB - pontoA).normalized;
        float distancia = Vector2.Distance(pontoA, pontoB);

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distancia, 2f); // Espessura de 2 pixels
        rectTransform.anchoredPosition = pontoA + direcao * distancia * 0.5f;
        
        // Rotaciona a imagem UI para alinhar perfeitamente entre os dois nós
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angulo);

        elementosGraficos.Add(gameObject);
    }
}