using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using BpmEngine.Core.Models;
using BpmEngine.Repository;
using BpmClient.Models;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace BpmClient.Repositories
{
    public class ProcessDefinitionRepository : IProcessDefinitionRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProcessDefinitionRepository(string connectionString)
        {
            _connectionString = connectionString;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }

        public async Task<ProcessDefinition> GetByIdAsync(string processId, int? version = null)
        {
            using var connection = CreateConnection();

            string sql;
            ProcessDefinitionEntity entity;

            if (version.HasValue)
            {
                // Get specific version
                sql = @"
                    SELECT DEF_ID, DEF_NAME, DEF_DESCRIPTION, DEF_VERSION,
                           DEF_JSON, DEF_CREATED_DT, DEF_UPDATED_DT
                    FROM TBL_PROC_DEFINITIONS
                    WHERE DEF_ID = :ProcessId AND DEF_VERSION = :Version";

                entity = await connection.QuerySingleOrDefaultAsync<ProcessDefinitionEntity>(
                    sql,
                    new { ProcessId = processId, Version = version.Value });
            }
            else
            {
                // Get latest version
                sql = @"
                    SELECT DEF_ID, DEF_NAME, DEF_DESCRIPTION, DEF_VERSION,
                           DEF_JSON, DEF_CREATED_DT, DEF_UPDATED_DT
                    FROM TBL_PROC_DEFINITIONS
                    WHERE DEF_ID = :ProcessId
                    ORDER BY DEF_VERSION DESC
                    FETCH FIRST 1 ROWS ONLY";

                entity = await connection.QuerySingleOrDefaultAsync<ProcessDefinitionEntity>(
                    sql,
                    new { ProcessId = processId });
            }

            if (entity == null)
                return null;

            // Deserialize JSON content to ProcessDefinition
            return JsonSerializer.Deserialize<ProcessDefinition>(
                entity.JsonContent,
                _jsonOptions);
        }

        public async Task SaveAsync(ProcessDefinition processDefinition)
        {
            using var connection = CreateConnection();

            var jsonContent = JsonSerializer.Serialize(processDefinition, _jsonOptions);

            var sql = @"
                MERGE INTO TBL_PROC_DEFINITIONS tgt
                USING (
                    SELECT :Id AS DEF_ID, :Version AS DEF_VERSION FROM DUAL
                ) src
                ON (tgt.DEF_ID = src.DEF_ID AND tgt.DEF_VERSION = src.DEF_VERSION)
                WHEN MATCHED THEN
                    UPDATE SET
                        DEF_NAME = :Name,
                        DEF_DESCRIPTION = :Description,
                        DEF_JSON = :JsonContent,
                        DEF_UPDATED_DT = SYSTIMESTAMP
                WHEN NOT MATCHED THEN
                    INSERT (DEF_ID, DEF_NAME, DEF_DESCRIPTION, DEF_VERSION,
                            DEF_JSON, DEF_CREATED_DT, DEF_UPDATED_DT)
                    VALUES (:Id, :Name, :Description, :Version,
                            :JsonContent, SYSTIMESTAMP, SYSTIMESTAMP)";

            await connection.ExecuteAsync(sql, new
            {
                processDefinition.Id,
                processDefinition.Name,
                processDefinition.Description,
                processDefinition.Version,
                JsonContent = jsonContent
            });
        }

        public async Task DeleteAsync(string processId, int? version = null)
        {
            using var connection = CreateConnection();

            string sql;
            object parameters;

            if (version.HasValue)
            {
                sql = "DELETE FROM TBL_PROC_DEFINITIONS WHERE DEF_ID = :ProcessId AND DEF_VERSION = :Version";
                parameters = new { ProcessId = processId, Version = version.Value };
            }
            else
            {
                sql = "DELETE FROM TBL_PROC_DEFINITIONS WHERE DEF_ID = :ProcessId";
                parameters = new { ProcessId = processId };
            }

            await connection.ExecuteAsync(sql, parameters);
        }
    }
}
