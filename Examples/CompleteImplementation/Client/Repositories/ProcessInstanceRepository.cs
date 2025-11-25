using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BpmEngine.Core.Models;
using BpmEngine.Repository;
using BpmClient.Models;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace BpmClient.Repositories
{
    public class ProcessInstanceRepository : IProcessInstanceRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProcessInstanceRepository(string connectionString)
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

        public async Task<ProcessInstance> GetByIdAsync(string instanceId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT INST_ID, PROC_DEF_ID, PROC_DEF_VER, INST_STATUS,
                       CURRENT_STEP, PARENT_INST_ID, VARS_JSON,
                       STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG
                FROM TBL_PROC_INSTANCES
                WHERE INST_ID = :InstanceId";

            var entity = await connection.QuerySingleOrDefaultAsync<ProcessInstanceEntity>(
                sql,
                new { InstanceId = instanceId });

            if (entity == null)
                return null;

            return MapToProcessInstance(entity);
        }

        public async Task<IEnumerable<ProcessInstance>> GetByStatusAsync(ProcessStatus status)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT INST_ID, PROC_DEF_ID, PROC_DEF_VER, INST_STATUS,
                       CURRENT_STEP, PARENT_INST_ID, VARS_JSON,
                       STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG
                FROM TBL_PROC_INSTANCES
                WHERE INST_STATUS = :Status";

            var entities = await connection.QueryAsync<ProcessInstanceEntity>(
                sql,
                new { Status = status.ToString() });

            return entities.Select(MapToProcessInstance);
        }

        public async Task<IEnumerable<ProcessInstance>> GetByParentIdAsync(string parentInstanceId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT INST_ID, PROC_DEF_ID, PROC_DEF_VER, INST_STATUS,
                       CURRENT_STEP, PARENT_INST_ID, VARS_JSON,
                       STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG
                FROM TBL_PROC_INSTANCES
                WHERE PARENT_INST_ID = :ParentInstanceId";

            var entities = await connection.QueryAsync<ProcessInstanceEntity>(
                sql,
                new { ParentInstanceId = parentInstanceId });

            return entities.Select(MapToProcessInstance);
        }

        public async Task SaveAsync(ProcessInstance instance)
        {
            using var connection = CreateConnection();

            var variablesJson = instance.Variables != null
                ? JsonSerializer.Serialize(instance.Variables, _jsonOptions)
                : null;

            var sql = @"
                MERGE INTO TBL_PROC_INSTANCES tgt
                USING (
                    SELECT :InstanceId AS INST_ID FROM DUAL
                ) src
                ON (tgt.INST_ID = src.INST_ID)
                WHEN MATCHED THEN
                    UPDATE SET
                        INST_STATUS = :Status,
                        CURRENT_STEP = :CurrentStepId,
                        VARS_JSON = :VariablesJson,
                        STARTED_DT = :StartedDate,
                        COMPLETED_DT = :CompletedDate,
                        FAILED_DT = :FailedDate,
                        ERROR_MSG = :ErrorMessage
                WHEN NOT MATCHED THEN
                    INSERT (INST_ID, PROC_DEF_ID, PROC_DEF_VER, INST_STATUS,
                            CURRENT_STEP, PARENT_INST_ID, VARS_JSON,
                            STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG)
                    VALUES (:InstanceId, :ProcessDefinitionId, :ProcessDefinitionVersion,
                            :Status, :CurrentStepId, :ParentInstanceId, :VariablesJson,
                            :StartedDate, :CompletedDate, :FailedDate, :ErrorMessage)";

            await connection.ExecuteAsync(sql, new
            {
                InstanceId = instance.Id,
                ProcessDefinitionId = instance.ProcessDefinitionId,
                ProcessDefinitionVersion = instance.ProcessDefinitionVersion,
                Status = instance.Status.ToString(),
                CurrentStepId = instance.CurrentStepId,
                ParentInstanceId = instance.ParentProcessInstanceId,
                VariablesJson = variablesJson,
                StartedDate = instance.StartedDate,
                CompletedDate = instance.CompletedDate,
                FailedDate = instance.FailedDate,
                ErrorMessage = instance.ErrorMessage
            });
        }

        public async Task DeleteAsync(string instanceId)
        {
            using var connection = CreateConnection();

            var sql = "DELETE FROM TBL_PROC_INSTANCES WHERE INST_ID = :InstanceId";

            await connection.ExecuteAsync(sql, new { InstanceId = instanceId });
        }

        private ProcessInstance MapToProcessInstance(ProcessInstanceEntity entity)
        {
            var variables = !string.IsNullOrEmpty(entity.VariablesJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                    entity.VariablesJson,
                    _jsonOptions)
                : new Dictionary<string, object>();

            return new ProcessInstance
            {
                Id = entity.InstanceId,
                ProcessDefinitionId = entity.ProcessDefinitionId,
                ProcessDefinitionVersion = entity.ProcessDefinitionVersion,
                Status = Enum.Parse<ProcessStatus>(entity.Status),
                CurrentStepId = entity.CurrentStepId,
                ParentProcessInstanceId = entity.ParentInstanceId,
                Variables = variables,
                StartedDate = entity.StartedDate,
                CompletedDate = entity.CompletedDate,
                FailedDate = entity.FailedDate,
                ErrorMessage = entity.ErrorMessage
            };
        }
    }
}
