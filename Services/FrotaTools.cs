using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.Services;

// Só expõe dados não pessoais (sem NIF nem telefone) - RGPD
public class FrotaTools(ApplicationDbContext db)
{
    [Description("Resumo geral da frota: número de veículos, motoristas e manutenções, custo total e data da última manutenção")]
    public async Task<object> ResumoFrota() => new
    {
        Veiculos = await db.Veiculos.CountAsync(),
        Motoristas = await db.Motoristas.CountAsync(),
        Manutencoes = await db.Manutencoes.CountAsync(),
        CustoTotal = await db.Manutencoes.SumAsync(m => m.Custo),
        UltimaManutencao = await db.Manutencoes.MaxAsync(m => (DateTime?)m.Data)
    };

    [Description("Lista todos os veículos da frota com matrícula, marca, modelo e motorista")]
    public async Task<object> ListarVeiculos() =>
        await db.Veiculos
            .Select(v => new { v.Matricula, v.Marca, v.Modelo, Motorista = v.Motorista!.Nome })
            .ToListAsync();

    [Description("Lista as manutenções (data, tipo, custo e veículo), da mais recente para a mais antiga. Sem matrícula devolve as de todos os veículos")]
    public async Task<object> ListarManutencoes(
        [Description("Matrícula do veículo (opcional). Omitir para todos os veículos")] string? matricula = null)
    {
        var query = db.Manutencoes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(matricula))
        {
            var filtro = matricula.Trim();
            query = query.Where(m => m.Veiculo!.Matricula == filtro);
        }
        return await query
            .OrderByDescending(m => m.Data)
            .Take(50)
            .Select(m => new { Veiculo = m.Veiculo!.Matricula, m.Data, m.Tipo, m.Custo })
            .ToListAsync();
    }

    [Description("Número de manutenções e custo total de cada veículo, do maior para o menor custo")]
    public async Task<object> CustoPorVeiculo(
        [Description("Ano (opcional). Omitir para considerar todo o histórico")] int? ano = null) =>
        await db.Veiculos
            .Select(v => new
            {
                v.Matricula,
                v.Marca,
                v.Modelo,
                NumeroManutencoes = v.Manutencoes.Count(m => ano == null || m.Data.Year == ano),
                Total = v.Manutencoes.Where(m => ano == null || m.Data.Year == ano).Sum(m => m.Custo)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

    [Description("Custo total de manutenções por motorista, do maior para o menor")]
    public async Task<object> CustoPorMotorista(
        [Description("Ano (opcional). Omitir para considerar todo o histórico")] int? ano = null) =>
        await db.Manutencoes
            .Where(m => ano == null || m.Data.Year == ano)
            .GroupBy(m => m.Veiculo!.Motorista!.Nome)
            .Select(g => new { Motorista = g.Key, Total = g.Sum(m => m.Custo) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

    [Description("Custo total de manutenções de toda a frota por mês")]
    public async Task<object> CustoTotalPorMes(
        [Description("Ano (opcional). Omitir para usar o ano atual")] int? ano = null)
    {
        var anoConsulta = ano ?? DateTime.Today.Year;
        return await db.Manutencoes
            .Where(m => m.Data.Year == anoConsulta)
            .GroupBy(m => m.Data.Month)
            .Select(g => new { Mes = g.Key, Total = g.Sum(m => m.Custo) })
            .OrderBy(x => x.Mes)
            .ToListAsync();
    }

    [Description("Veículos que não têm nenhuma manutenção desde uma data")]
    public async Task<object> VeiculosSemManutencaoDesde(
        [Description("Data no formato yyyy-MM-dd, ex: 2026-01-01")] DateTime data) =>
        await db.Veiculos
            .Where(v => !v.Manutencoes.Any(m => m.Data >= data))
            .Select(v => new { v.Matricula, v.Marca, v.Modelo })
            .ToListAsync();
}
