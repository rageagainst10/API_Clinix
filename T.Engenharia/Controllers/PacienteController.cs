using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using T.Engenharia.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace T.Engenharia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacienteController : ControllerBase
    {
        private readonly IDriver _neo4jDriver;

        public PacienteController(IDriver neo4jDriver)
        {
            _neo4jDriver = neo4jDriver;
        }

        // Adicionar Paciente
        [HttpPost("addPaciente")]
        public async Task<IActionResult> AddPaciente([FromBody] Paciente paciente)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var result = await session.RunAsync(
                    "CREATE (p:Paciente {nome: $nome, sobrenome: $sobrenome}) RETURN p",
                    new { nome = paciente.Nome, sobrenome = paciente.Sobrenome });
                await result.ConsumeAsync();
                return Ok(new { message = "Paciente criado com sucesso!" });
                
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao criar paciente", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        // Listar Pacientes
        [HttpGet("getPacientes")]
        public async Task<IActionResult> GetPacientes()
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var result = await session.RunAsync("MATCH (p:Paciente) RETURN p");
                var pacientes = new List<Paciente>();

                await result.ForEachAsync(record =>
                {
                    var node = record["p"].As<INode>();
                    pacientes.Add(new Paciente
                    {
                        Nome = node.Properties["nome"].As<string>(),
                        Sobrenome = node.Properties.ContainsKey("sobrenome") ? node.Properties["sobrenome"].As<string>() : null
                    });
                });

                return Ok(pacientes);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao buscar pacientes", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        // Buscar Paciente por nome e sobrenome
        [HttpGet("getPaciente/{nome}/{sobrenome}")]
        public async Task<IActionResult> GetPaciente(string nome, string sobrenome)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var result = await session.RunAsync(
                    "MATCH (p:Paciente {nome: $nome, sobrenome: $sobrenome}) RETURN p",
                    new { nome, sobrenome });

                var records = await result.ToListAsync();
                var record = records.SingleOrDefault();

                if (record == null) return NotFound(new { message = "Paciente não encontrado." });

                var node = record["p"].As<INode>();
                return Ok(new Paciente
                {
                    Nome = node.Properties["nome"].As<string>(),
                    Sobrenome = node.Properties["sobrenome"].As<string>()
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao buscar paciente", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

   
        //Update Paciente (atualiza nome e sobrenome)
        [HttpPut("updatePaciente")]
        public async Task<IActionResult> UpdatePaciente([FromBody] AtualizarPacienteRequest request)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                await session.RunAsync(
                    @"MATCH (p:Paciente {nome: $nomeAntigo, sobrenome: $sobrenomeAntigo})
                      SET p.nome = $nomeNovo, p.sobrenome = $sobrenomeNovo",
                    new
                    {
                        nomeAntigo = request.NomeAntigo,
                        sobrenomeAntigo = request.SobrenomeAntigo,
                        nomeNovo = request.NomeNovo,
                        sobrenomeNovo = request.SobrenomeNovo
                    });

                return Ok(new { message = "Paciente atualizado com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao atualizar paciente", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        // Deletar Paciente
        [HttpDelete("deletePaciente/{nome}/{sobrenome}")]
        public async Task<IActionResult> DeletePaciente(string nome, string sobrenome)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                await session.RunAsync(
                    "MATCH (p:Paciente {nome: $nome, sobrenome: $sobrenome}) DETACH DELETE p",
                    new { nome, sobrenome });

                return Ok(new { message = "Paciente removido com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao remover paciente", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
