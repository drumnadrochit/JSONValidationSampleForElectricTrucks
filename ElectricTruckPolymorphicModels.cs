using System.Text.Json.Serialization;

namespace ElectricTruckJsonValidator;

public sealed class ElectricTruckDocument
{
	public string? FleetId { get; init; }
	public MaintenanceSnapshot? Maintenance { get; init; }
}

public sealed class MaintenanceSnapshot
{
	public DateTimeOffset? LastServiceDate { get; init; }
	public int? NextServiceDueKm { get; init; }
	public List<MaintenanceTask> Tasks { get; init; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "taskType", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(BrakeCheckTask), typeDiscriminator: "BRAKE_CHECK")]
[JsonDerivedType(typeof(CoolantLoopInspectionTask), typeDiscriminator: "COOLANT_LOOP_INSPECTION")]
public class MaintenanceTask
{
	public string? TaskCode { get; init; }
	public int? Priority { get; init; }
	public bool? Completed { get; init; }
	public string? Notes { get; init; }
}

public sealed class BrakeCheckTask : MaintenanceTask
{
	public int? AxleNumber { get; init; }
	public double? PadThicknessMm { get; init; }
}

public sealed class CoolantLoopInspectionTask : MaintenanceTask
{
	public string? CoolantLoop { get; init; }
	public bool? LeakDetected { get; init; }
}