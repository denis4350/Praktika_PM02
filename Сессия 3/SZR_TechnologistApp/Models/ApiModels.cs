using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SZR_TechnologistApp.Models
{
    // ========================= АВТОРИЗАЦИЯ =========================
    public class TokenResponseDto
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [JsonProperty("user")]
        public UserInfoDto User { get; set; }
    }

    public class UserInfoDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("roleId")]
        public int RoleId { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }
    }

    // ========================= ПРОДУКЦИЯ =========================
    public class ProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("productType")]
        public string ProductType { get; set; }

        [JsonProperty("form")]
        public string Form { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProductDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("productType")]
        public string ProductType { get; set; }

        [JsonProperty("form")]
        public string Form { get; set; }
    }

    public class UpdateProductDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("productType")]
        public string ProductType { get; set; }

        [JsonProperty("form")]
        public string Form { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    // ========================= РЕЦЕПТУРЫ =========================
    public class RecipeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("approvedAt")]
        public DateTime? ApprovedAt { get; set; }

        [JsonProperty("componentCount")]
        public int ComponentCount { get; set; }

        [JsonProperty("totalPercentage")]
        public decimal TotalPercentage { get; set; }
    }

    public class RecipeDetailDto
    {
        [JsonProperty("recipe")]
        public RecipeDto Recipe { get; set; }

        [JsonProperty("components")]
        public List<RecipeComponentDto> Components { get; set; }

        [JsonProperty("totalPercentage")]
        public decimal TotalPercentage { get; set; }
    }

    public class RecipeComponentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rawMaterialId")]
        public int RawMaterialId { get; set; }

        [JsonProperty("rawMaterialCode")]
        public string RawMaterialCode { get; set; }

        [JsonProperty("rawMaterialName")]
        public string RawMaterialName { get; set; }

        [JsonProperty("percentage")]
        public decimal Percentage { get; set; }

        [JsonProperty("toleranceMin")]
        public decimal? ToleranceMin { get; set; }

        [JsonProperty("toleranceMax")]
        public decimal? ToleranceMax { get; set; }

        [JsonProperty("loadOrder")]
        public int LoadOrder { get; set; }
    }

    public class CreateRecipeDto
    {
        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class UpdateRecipeComponentsDto
    {
        [JsonProperty("components")]
        public RecipeComponentDto[] Components { get; set; }
    }

    // ========================= ТЕХКАРТЫ =========================
    public class TechCardDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("approvedAt")]
        public DateTime? ApprovedAt { get; set; }

        [JsonProperty("stepCount")]
        public int StepCount { get; set; }
    }

    public class TechCardDetailDto
    {
        [JsonProperty("techCard")]
        public TechCardDto TechCard { get; set; }

        [JsonProperty("steps")]
        public List<TechStepDto> Steps { get; set; }
    }

    public class TechStepDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("stepNumber")]
        public int StepNumber { get; set; }

        [JsonProperty("stepType")]
        public string StepType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        [JsonProperty("isMandatory")]
        public bool IsMandatory { get; set; }

        [JsonProperty("plannedParams")]
        public object PlannedParams { get; set; }

        [JsonProperty("toleranceParams")]
        public object ToleranceParams { get; set; }

        [JsonProperty("equipmentId")]
        public int? EquipmentId { get; set; }

        [JsonProperty("equipmentName")]
        public string EquipmentName { get; set; }
    }

    public class CreateTechCardDto
    {
        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class AddTechStepDto
    {
        [JsonProperty("stepType")]
        public string StepType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        [JsonProperty("isMandatory")]
        public bool IsMandatory { get; set; }

        [JsonProperty("plannedParams")]
        public object PlannedParams { get; set; }

        [JsonProperty("toleranceParams")]
        public object ToleranceParams { get; set; }

        [JsonProperty("equipmentId")]
        public int? EquipmentId { get; set; }
    }

    public class UpdateTechStepDto
    {
        [JsonProperty("stepType")]
        public string StepType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        [JsonProperty("isMandatory")]
        public bool? IsMandatory { get; set; }

        [JsonProperty("plannedParams")]
        public object PlannedParams { get; set; }

        [JsonProperty("toleranceParams")]
        public object ToleranceParams { get; set; }

        [JsonProperty("equipmentId")]
        public int? EquipmentId { get; set; }
    }

    public class ReorderStepsDto
    {
        [JsonProperty("items")]
        public ReorderStepItemDto[] Items { get; set; }
    }

    public class ReorderStepItemDto
    {
        [JsonProperty("stepId")]
        public int StepId { get; set; }

        [JsonProperty("stepNumber")]
        public int StepNumber { get; set; }
    }

    // ========================= ЗАКАЗЫ =========================
    public class ProductionOrderDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("plannedQuantity")]
        public decimal PlannedQuantity { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("plannedStartDate")]
        public DateTime? PlannedStartDate { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("batchesCount")]
        public int BatchesCount { get; set; }
    }

    public class CreateOrderDto
    {
        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("plannedQuantity")]
        public decimal PlannedQuantity { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("plannedStartDate")]
        public DateTime? PlannedStartDate { get; set; }
    }

    public class UpdateOrderDto
    {
        [JsonProperty("plannedQuantity")]
        public decimal? PlannedQuantity { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("plannedStartDate")]
        public DateTime? PlannedStartDate { get; set; }
    }

    public class CreateBatchFromOrderDto
    {
        [JsonProperty("line")]
        public string Line { get; set; }

        [JsonProperty("equipmentId")]
        public int? EquipmentId { get; set; }
    }

    // ========================= ПАРТИИ =========================
    public class ProductionBatchDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("line")]
        public string Line { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("labStatus")]
        public string LabStatus { get; set; }

        [JsonProperty("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonProperty("finishedAt")]
        public DateTime? FinishedAt { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class ActiveBatchDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("line")]
        public string Line { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("labStatus")]
        public string LabStatus { get; set; }

        [JsonProperty("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonProperty("currentStep")]
        public int? CurrentStep { get; set; }

        [JsonProperty("currentStepName")]
        public string CurrentStepName { get; set; }

        [JsonProperty("currentStepStatus")]
        public string CurrentStepStatus { get; set; }

        [JsonProperty("hasWarning")]
        public bool HasWarning { get; set; }

        [JsonProperty("hasCriticalDeviation")]
        public bool HasCriticalDeviation { get; set; }
    }

    public class BatchProgramDto
    {
        [JsonProperty("batch")]
        public ProductionBatchDto Batch { get; set; }

        [JsonProperty("currentStep")]
        public object CurrentStep { get; set; }

        [JsonProperty("steps")]
        public List<TechStepDto> Steps { get; set; }
    }

    // ========================= ОТКЛОНЕНИЯ =========================
    public class DeviationEventDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("batchId")]
        public int BatchId { get; set; }

        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("plannedValue")]
        public string PlannedValue { get; set; }

        [JsonProperty("actualValue")]
        public string ActualValue { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class RegisterDeviationDto
    {
        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("stepNumber")]
        public int? StepNumber { get; set; }

        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("plannedValue")]
        public string PlannedValue { get; set; }

        [JsonProperty("actualValue")]
        public string ActualValue { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }
    }

    // ========================= DASHBOARD =========================
    public class DashboardDataDto
    {
        [JsonProperty("kpis")]
        public DashboardKpisDto KPIs { get; set; }

        [JsonProperty("recentEvents")]
        public List<RecentEventDto> RecentEvents { get; set; }

        [JsonProperty("criticalDeviations")]
        public List<CriticalDeviationDto> CriticalDeviations { get; set; }

        [JsonProperty("batchesForAnalysis")]
        public List<BatchForAnalysisDto> BatchesForAnalysis { get; set; }
        [JsonProperty("batchesWaitingLab")]
        public int BatchesWaitingLab { get; set; }
    }

    public class DashboardKpisDto
    {
        [JsonProperty("activeProducts")]
        public int ActiveProducts { get; set; }

        [JsonProperty("activeRecipes")]
        public int ActiveRecipes { get; set; }

        [JsonProperty("activeTechCards")]
        public int ActiveTechCards { get; set; }

        [JsonProperty("ordersInProgress")]
        public int OrdersInProgress { get; set; }

        [JsonProperty("batchesInProduction")]
        public int BatchesInProduction { get; set; }

        [JsonProperty("batchesWithDeviations")]
        public int BatchesWithDeviations { get; set; }

        [JsonProperty("batchesWaitingLab")]
        public int BatchesWaitingLab { get; set; }
    }

    public class RecentEventDto
    {
        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }
    }

    public class CriticalDeviationDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("actualValue")]
        public string ActualValue { get; set; }

        [JsonProperty("plannedValue")]
        public string PlannedValue { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("occurredAt")]
        public DateTime OccurredAt { get; set; }
    }

    public class BatchForAnalysisDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("labStatus")]
        public string LabStatus { get; set; }

        [JsonProperty("finishedAt")]
        public DateTime? FinishedAt { get; set; }

        [JsonProperty("deviationCount")]
        public int DeviationCount { get; set; }
    }

    // ========================= ЭКСТРУДЕР =========================
    public class ExtruderProgramDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("activatedAt")]
        public DateTime? ActivatedAt { get; set; }

        [JsonProperty("zoneCount")]
        public int ZoneCount { get; set; }
    }

    public class ExtruderProgramDetailDto
    {
        [JsonProperty("program")]
        public ExtruderProgramDto Program { get; set; }

        [JsonProperty("zones")]
        public List<ExtruderZoneDto> Zones { get; set; }
    }

    public class ExtruderZoneDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("zoneNumber")]
        public int ZoneNumber { get; set; }

        [JsonProperty("zoneName")]
        public string ZoneName { get; set; }

        [JsonProperty("temperatureSetpoint")]
        public decimal TemperatureSetpoint { get; set; }

        [JsonProperty("temperatureMin")]
        public decimal TemperatureMin { get; set; }

        [JsonProperty("temperatureMax")]
        public decimal TemperatureMax { get; set; }

        [JsonProperty("pressureSetpoint")]
        public decimal PressureSetpoint { get; set; }

        [JsonProperty("pressureMin")]
        public decimal PressureMin { get; set; }

        [JsonProperty("pressureMax")]
        public decimal PressureMax { get; set; }

        [JsonProperty("screwSpeed")]
        public int ScrewSpeed { get; set; }

        [JsonProperty("feedRate")]
        public int FeedRate { get; set; }
    }

    public class CreateExtruderProgramDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("zones")]
        public List<ExtruderZoneDto> Zones { get; set; }
    }

    public class TelemetryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("zoneNumber")]
        public int ZoneNumber { get; set; }

        [JsonProperty("currentTemperature")]
        public decimal CurrentTemperature { get; set; }

        [JsonProperty("currentPressure")]
        public decimal CurrentPressure { get; set; }

        [JsonProperty("currentSpeed")]
        public int CurrentSpeed { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    // ========================= СПРАВОЧНИКИ =========================
    public class EquipmentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("line")]
        public string Line { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }
    }

    public class RoleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class StatusItem
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }
    }

    // ========================= СЫРЬЁ =========================
    public class RawMaterialDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }
    }
    public class NotificationDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("isRead")]
        public bool IsRead { get; set; }
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
    public class ComponentItemDto
    {
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public decimal Percentage { get; set; }
        public decimal? ToleranceMin { get; set; }
        public decimal? ToleranceMax { get; set; }
        public int LoadOrder { get; set; }
    }
    public class UpdateComponentsDto
    {
        [JsonProperty("components")]
        public List<ComponentDto> Components { get; set; }
    }

    public class ComponentDto
    {
        [JsonProperty("rawMaterialId")]
        public int RawMaterialId { get; set; }
        [JsonProperty("percentage")]
        public decimal Percentage { get; set; }
        [JsonProperty("toleranceMin")]
        public decimal? ToleranceMin { get; set; }
        [JsonProperty("toleranceMax")]
        public decimal? ToleranceMax { get; set; }
        [JsonProperty("loadOrder")]
        public int LoadOrder { get; set; }
    }
    public class BatchReportItem
    {
        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("hasDeviations")]
        public bool HasDeviations { get; set; }

        [JsonProperty("labDecision")]
        public string LabDecision { get; set; }
    }

    public class DeviationReportItem
    {
        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonProperty("stepNumber")]
        public int? StepNumber { get; set; }

        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("plannedValue")]
        public string PlannedValue { get; set; }

        [JsonProperty("actualValue")]
        public string ActualValue { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}