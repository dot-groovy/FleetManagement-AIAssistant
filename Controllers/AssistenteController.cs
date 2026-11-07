using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestaoFrota.Services;

namespace GestaoFrota.Controllers;

[Authorize]
public class AssistenteController(AssistenteService assistente, ILogger<AssistenteController> logger) : Controller
{
    private const int MaxMensagensHistorico = 10;
    private const int MaxCaracteresMensagem = 1000;

    // Recebe a pergunta do chat (com o histórico) e devolve a resposta da IA em JSON
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perguntar([FromBody] PerguntaRequest pedido)
    {
        if (string.IsNullOrWhiteSpace(pedido.Pergunta) || pedido.Pergunta.Length > 500)
            return BadRequest(new { erro = "Pergunta inválida (máx. 500 caracteres)." });

        var historico = (pedido.Historico ?? [])
            .Where(m => (m.Papel == "utilizador" || m.Papel == "assistente") && !string.IsNullOrWhiteSpace(m.Texto))
            .TakeLast(MaxMensagensHistorico)
            .Select(m => m with { Texto = m.Texto.Length > MaxCaracteresMensagem ? m.Texto[..MaxCaracteresMensagem] : m.Texto })
            .ToList();

        var idioma = pedido.Idioma == "en" ? "en" : "pt";

        try
        {
            var resposta = await assistente.PerguntarAsync(pedido.Pergunta, historico, idioma);
            return Json(new { resposta });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao comunicar com o serviço de IA");
            return StatusCode(503, new { erro = "O assistente não está disponível. Verifique se o Ollama está em execução." });
        }
    }
}

// Idioma: "pt" ou "en"
public record PerguntaRequest(string Pergunta, List<MensagemChat>? Historico, string? Idioma);
