
/*                 S.A.R.A - Speech-Activated Responsive Assistant                      
                                                                              
         Sistema desenvolvido por Wenderson Rafael e sua empresa independente: 
         welabs TEAM.                                                               
                                                                              
         Este software é de código aberto, permitindo modificações e           
         atualizações. Solicitamos que os créditos do código-fonte sejam       
         mantidos em nome do criador original.    

        ----------------------------------------------------------------------------

        System developed by Wenderson Rafael and his independent company:     
        welabs TEAM.                                                               
                                                                             
        This software is open source, allowing modifications and updates. We  
        kindly request that the source code credits be kept in the name of     
        the original creator. 

          Copyright © 2023-2024 welabs TEAM Softwares Co. Todos os direitos reservados.
          Site: https://welabs.tech or https://welabsteam.com
          Repository: https://github.com/WendersonRafael/Projeto-S.A.R.A/

*/


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using Microsoft.Speech.Recognition;
using System.Speech.Synthesis;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using welabs.SISTEMAS_S.A.R.A;

namespace welabs
{

    public partial class saraform : Form
    {
        //Variaveis globais
        // variaveis para voz
        static CultureInfo ci = new CultureInfo("pt-BR");// linguagem utilizada
        static SpeechRecognitionEngine reconhecedor; // reconhecedor de voz
        SpeechSynthesizer resposta = new SpeechSynthesizer();// sintetizador de voz

        CancellationTokenSource cancelTokenSource; // Token de cancelamento
        bool falando = false; // Variável para controlar se o sintetizador está falando
        bool aguardandoChamada = true;
        bool microfoneMudo = false;



        // Palavras aceitas: PalavrasAceitas.cs
        public string[] listaPalavras = PalavrasAceitas.ListaPalavras;

        //lê o texto copiado
        private void LerTextoCopiado()
        {
            if (Clipboard.ContainsText())
            {
                string textoCopiado = Clipboard.GetText();
                resposta.SpeakAsync(textoCopiado);
            }
            else
            {
                resposta.SpeakAsync("Não encontrei nada para ler");
            }
        }

        private void EsvaziarLixeira()
        {
            try
            {
                string recycleBinPath = "C:\\$Recycle.Bin"; // Caminho para a lixeira

                DirectoryInfo recycleBinInfo = new DirectoryInfo(recycleBinPath);
                if (recycleBinInfo.Exists)
                {
                    foreach (FileInfo file in recycleBinInfo.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (DirectoryInfo dir in recycleBinInfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }

                    resposta.SpeakAsync("Lixeira esvaziada com sucesso!");
                }
                else
                {
                    resposta.SpeakAsync("A lixeira não foi encontrada. Certifique-se de que a lixeira existe no caminho especificado.");
                }
            }
            catch (Exception ex)
            {
                resposta.SpeakAsync("Ocorreu um erro ao esvaziar a lixeira: " + ex.Message);
            }
        }

        //PROCURA MALWARES NO SISTEMA
        private void VerificarArquivosMaliciososNoComputador()
        {
            string[] drives = Directory.GetLogicalDrives(); // Obtém as unidades lógicas do computador

            string[] suspiciousKeywords = { "malware", "virus", "trojan", "ransomware" }; // Palavras-chave suspeitas

            foreach (string drive in drives)
            {
                var files = Directory.GetFiles(drive, "*.*", SearchOption.AllDirectories);

                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string fileContent = File.ReadAllText(filePath);

                    foreach (string keyword in suspiciousKeywords)
                    {
                        if (fileContent.Contains(keyword))
                        {
                            resposta.SpeakAsync("Arquivo suspeito encontrado: " + filePath);
                            break;
                        }
                    }
                }
            }

            resposta.SpeakAsync("Verificação de arquivos concluída em todo o computador.");
        }

        private string mensagemAnterior = string.Empty;

        private async Task<string> ObterRespostaInteligente(string pergunta)
        {
            try
            {
                string respostaGPT = await ChatGPTIntegration.ObterRespostaGPT(pergunta);
                return respostaGPT;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter resposta inteligente: {ex.Message}");
                return "Desculpe, não consegui obter uma resposta no momento.";
            }
        }

        private async Task<string> AguardarRespostaUsuario()
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            EventHandler<SpeechRecognizedEventArgs> handler = null;
            handler = (sender, e) =>
            {
                string respostaUsuario = e.Result.Text;
                tcs.SetResult(respostaUsuario);
                reconhecedor.SpeechRecognized -= handler;
            };

            reconhecedor.SpeechRecognized += handler;

            return await tcs.Task; // Marcar o método como assíncrono e usar await aqui
        }


        public saraform()
        {
            InitializeComponent();
            Init();
            // Obter o nome da máquina
            string nomeMaquina = Environment.MachineName;

            // Atribuir o nome da máquina à propriedade Text da label
            guna2Button2.Text = nomeMaquina;



        }
       

