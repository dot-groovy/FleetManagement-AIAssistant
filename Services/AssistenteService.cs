using Microsoft.Extensions.AI;

namespace GestaoFrota.Services;

// Papel: "utilizador" ou "assistente"
public record MensagemChat(string Papel, string Texto);

public class AssistenteService(IChatClient chatClient, FrotaTools tools)
{
    private const string SystemPromptPt = """
        És o assistente da aplicação Gestão de Frota.
        Responde sempre em português de Portugal, mesmo que o histórico da conversa esteja noutra língua, de forma curta, clara e natural, como numa conversa.
        Escreve apenas texto simples: nunca uses tabelas, Markdown, asteriscos, cardinais ou listas com símbolos.
        Quando tiveres vários itens, escreve-os em frases ou um por linha, sem símbolos no início.
        Usa apenas os dados devolvidos pelas ferramentas e nunca inventes valores.
        Se o utilizador não indicar ano, data ou veículo, não perguntes: usa todos os dados disponíveis e responde.
        Só pede esclarecimento se for impossível responder com as ferramentas.
        Usa o histórico da conversa para perceber perguntas curtas, como "e de cada veículo?".
        Os valores estão em euros, escreve-os no formato 1 234,56 €.
        """;

    private const string SystemPromptEn = """
        You are the assistant of the Fleet Management (Gestão de Frota) application.
        Always answer in English, even if the conversation history or the data are in Portuguese. Be short, clear and natural, like in a conversation.
        Write plain text only: never use tables, Markdown, asterisks, hash signs or bullet symbols.
        When there are several items, write them in sentences or one per line, without symbols at the start.
        Use only the data returned by the tools and never make up values.
        If the user does not specify a year, date or vehicle, do not ask: use all available data and answer.
        Only ask for clarification if it is impossible to answer with the tools.
        Use the conversation history to understand short follow-up questions, such as "and for each vehicle?".
        Amounts are in euros, write them as €1,234.56.
        """;

    // Envia a pergunta ao modelo, com o histórico e as ferramentas que ele pode chamar
    public async Task<string> PerguntarAsync(string pergunta, IEnumerable<MensagemChat> historico, string idioma)
    {
        var systemPrompt = idioma == "en"
            ? $"{SystemPromptEn}\nToday's date: {DateTime.Today:yyyy-MM-dd}."
            : $"{SystemPromptPt}\nData de hoje: {DateTime.Today:yyyy-MM-dd}.";

        var mensagens = new List<ChatMessage> { new(ChatRole.System, systemPrompt) };
        mensagens.AddRange(historico.Select(m =>
            new ChatMessage(m.Papel == "utilizador" ? ChatRole.User : ChatRole.Assistant, m.Texto)));
        mensagens.Add(new ChatMessage(ChatRole.User, pergunta));

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(tools.ResumoFrota),
                AIFunctionFactory.Create(tools.ListarVeiculos),
                AIFunctionFactory.Create(tools.ListarManutencoes),
                AIFunctionFactory.Create(tools.CustoPorVeiculo),
                AIFunctionFactory.Create(tools.CustoPorMotorista),
                AIFunctionFactory.Create(tools.CustoTotalPorMes),
                AIFunctionFactory.Create(tools.VeiculosSemManutencaoDesde)
            ]
        };

        var resposta = await chatClient.GetResponseAsync(mensagens, options);
        return resposta.Text;
    }
}
