using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace welabs
{


public class ChatGPTIntegration
{
    private const string ChatGPTApiKey = "SuaAPIKey";
    private const string ChatGPTApiUrl = "https://api.openai.com/v1/chat/completions";

    public static async Task<string> ObterRespostaGPT(string mensagem)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ChatGPTApiKey}");

            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "Você é a Assistente S.A.R.A." },
                    new { role = "user", content = mensagem }
                }
            };

            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(ChatGPTApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                // Analisar a resposta do Chat GPT e extrair a mensagem gerada
                var respostaGPT = Newtonsoft.Json.JsonConvert.DeserializeObject<ChatGPTResponse>(jsonResponse);
                return respostaGPT.choices[0].message.content;
            }
            else
            {
                return "Desculpe, ocorreu um erro ao obter uma resposta do Chat GPT.";
            }
        }
    }
}

public class ChatGPTResponse
{
    public ChatGPTChoice[] choices { get; set; }
}

public class ChatGPTChoice
{
    public ChatGPTMessage message { get; set; }
    }

    public class ChatGPTMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
