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
    public class TaskRepository : ITaskRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public TaskRepository(string connectionString)
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

        public async Task<TaskInstance> GetByIdAsync(string taskId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT TASK_ID, PROC_INST_ID, STEP_INST_ID, TASK_TYPE,
                       TASK_STATUS, ASSIGNED_ROLE, ASSIGNED_USER,
                       TASK_DATA_JSON, RESULT_JSON,
                       CREATED_DT, COMPLETED_DT
                FROM TBL_TASK_INSTANCES
                WHERE TASK_ID = :TaskId";

            var entity = await connection.QuerySingleOrDefaultAsync<TaskInstanceEntity>(
                sql,
                new { TaskId = taskId });

            if (entity == null)
                return null;

            return MapToTaskInstance(entity);
        }

        public async Task<IEnumerable<TaskInstance>> GetByUserAsync(string userId, TaskStatus? status = null)
        {
            using var connection = CreateConnection();

            string sql;
            object parameters;

            if (status.HasValue)
            {
                sql = @"
                    SELECT TASK_ID, PROC_INST_ID, STEP_INST_ID, TASK_TYPE,
                           TASK_STATUS, ASSIGNED_ROLE, ASSIGNED_USER,
                           TASK_DATA_JSON, RESULT_JSON,
                           CREATED_DT, COMPLETED_DT
                    FROM TBL_TASK_INSTANCES
                    WHERE ASSIGNED_USER = :UserId
                      AND TASK_STATUS = :Status
                    ORDER BY CREATED_DT DESC";

                parameters = new { UserId = userId, Status = status.ToString() };
            }
            else
            {
                sql = @"
                    SELECT TASK_ID, PROC_INST_ID, STEP_INST_ID, TASK_TYPE,
                           TASK_STATUS, ASSIGNED_ROLE, ASSIGNED_USER,
                           TASK_DATA_JSON, RESULT_JSON,
                           CREATED_DT, COMPLETED_DT
                    FROM TBL_TASK_INSTANCES
                    WHERE ASSIGNED_USER = :UserId
                    ORDER BY CREATED_DT DESC";

                parameters = new { UserId = userId };
            }

            var entities = await connection.QueryAsync<TaskInstanceEntity>(sql, parameters);

            return entities.Select(MapToTaskInstance);
        }

        public async Task<IEnumerable<TaskInstance>> GetByRoleAsync(string role, TaskStatus? status = null)
        {
            using var connection = CreateConnection();

            string sql;
            object parameters;

            if (status.HasValue)
            {
                sql = @"
                    SELECT TASK_ID, PROC_INST_ID, STEP_INST_ID, TASK_TYPE,
                           TASK_STATUS, ASSIGNED_ROLE, ASSIGNED_USER,
                           TASK_DATA_JSON, RESULT_JSON,
                           CREATED_DT, COMPLETED_DT
                    FROM TBL_TASK_INSTANCES
                    WHERE ASSIGNED_ROLE = :Role
                      AND TASK_STATUS = :Status
                    ORDER BY CREATED_DT DESC";

                parameters = new { Role = role, Status = status.ToString() };
            }
            else
            {
                sql = @"
                    SELECT TASK_ID, PROC_INST_ID, STEP_INST_ID, TASK_TYPE,
                           TASK_STATUS, ASSIGNED_ROLE, ASSIGNED_USER,
                           TASK_DATA_JSON, RESULT_JSON,
                           CREATED_DT, COMPLETED_DT
                    FROM TBL_TASK_INSTANCES
                    WHERE ASSIGNED_ROLE = :Role
                    ORDER BY CREATED_DT DESC";

                parameters = new { Role = role };
            }

            var entities = await connection.QueryAsync<TaskInstanceEntity>(sql, parameters);

            return entities.Select(MapToTaskInstance);
        }

        public async Task<IEnumerable<TaskInstance>> GetByProcessInstanceIdAsync(string processInstanceId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT TASK_ID, PROC_INST_ID, STEP_INST_ID, TASK_TYPE,
                       TASK_STATUS, ASSIGNED_ROLE, ASSIGNED_USER,
                       TASK_DATA_JSON, RESULT_JSON,
                       CREATED_DT, COMPLETED_DT
                FROM TBL_TASK_INSTANCES
                WHERE PROC_INST_ID = :ProcessInstanceId
                ORDER BY CREATED_DT DESC";

            var entities = await connection.QueryAsync<TaskInstanceEntity>(
                sql,
                new { ProcessInstanceId = processInstanceId });

            return entities.Select(MapToTaskInstance);
        }

        public async Task SaveAsync(TaskInstance task)
        {
            using var connection = CreateConnection();

            var taskDataJson = task.TaskData != null
                ? JsonSerializer.Serialize(task.TaskData, _jsonOptions)
                : null;

            var resultJson = task.Result != null
                ? JsonSerializer.Serialize(task.Result, _jsonOptions)
                : null;

            var sql = @"
                MERGE INTO TBL_TASK_INSTANCES tgt
                USING (
                    SELECT :TaskId AS TASK_ID FROM DUAL
                ) src
                ON (tgt.TASK_ID = src.TASK_ID)
                WHEN MATCHED THEN
                    UPDATE SET
                        TASK_STATUS = :Status,
                        ASSIGNED_USER = :AssignedUser,
                        RESULT_JSON = :ResultJson,
                        COMPLETED_DT = :CompletedDate
                WHEN NOT MATCHED THEN
                    INSERT (TASK_ID, PROC_INST_ID, STEP_INST_ID, TASK_TYPE,
                            TASK_STATUS, ASSIGNED_ROLE, ASSIGNED_USER,
                            TASK_DATA_JSON, RESULT_JSON,
                            CREATED_DT, COMPLETED_DT)
                    VALUES (:TaskId, :ProcessInstanceId, :StepInstanceId, :TaskType,
                            :Status, :AssignedRole, :AssignedUser,
                            :TaskDataJson, :ResultJson,
                            :CreatedDate, :CompletedDate)";

            await connection.ExecuteAsync(sql, new
            {
                TaskId = task.Id,
                ProcessInstanceId = task.ProcessInstanceId,
                StepInstanceId = task.StepInstanceId,
                TaskType = task.TaskType,
                Status = task.Status.ToString(),
                AssignedRole = task.AssignedRole,
                AssignedUser = task.AssignedUser,
                TaskDataJson = taskDataJson,
                ResultJson = resultJson,
                CreatedDate = task.CreatedDate,
                CompletedDate = task.CompletedDate
            });
        }

        public async Task DeleteAsync(string taskId)
        {
            using var connection = CreateConnection();

            var sql = "DELETE FROM TBL_TASK_INSTANCES WHERE TASK_ID = :TaskId";

            await connection.ExecuteAsync(sql, new { TaskId = taskId });
        }

        private TaskInstance MapToTaskInstance(TaskInstanceEntity entity)
        {
            var taskData = !string.IsNullOrEmpty(entity.TaskDataJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                    entity.TaskDataJson,
                    _jsonOptions)
                : new Dictionary<string, object>();

            var result = !string.IsNullOrEmpty(entity.ResultJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                    entity.ResultJson,
                    _jsonOptions)
                : null;

            return new TaskInstance
            {
                Id = entity.TaskId,
                ProcessInstanceId = entity.ProcessInstanceId,
                StepInstanceId = entity.StepInstanceId,
                TaskType = entity.TaskType,
                Status = Enum.Parse<TaskStatus>(entity.Status),
                AssignedRole = entity.AssignedRole,
                AssignedUser = entity.AssignedUser,
                TaskData = taskData,
                Result = result,
                CreatedDate = entity.CreatedDate,
                CompletedDate = entity.CompletedDate
            };
        }
    }
}
