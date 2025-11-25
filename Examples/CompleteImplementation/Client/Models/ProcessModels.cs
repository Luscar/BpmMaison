using System;
using System.Collections.Generic;
using Dapper;

namespace BpmClient.Models
{
    /// <summary>
    /// Process Definition Model
    /// Database columns use DEF_ prefix, model uses standard names
    /// </summary>
    public class ProcessDefinitionEntity
    {
        [Column("DEF_ID")]
        public string Id { get; set; }

        [Column("DEF_NAME")]
        public string Name { get; set; }

        [Column("DEF_DESCRIPTION")]
        public string Description { get; set; }

        [Column("DEF_VERSION")]
        public int Version { get; set; }

        [Column("DEF_JSON")]
        public string JsonContent { get; set; }

        [Column("DEF_CREATED_DT")]
        public DateTime CreatedDate { get; set; }

        [Column("DEF_UPDATED_DT")]
        public DateTime UpdatedDate { get; set; }
    }

    /// <summary>
    /// Process Instance Model
    /// Database uses abbreviated column names
    /// </summary>
    public class ProcessInstanceEntity
    {
        [Column("INST_ID")]
        public string InstanceId { get; set; }

        [Column("PROC_DEF_ID")]
        public string ProcessDefinitionId { get; set; }

        [Column("PROC_DEF_VER")]
        public int ProcessDefinitionVersion { get; set; }

        [Column("INST_STATUS")]
        public string Status { get; set; }

        [Column("CURRENT_STEP")]
        public string CurrentStepId { get; set; }

        [Column("PARENT_INST_ID")]
        public string ParentInstanceId { get; set; }

        [Column("VARS_JSON")]
        public string VariablesJson { get; set; }

        [Column("STARTED_DT")]
        public DateTime? StartedDate { get; set; }

        [Column("COMPLETED_DT")]
        public DateTime? CompletedDate { get; set; }

        [Column("FAILED_DT")]
        public DateTime? FailedDate { get; set; }

        [Column("ERROR_MSG")]
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Step Instance Model
    /// Database uses TBL_STEP_INSTANCES with abbreviated names
    /// </summary>
    public class StepInstanceEntity
    {
        [Column("STEP_INST_ID")]
        public string StepInstanceId { get; set; }

        [Column("PROC_INST_ID")]
        public string ProcessInstanceId { get; set; }

        [Column("STEP_DEF_ID")]
        public string StepDefinitionId { get; set; }

        [Column("STEP_STATUS")]
        public string Status { get; set; }

        [Column("STEP_TYPE")]
        public string StepType { get; set; }

        [Column("INPUT_JSON")]
        public string InputDataJson { get; set; }

        [Column("OUTPUT_JSON")]
        public string OutputDataJson { get; set; }

        [Column("STARTED_DT")]
        public DateTime? StartedDate { get; set; }

        [Column("COMPLETED_DT")]
        public DateTime? CompletedDate { get; set; }

        [Column("FAILED_DT")]
        public DateTime? FailedDate { get; set; }

        [Column("ERROR_MSG")]
        public string ErrorMessage { get; set; }

        [Column("RESUME_DT")]
        public DateTime? ResumeDate { get; set; }

        [Column("WAITING_SIGNAL")]
        public string WaitingForSignal { get; set; }
    }

    /// <summary>
    /// Task Instance Model
    /// Database uses TBL_TASK_INSTANCES
    /// </summary>
    public class TaskInstanceEntity
    {
        [Column("TASK_ID")]
        public string TaskId { get; set; }

        [Column("PROC_INST_ID")]
        public string ProcessInstanceId { get; set; }

        [Column("STEP_INST_ID")]
        public string StepInstanceId { get; set; }

        [Column("TASK_TYPE")]
        public string TaskType { get; set; }

        [Column("TASK_STATUS")]
        public string Status { get; set; }

        [Column("ASSIGNED_ROLE")]
        public string AssignedRole { get; set; }

        [Column("ASSIGNED_USER")]
        public string AssignedUser { get; set; }

        [Column("TASK_DATA_JSON")]
        public string TaskDataJson { get; set; }

        [Column("RESULT_JSON")]
        public string ResultJson { get; set; }

        [Column("CREATED_DT")]
        public DateTime CreatedDate { get; set; }

        [Column("COMPLETED_DT")]
        public DateTime? CompletedDate { get; set; }
    }
}