        bool janelaMaximizada = true; // Variável para controlar o estado da janela

        public void Gramatica()
        {
            try
            {
                reconhecedor = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("pt-BR"));
                reconhecedor = new SpeechRecognitionEngine(ci);
               
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRO ao integrar lingua escolhida:" + ex.Message);
            }

            // criacao da gramatica simples que o programa vai entender
            // usando um objeto Choices
            var gramatica = new Choices();
            gramatica.Add(listaPalavras); // inclui a gramatica criada

            // cria o construtor gramatical
            // e passa o objeto criado com as palavras
            var gb = new GrammarBuilder();
            gb.Append(gramatica);

            // cria a instancia e carrega a engine de reconhecimento
            // passando a gramatica construida anteriomente
            try
            {
                var g = new Grammar(gb);

                try
                {
                    // carrega o arquivo de gramatica
                    reconhecedor.RequestRecognizerUpdate();
                    reconhecedor.LoadGrammarAsync(g);

                    // registra a voz como mecanismo de entrada para o evento de reconhecimento
                    reconhecedor.SpeechRecognized += Sre_Reconhecimento;

                    reconhecedor.SetInputToDefaultAudioDevice(); // microfone padrao
                    resposta.SetOutputToDefaultAudioDevice(); // auto falante padrao
                    reconhecedor.RecognizeAsync(RecognizeMode.Multiple); // multiplo reconhecimento
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERRO ao criar reconhecedor: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRO ao criar a gramática: " + ex.Message);
            }
        }

        public void Init()
        {
            resposta.Volume = 100; // controla volume de saida
            resposta.Rate = 3; // velocidade de fala

            Gramatica(); // inicialização da gramatica
        }

