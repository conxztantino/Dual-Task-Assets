using UnityEngine;
using uOSC;
using System.IO;

public class SlimeDataReceiver : MonoBehaviour
{
    [Header("Configuração dos Pés")]
    public Transform Quadril;
    public Transform peEsquerdo;
    public Transform peDireito;
    public Transform coxaEsquerda;
    public Transform coxaDireita;
    public Transform Peito;

    // Substitua pelos números que você identificou no console
    [Header("IDs dos Trackers (Vistos no Console)")]
    public string idQuadril = "5"; 
    public string idPeEsquerdo = "1";
    public string idPeDireito = "2";
    public string idCoxaEsquerda = "3";
    public string idCoxaDireita = "4";
    public string idPeito = "6";

    private string caminhoArquivo;
    private float distanciaTotalGeral = 0f;
    private Vector3 ultimaPosEsquerdo;
    private Vector3 ultimaPosDireito;
    private Vector3 ultimaPosCoxaEsquerda;
    private Vector3 ultimaPosCoxaDireita;
    private Vector3 ultimaPosQuadril;
    private Vector3 ultimaPosPeito;
    void Start()
    {
        // Define onde o arquivo será salvo (na pasta do projeto)
        caminhoArquivo = Application.dataPath + "/Dados_Marcha_Neuroreabilitacao.csv";
        
        // Cria o cabeçalho do CSV se o arquivo não existir
        if (!File.Exists(caminhoArquivo))
        {
            File.WriteAllText(caminhoArquivo, "Tempo;ID;Eixo_X;Eixo_Y;Eixo_Z;Distancia_Passo;Distancia_Total\n");
        }
        if(peEsquerdo) ultimaPosEsquerdo = peEsquerdo.position;
        if(peDireito) ultimaPosDireito = peDireito.position;
        if(coxaEsquerda) ultimaPosCoxaEsquerda = coxaEsquerda.position;
        if(coxaDireita) ultimaPosCoxaDireita = coxaDireita.position;
        if(Quadril) ultimaPosQuadril = Quadril.position;
        if(Peito) ultimaPosPeito = Peito.position;
    }
    private void SalvarDados(string endereco, float x, float y, float z, float dPasso, float dTotal)
{
    // Adicionamos dPasso e dTotal no final da linha
    string novaLinha = $"{Time.time:F2};{endereco};{x:F2};{y:F2};{z:F2};{dPasso:F4};{dTotal:F4}\n";
    File.AppendAllText(caminhoArquivo, novaLinha);
}
    public void OnDataReceived(Message message)
    {
        // Filtra apenas mensagens de rotação com 3 valores (Euler)
        if (message.address.EndsWith("/rotation") && message.values.Length >= 3)
        {
            float x = (float)message.values[0];
            float y = (float)message.values[1];
            float z = (float)message.values[2];

            Quaternion rotacaoAlvo = Quaternion.Euler(x, y, z);
            float dPasso = 0f;

            if (message.address.Contains("/trackers/" + idPeEsquerdo))
            {
                peEsquerdo.localRotation = rotacaoAlvo;
                dPasso = Vector3.Distance(peEsquerdo.position, ultimaPosEsquerdo);
                if(dPasso > 0.01f) {
                    distanciaTotalGeral += dPasso;
                    ultimaPosEsquerdo = peEsquerdo.position;
                }
                SalvarDados(message.address, x, y, z, dPasso, distanciaTotalGeral);
            }
            else if (message.address.Contains("/trackers/" + idPeDireito))
            {
                peDireito.localRotation = rotacaoAlvo;
                dPasso = Vector3.Distance(peDireito.position, ultimaPosDireito);
                if(dPasso > 0.01f) {
                    distanciaTotalGeral += dPasso;
                    ultimaPosDireito = peDireito.position;
                }
                SalvarDados(message.address, x, y, z, dPasso, distanciaTotalGeral);
            }
        
            else if (message.address.Contains("/trackers/" + idCoxaEsquerda))
            {
                coxaEsquerda.localRotation = rotacaoAlvo;
                dPasso = Vector3.Distance(coxaEsquerda.position, ultimaPosCoxaEsquerda);
                if(dPasso > 0.01f) {
                    distanciaTotalGeral += dPasso;
                    ultimaPosCoxaEsquerda = coxaEsquerda.position;
                }
                SalvarDados(message.address, x, y, z, dPasso, distanciaTotalGeral);
            }
            else if (message.address.Contains("/trackers/" + idCoxaDireita + "/"))
            {
                coxaDireita.localRotation = rotacaoAlvo;
                dPasso = Vector3.Distance(coxaDireita.position, ultimaPosCoxaDireita);
                if(dPasso > 0.01f) {
                    distanciaTotalGeral += dPasso;
                    ultimaPosCoxaDireita = coxaDireita.position;
                }
                SalvarDados(message.address, x, y, z, dPasso, distanciaTotalGeral);
            }
             else if (message.address.Contains("/trackers/" + idQuadril + "/"))
            {
                Quadril.localRotation = rotacaoAlvo;
                dPasso = Vector3.Distance(Quadril.position, ultimaPosQuadril);
                if(dPasso > 0.01f) {
                    distanciaTotalGeral += dPasso;
                    ultimaPosQuadril = Quadril.position;
                }
                SalvarDados(message.address, x, y, z, dPasso, distanciaTotalGeral);
            }
             else if (message.address.Contains("/trackers/" + idPeito + "/"))
            {
                Peito.localRotation = rotacaoAlvo;
                dPasso = Vector3.Distance(Peito.position, ultimaPosPeito);
                if(dPasso > 0.01f) {
                    distanciaTotalGeral += dPasso;
                    ultimaPosPeito = Peito.position;
                }
                SalvarDados(message.address, x, y, z, dPasso, distanciaTotalGeral);
            }
        }

        //if (message.address.EndsWith("/position") && message.values.Length >= 3)
    //{
       // float px = System.Convert.ToSingle(message.values[0]);
       // float py = System.Convert.ToSingle(message.values[1]);
       // float pz = System.Convert.ToSingle(message.values[2]);

        //if (message.address.Contains("/trackers/" + idPeEsquerdo))
       // {
      //      peEsquerdo.position = new Vector3(px, py, pz);
     //       AtualizarDistancia(peEsquerdo.position, ref ultimaPosEsquerdo, message.address);
      //  }
      //  else if (message.address.Contains("/trackers/" + idPeDireito))
       // {
       //     peDireito.position = new Vector3(px, py, pz);
       //     AtualizarDistancia(peDireito.position, ref ultimaPosDireito, message.address);
       // }
       //   else if (message.address.Contains("/trackers/" + idCoxaEsquerda))
        //{
          //  coxaEsquerda.position = new Vector3(px, py, pz);
            //AtualizarDistancia(coxaEsquerda.position, ref ultimaPosCoxaEsquerda, message.address);
        //}
         // else if (message.address.Contains("/trackers/" + idCoxaDireita))
        //{
          //  coxaDireita.position = new Vector3(px, py, pz);
            //AtualizarDistancia(coxaDireita.position, ref ultimaPosCoxaDireita, message.address);
        //}
          //else if (message.address.Contains("/trackers/" + idQuadril))
        //{
          //  Quadril.position = new Vector3(px, py, pz);
            //AtualizarDistancia(Quadril.position, ref ultimaPosQuadril, message.address);
        //}
          //else if (message.address.Contains("/trackers/" + idPeito))
        //{
          //  Peito.position = new Vector3(px, py, pz);
            //AtualizarDistancia(Peito.position, ref ultimaPosPeito, message.address);
        //}
    //}
    }

