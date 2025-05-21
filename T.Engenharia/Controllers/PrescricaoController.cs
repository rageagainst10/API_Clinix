using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using T.Engenharia.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace T.Engenharia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrescricaoController : ControllerBase
    {
        private readonly IDriver _neo4jDriver;

        public PrescricaoController(IDriver neo4jDriver)
        {
            _neo4jDriver = neo4jDriver;
        }

        //Vincular ao médico e paciente
        [HttpPost("vincularPrescricao")]
        public async Task<IActionResult> VincularPrescricao([FromBody] Prescricao request)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var query = @"
                    MATCH (m:Medico {nome: $medicoNome})
                    MATCH (p:Paciente {nome: $pacienteNome})
                    CREATE (m)-[:PRESCREVE {descricao: $descricao}]->(p)
                ";

                await session.RunAsync(query, new
                {
                    medicoNome = request.MedicoNome,
                    pacienteNome = request.PacienteNome,
                    descricao = request.Descricao
                });

                return Ok(new { message = "Prescrição vinculada com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao vincular prescrição", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        //Listar pacientes com prescrições
        [HttpGet("getPacientesComPrescricoes")]
        public async Task<IActionResult> GetPacientesComPrescricoes()
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var query = @"
                    MATCH (p:Paciente)<-[r:PRESCREVE]-(m:Medico)
                    RETURN p.nome AS Paciente, m.nome AS Medico, r.descricao AS Prescricao
                ";

                var result = await session.RunAsync(query);
                var lista = new List<object>();

                await result.ForEachAsync(record =>
                {
                    lista.Add(new
                    {
                        Paciente = record["Paciente"].As<string>(),
                        Medico = record["Medico"].As<string>(),
                        Prescricao = record["Prescricao"].As<string>()
                    });
                });

                return Ok(lista);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao buscar prescrições", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        //Atualizar prescrição
        [HttpPut("updatePrescricao")]
        public async Task<IActionResult> UpdatePrescricao([FromBody] Prescricao request)
        {
            var session = _neo4jDriver.AsyncSession();
            try
            {
                var query = @"
            MATCH (m:Medico {nome: $medicoNome})-[r:PRESCREVE]->(p:Paciente {nome: $pacienteNome})
            SET r.descricao = $novaDescricao
            RETURN r
        ";

                var result = await session.RunAsync(query, new
                {
                    medicoNome = request.MedicoNome,
                    pacienteNome = request.PacienteNome,
                    novaDescricao = request.Descricao
                });

                var records = await result.ToListAsync();
                var record = records.SingleOrDefault();

                if (record == null)
                    return NotFound(new { message = "Prescrição não encontrada para atualizar." });

                return Ok(new { message = "Prescrição atualizada com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao atualizar prescrição", error = ex.Message });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

    }
}
