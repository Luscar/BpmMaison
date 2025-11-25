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
    public class StepInstanceRepository : IStepInstanceRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public StepInstanceRepository(string connectionString)
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

        public async Task<StepInstance> GetByIdAsync(string stepInstanceId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT STEP_INST_ID, PROC_INST_ID, STEP_DEF_ID, STEP_STATUS,
                       STEP_TYPE, INPUT_JSON, OUTPUT_JSON,
                       STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG,
                       RESUME_DT, WAITING_SIGNAL
                FROM TBL_STEP_INSTANCES
                WHERE STEP_INST_ID = :StepInstanceId";

            var entity = await connection.QuerySingleOrDefaultAsync<StepInstanceEntity>(
                sql,
                new { StepInstanceId = stepInstanceId });

            if (entity == null)
                return null;

            return MapToStepInstance(entity);
        }

        public async Task<IEnumerable<StepInstance>> GetByProcessInstanceIdAsync(string processInstanceId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT STEP_INST_ID, PROC_INST_ID, STEP_DEF_ID, STEP_STATUS,
                       STEP_TYPE, INPUT_JSON, OUTPUT_JSON,
                       STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG,
                       RESUME_DT, WAITING_SIGNAL
                FROM TBL_STEP_INSTANCES
                WHERE PROC_INST_ID = :ProcessInstanceId
                ORDER BY STARTED_DT";

            var entities = await connection.QueryAsync<StepInstanceEntity>(
                sql,
                new { ProcessInstanceId = processInstanceId });

            return entities.Select(MapToStepInstance);
        }

        public async Task<StepInstance> GetByProcessAndStepIdAsync(string processInstanceId, string stepDefinitionId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT STEP_INST_ID, PROC_INST_ID, STEP_DEF_ID, STEP_STATUS,
                       STEP_TYPE, INPUT_JSON, OUTPUT_JSON,
                       STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG,
                       RESUME_DT, WAITING_SIGNAL
                FROM TBL_STEP_INSTANCES
                WHERE PROC_INST_ID = :ProcessInstanceId
                  AND STEP_DEF_ID = :StepDefinitionId
                ORDER BY STARTED_DT DESC
                FETCH FIRST 1 ROWS ONLY";

            var entity = await connection.QuerySingleOrDefaultAsync<StepInstanceEntity>(
                sql,
                new
                {
                    ProcessInstanceId = processInstanceId,
                    StepDefinitionId = stepDefinitionId
                });

            if (entity == null)
                return null;

            return MapToStepInstance(entity);
        }

        public async Task<IEnumerable<StepInstance>> GetScheduledStepsAsync()
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT STEP_INST_ID, PROC_INST_ID, STEP_DEF_ID, STEP_STATUS,
                       STEP_TYPE, INPUT_JSON, OUTPUT_JSON,
                       STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG,
                       RESUME_DT, WAITING_SIGNAL
                FROM TBL_STEP_INSTANCES
                WHERE STEP_STATUS = 'WaitingForSchedule'
                  AND RESUME_DT IS NOT NULL
                  AND RESUME_DT <= SYSTIMESTAMP";

            var entities = await connection.QueryAsync<StepInstanceEntity>(sql);

            return entities.Select(MapToStepInstance);
        }

        public async Task SaveAsync(StepInstance stepInstance)
        {
            using var connection = CreateConnection();

            var inputJson = stepInstance.InputData != null
                ? JsonSerializer.Serialize(stepInstance.InputData, _jsonOptions)
                : null;

            var outputJson = stepInstance.OutputData != null
                ? JsonSerializer.Serialize(stepInstance.OutputData, _jsonOptions)
                : null;

            var sql = @"
                MERGE INTO TBL_STEP_INSTANCES tgt
                USING (
                    SELECT :StepInstanceId AS STEP_INST_ID FROM DUAL
                ) src
                ON (tgt.STEP_INST_ID = src.STEP_INST_ID)
                WHEN MATCHED THEN
                    UPDATE SET
                        STEP_STATUS = :Status,
                        INPUT_JSON = :InputJson,
                        OUTPUT_JSON = :OutputJson,
                        STARTED_DT = :StartedDate,
                        COMPLETED_DT = :CompletedDate,
                        FAILED_DT = :FailedDate,
                        ERROR_MSG = :ErrorMessage,
                        RESUME_DT = :ResumeDate,
                        WAITING_SIGNAL = :WaitingForSignal
                WHEN NOT MATCHED THEN
                    INSERT (STEP_INST_ID, PROC_INST_ID, STEP_DEF_ID, STEP_STATUS,
                            STEP_TYPE, INPUT_JSON, OUTPUT_JSON,
                            STARTED_DT, COMPLETED_DT, FAILED_DT, ERROR_MSG,
                            RESUME_DT, WAITING_SIGNAL)
                    VALUES (:StepInstanceId, :ProcessInstanceId, :StepDefinitionId,
                            :Status, :StepType, :InputJson, :OutputJson,
                            :StartedDate, :CompletedDate, :FailedDate, :ErrorMessage,
                            :ResumeDate, :WaitingForSignal)";

            await connection.ExecuteAsync(sql, new
            {
                StepInstanceId = stepInstance.Id,
                ProcessInstanceId = stepInstance.ProcessInstanceId,
                StepDefinitionId = stepInstance.StepDefinitionId,
                Status = stepInstance.Status.ToString(),
                StepType = stepInstance.StepType.ToString(),
                InputJson = inputJson,
                OutputJson = outputJson,
                StartedDate = stepInstance.StartedDate,
                CompletedDate = stepInstance.CompletedDate,
                FailedDate = stepInstance.FailedDate,
                ErrorMessage = stepInstance.ErrorMessage,
                ResumeDate = stepInstance.ResumeAt,
                WaitingForSignal = stepInstance.WaitingForSignal
            });
        }

        public async Task DeleteAsync(string stepInstanceId)
        {
            using var connection = CreateConnection();

            var sql = "DELETE FROM TBL_STEP_INSTANCES WHERE STEP_INST_ID = :StepInstanceId";

            await connection.ExecuteAsync(sql, new { StepInstanceId = stepInstanceId });
        }

        private StepInstance MapToStepInstance(StepInstanceEntity entity)
        {
            var inputData = !string.IsNullOrEmpty(entity.InputDataJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                    entity.InputDataJson,
                    _jsonOptions)
                : new Dictionary<string, object>();

            var outputData = !string.IsNullOrEmpty(entity.OutputDataJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                    entity.OutputDataJson,
                    _jsonOptions)
                : new Dictionary<string, object>();

            return new StepInstance
            {
                Id = entity.StepInstanceId,
                ProcessInstanceId = entity.ProcessInstanceId,
                StepDefinitionId = entity.StepDefinitionId,
                Status = Enum.Parse<StepStatus>(entity.Status),
                StepType = Enum.Parse<StepType>(entity.StepType),
                InputData = inputData,
                OutputData = outputData,
                StartedDate = entity.StartedDate,
                CompletedDate = entity.CompletedDate,
                FailedDate = entity.FailedDate,
                ErrorMessage = entity.ErrorMessage,
                ResumeAt = entity.ResumeDate,
                WaitingForSignal = entity.WaitingForSignal
            };
        }
    }
}