      private void AtualizarDistancia(Vector3 posAtual, ref Vector3 posAnterior, string addr)
   {
    float dPasso = Vector3.Distance(posAtual, posAnterior);
    if (dPasso > 0.01f)
    {
        distanciaTotalGeral += dPasso;
        posAnterior = posAtual;
        // Salva os dados automaticamente no CSV
        SalvarDados(addr, 0, 0, 0, dPasso, distanciaTotalGeral);
    }
  }
  }

   // Certifique-se de ter as referências do quadril e peito declaradas no topo do script

//void Update()
//{
    // Só move o tronco se ambos os pés já tiverem dados iniciais
  //  if (peEsquerdo != null && peDireito != null)
    //{
        // 1. Calcula a posição média dos pés no plano horizontal (X e Z)
      //  float centroX = (peEsquerdo.position.x + peDireito.position.x) / 2f;
        //float centroZ = (peEsquerdo.position.z + peDireito.position.z) / 2f;

        // 2. Desloca o Quadril mantendo a altura (Y) original dele
       // Quadril.position = new Vector3(centroX, Quadril.position.y, centroZ);

        // 3. Desloca o Peito acompanhando o quadril no plano horizontal
       // Peito.position = new Vector3(centroX, Peito.position.y, centroZ);
    //}
//}
//}