        // funcao para reconhecimento de voz
        async void Sre_Reconhecimento(object sender, SpeechRecognizedEventArgs e)
        {


            string frase = e.Result.Text;
            //////Sistema de parar

            if (frase.Equals("Pare") || frase.Equals("Sara ja esta bom") || frase.Equals("Sara fique queita"))
            {
                if (cancelTokenSource != null)
                {
                    cancelTokenSource.Cancel(); // Cancela a execução da função atual
                }
                resposta.SpeakAsyncCancelAll();
                falando = false;
                aguardandoChamada = true; // Define como verdadeiro para aguardar um novo comando
                Thread.Sleep(100);
                resposta.SpeakAsync("Tudo bem, me desculpe!");
            }
            /////
            else if (aguardandoChamada)
            {
                if (frase.Equals("Sara") || frase.Equals("O Sara")) 
                {
                    resposta.SpeakAsync("Sim?");

                    aguardandoChamada = false;
                }

            }
            else
            {

                if (frase.Equals("Bloquear computador"))
                {
                    {
                        Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
                        resposta.SpeakAsync("Computador bloqueado");


                    }
                }
                else if (frase.Equals("Lêia"))
                {
                    LerTextoCopiado();
                }

                else if (frase.Equals("Me diga as horas"))
                {
                    DateTime currentTime = DateTime.Now;
                    string hora = currentTime.ToString("HH:mm");
                    resposta.SpeakAsync($"Agora são {hora}");
                }
                else if (frase.Equals("Me dê uma resposta inteligente"))
                {
                    resposta.SpeakAsync("Claro! Qual é a sua pergunta?");
                    aguardandoChamada = false;
                    string pergunta = await AguardarRespostaUsuario(); // Aguarda a resposta do usuário
                    string respostaInteligente = await ObterRespostaInteligente(pergunta);
                    resposta.SpeakAsync(respostaInteligente);
                }
                else if (frase.Equals("Quem te criou"))
                {
                    resposta.SpeakAsync("Fui pensada, criada e desenvolvida pelo jovem cientista Uenderson Rafael");

                }
                else if (frase.Equals("Obrigado"))
                {
                    resposta.SpeakAsync("Disponha!");
                }
                else if (frase.Equals("Encerrar sistema"))
                {
                    resposta.Speak("Ok. Salvando dados coletados...");
                    Thread.Sleep(300);
                    resposta.Speak("Sistema encerrado.");
                    Application.Exit();
                }
                else if (frase.Equals("Reiniciar sistema"))
                {
                    resposta.Speak("Reiniciando sistema. salvando protocolos...");
                    Thread.Sleep(3000);
                    resposta.Speak("Sistema reiniciado.");
                    Application.Restart();

                }
                else if (frase.Equals("Esconder-se"))
                {
                    this.WindowState = FormWindowState.Minimized;
                    resposta.SpeakAsync("Pronto");
                    janelaMaximizada = false; // Define o estado da janela como minimizado
                }
                else if (frase.Equals("Apareça"))
                {
                    this.WindowState = FormWindowState.Normal; // Restaura o tamanho da janela
                    resposta.SpeakAsync("Tudo bem");
                    janelaMaximizada = true; // Define o estado da janela como maximizado
                }
                else if (frase.StartsWith("Abrir MangleHub"))// app desenvolvido pelo dev da S.A.R.A
                {
                    resposta.SpeakAsync("Abrindo MangouHub!");
                    string manglehub = $"C:\\Users\\Usuário\\source\\repos\\MangleHub\\MangleHub\\bin\\Debug\\MangleHub.exe";
                    System.Diagnostics.Process.Start(manglehub);
                }
                else if (frase.StartsWith("Abrir servidor do MangleHub"))// app desenvolvido pelo dev da S.A.R.A
                {
                    resposta.SpeakAsync("Abrindo MangouHub Server Manegement!");
                    string manglehub = $"C:\\Users\\Usuário\\source\\repos\\SERVER\\SERVER\\bin\\Debug\\net7.0\\SERVER.exe";
                    System.Diagnostics.Process.Start(manglehub);
                }

                else if (frase.StartsWith("Abrir Spotify"))
                {
                    resposta.SpeakAsync("Abrindo Spotify!");
                    Process.Start("spotify.exe");
                }
                else if (frase.StartsWith("Abrir navegador"))
                {
                    resposta.SpeakAsync("Abrindo Microsoft Edge!");
                    Process.Start("msedge.exe");

                }


                else if (frase.Equals("Abrir site do desenvolvedor"))
                {
                    resposta.SpeakAsync("Abrindo ui labs");
                    string url = "https://www.welabs.tech";

                    // Inicia o navegador padrão com a URL especificada
                    Process.Start(url);
                }
                else if (frase.Equals("Realize um diaguinostico do seu sistema"))
                {
                    resposta.SpeakAsync("Realizando diaguinostico.");
                    VerificarArquivosMaliciososNoComputador();

                }
                else if (frase.Equals("Faça um backup"))
                {


                    Console.WriteLine("Realizando backup dos arquivos...");
                    resposta.SpeakAsync("Realizando backup dos arquivos...");
                    string sourceDirectory = $"C:\\Users\\Usuário\\source\\repos\\welabs\\welabs";
                    string backupDirectory = $"C:\\Users\\Usuário\\Documents\\BACKUP";

                    try
                    {
                        // Se o diretório de backup não existir, crie-o
                        if (!Directory.Exists(backupDirectory))
                        {
                            Directory.CreateDirectory(backupDirectory);
                        }

                        // Copie todos os arquivos do diretório de origem para o diretório de backup
                        foreach (string filePath in Directory.GetFiles(sourceDirectory))
                        {
                            string fileName = Path.GetFileName(filePath);
                            string destFilePath = Path.Combine(backupDirectory, fileName);
                            File.Copy(filePath, destFilePath, true);
                        }

                        Console.WriteLine("Backup concluído com sucesso!");
                        resposta.SpeakAsync("Backup concluído com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ocorreu um erro ao realizar o backup: {ex.Message}");
                        resposta.SpeakAsync($"Ocorreu um erro ao realizar o backup: {ex.Message}");
                    }
                }
                else if (frase.Equals("Esvaziar lixeira"))
                {
                    resposta.SpeakAsync($"Executando protocolos. Esvaziando a lixeira. Aguarde.");
                    EsvaziarLixeira();
                }
                else if (frase.Equals("Me diga a data"))
                {
                    DateTime currentDate = DateTime.Now;
                    string date = currentDate.ToString("dd/MM/yyyy");
                    resposta.SpeakAsync($"Hoje é {date}");
                }
                else if (frase.Equals("Informações da maquina"))
                {
                    resposta.SpeakAsync("Tudo bem!, aguarde");
                    frmInfo info = new frmInfo();
                    info.Show();
                }
                else if (frase.Equals("Fechar informações da maquina"))
                {
                    resposta.SpeakAsync("Fechando informações da máquina.");
                    // Verifica se a janela de informações da máquina está aberta
                    frmInfo info = Application.OpenForms.OfType<frmInfo>().FirstOrDefault();
                    if (info != null)
                    {
                        info.Close(); // Fecha a janela de informações da máquina
                    }
                    else
                    {
                        resposta.SpeakAsync("As informações da máquina não estão abertas.");
                    };
                }
                else if (frase.Equals("Mostrar janela de comandos"))
                {
                    resposta.SpeakAsync("Aqui está!");
                    frmComandos cmds = new frmComandos();
                    cmds.Show();
                }
                else if (frase.Equals("Fechar janela de comandos"))
                {
                    resposta.SpeakAsync("Fechando.");
                    // Verifica se a janela de informações da máquina está aberta
                    frmComandos cmds = Application.OpenForms.OfType<frmComandos>().FirstOrDefault();
                    if (cmds != null)
                    {
                        cmds.Close(); // Fecha a janela de informações da máquina
                    }
                    else
                    {
                        resposta.SpeakAsync("A janela de comandos não está aberta.");
                    };
                }
                else if (frase.Equals("Changelog"))
                {
                    string changelog = "Changelog da Assistente Inteligente S.A.R.A\n\n" +
                   "Versão 1.0.0:\n" +
                   "- Reconhecimento de voz implementado\n" +
                   "- Resposta a comandos de voz básicos\n" +
                   "- Função de bloqueio do computador\n" +
                   "- Consulta das horas\n" +
                   "- Identificação do desenvolvedor\n\n" +
                   "Versão 1.1.0:\n" +
                   "- Adicionada função de leitura de texto copiado\n" +
                   "- Verificação de arquivos maliciosos no computador\n" +
                   "- Reinicialização do sistema\n" +
                   "- Backup de arquivos\n" +
                   "- Consulta da data\n" +
                   "- Abertura do site do desenvolvedor\n" +
                   "- Diagnóstico do sistema\n" +
                   "- Exibição de informações da máquina\n" +
                   "- Exibição da janela de comandos\n" +
                   "- Changelog\n\n" +
                   "Versão 1.2.0 (Próxima Atualização):\n" +
                   "- Implementação de rede neural para respostas mais inteligentes\n" +
                   "- Integração com mais aplicativos e serviços\n" +
                   "- Expansão da lista de comandos\n" +
                   "- Melhorias na interface de usuário\n\n" +
                   "Agradecemos por usar a Assistente Inteligente S.A.R.A! Continue explorando suas funcionalidades.";

                    MessageBox.Show(changelog, "Changelog", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                else
                {
                    resposta.SpeakAsync("Você já me chamou. Diga: Sara, e a função desejada");
                }


                aguardandoChamada = true; // Define novamente como verdadeiro após um comando ser executado
            }
        }
        
        private void btnFechar_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnMinimizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnInfo_Click_1(object sender, EventArgs e)
        {
            string mensagem = "Bem-vindo à Assistente Inteligente S.A.R.A!\n\n" +
                    "Estamos empolgados em apresentar a versão 1.2.0 da S.A.R.A, que traz incríveis aprimoramentos para tornar sua experiência ainda mais inteligente e conveniente." +
                    "\n\nA grande novidade é a implementação de uma poderosa rede neural, permitindo que a S.A.R.A aprenda com suas interações e se torne ainda mais perspicaz. Ela compreenderá suas necessidades com maior precisão e oferecerá respostas mais inteligentes." +
                    "\n\nAlém disso, expandimos as funções que a S.A.R.A pode executar no sistema operacional Windows. Agora, ela pode auxiliá-lo em tarefas como gerenciar arquivos, realizar diagnósticos de sistema, realizar backups e muito mais." +
                    "\n\nE isso não é tudo. Estamos planejando o futuro da S.A.R.A, e uma das próximas etapas é a integração com a automação residencial. Em breve, você poderá controlar sua casa de forma mais inteligente e eficiente com a ajuda da S.A.R.A." +
                    "\n\nFique atento para atualizações futuras e novas funcionalidades que tornarão sua vida ainda mais fácil. Para ver a lista de comandos da S.A.R.A, diga \"Sara, mostrar janela de comandos\"." +
                    "\n\nPara ver a lista de comandos da S.A.R.A, diga \"Sara, mostrar janela de comandos\".";
                MessageBox.Show(mensagem, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            frmInfo info = new frmInfo();

            info.Show();
        }


        private void frmPrincipal_Load(object sender, EventArgs e)
        {
            if (microfoneMudo)
            {
                btnMuteMicrofone.Text = "Desmutar";
            }
            else
            {
                btnMuteMicrofone.Text = "Mutar";
            }

            //mensagem incial (adicionada após a lógica do botão)
            resposta.SpeakAsync(" Sara iniciado! Em que posso ajudar?!");
        }

        private void btnMuteMicrofone_Click(object sender, EventArgs e)
        {
            if (microfoneMudo)
    {
        // Se o microfone estiver mudo, reativar o reconhecimento de voz
        reconhecedor.RecognizeAsync(RecognizeMode.Multiple);
        btnMuteMicrofone.Text = "Mutar";
    }
    else
    {
        // Se o microfone estiver ativado, interromper o reconhecimento de voz
        reconhecedor.RecognizeAsyncStop();
        btnMuteMicrofone.Text = "Desmutar";
    }
    
    microfoneMudo = !microfoneMudo; // Inverte o estado do microfone
        }

        
    }
}
