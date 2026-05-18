using System;

namespace SZR_TechnologistApp.Models
{
    public class StepExecutionDto
    {
        public int Id { get; set; }
        public int StepNumber { get; set; }
        public string Name { get; set; }
        public string Status { get; set; } // "Не начат", "Выполняется", "Завершён"
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string ActualParams { get; set; } // JSON
        public string Instruction { get; set; }
        public bool IsMandatory { get; set; }
    }
}