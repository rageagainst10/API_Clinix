using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using T.Engenharia.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace T.Engenharia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicoController : ControllerBase
    {
        private readonly IDriver _neo4jDriver;

        public MedicoController(IDriver neo4jDriver)
        {
            _neo4jDriver = neo4jDriver;
        }

        //Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Medico medico)
        {
            await using var session = _neo4jDriver.AsyncSession();
            try
            {
                var query = @"MATCH (m:Medico {nome: $nome, senha: $senha}) RETURN m";
                var result = await session.RunAsync(query, new { medico.Nome, medico.Senha });
                var record = (await result.ToListAsync()).SingleOrDefault();

                return record != null
                    ? Ok(new { message = "Login realizado com sucesso!" })
                    : Unauthorized(new { message = "Credenciais inválidas." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao realizar login", error = ex.Message });
            }
        }

        // Criar novo médico
        [HttpPost("addMedico")]
        public async Task<IActionResult> AddMedico([FromBody] Medico medico)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                // Verifica se já existe
                var existe = await session.RunAsync(
                    "MATCH (m:Medico {nome: $nome}) RETURN m",
                    new {nome= medico.Nome });

                if ((await existe.ToListAsync()).Any())
                {
                    return Conflict(new { message = "Médico já cadastrado." });
                }

                await session.RunAsync(
                    "CREATE (m:Medico {nome: $nome, senha: $senha}) RETURN m",
                    new {nome = medico.Nome,senha = medico.Senha });

                return Ok(new { message = "Médico criado com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao adicionar médico", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        //Listar Médicos
        [HttpGet("getMedicos")]
        public async Task<IActionResult> GetMedicos()
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var result = await session.RunAsync("MATCH (m:Medico) RETURN m");
                var medicos = new List<Medico>();
                await result.ForEachAsync(record =>
                {
                    var node = record["m"].As<INode>();
                    medicos.Add(new Medico
                    {
                        Nome = node.Properties["nome"].As<string>(),
                        Senha = node.Properties.ContainsKey("senha") ? node.Properties["senha"].As<string>() : null
                    });
                });
                return Ok(medicos);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao buscar médicos", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        //Buscar Médico por nome
        [HttpGet("getMedico/{nome}")]
        public async Task<IActionResult> GetMedico(string nome)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var result = await session.RunAsync("MATCH (m:Medico {nome: $nome}) RETURN m", new { nome });
                var records = await result.ToListAsync();
                var record = records.SingleOrDefault();

                if (record == null) return NotFound(new { message = "Médico não encontrado." });

                var node = record["m"].As<INode>();
                return Ok(new Medico { Nome = node.Properties["nome"].As<string>(), Senha = node.Properties["senha"].As<string>() });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao buscar médico", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        //Update Senha
        [HttpPut("updateMedico")]
        public async Task<IActionResult> UpdateMedico([FromBody] Medico medico)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                
                var query = @"
                MATCH (m:Medico {nome: $nome})
                SET m.senha = $senha
                RETURN m";

                
                var result = await session.RunAsync(query, new { nome = medico.Nome, senha = medico.Senha });

                var records = await result.ToListAsync();
                if (records.Count == 0)
                {
                    return NotFound(new { message = "Médico não encontrado." });
                }

                return Ok(new { message = "Médico atualizado com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao atualizar médico", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }


        //Delete
        [HttpDelete("deleteMedico/{nome}")]
        public async Task<IActionResult> DeleteMedico(string nome)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                await session.RunAsync("MATCH (m:Medico {nome: $nome}) DETACH DELETE m", new { nome });
                return Ok(new { message = "Médico removido com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao remover médico", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
