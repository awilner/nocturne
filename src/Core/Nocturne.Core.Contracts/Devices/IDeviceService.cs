using Nocturne.Core.Models.V4;

namespace Nocturne.Core.Contracts.Devices;

/// <summary>
/// Service for resolving physical devices to their canonical <see cref="Guid"/> identifiers.
/// Devices are identified by a combination of <see cref="DeviceCategory"/>, type, and serial number.
/// </summary>
/// <seealso cref="DeviceCategory"/>
public interface IDeviceService
{
    /// <summary>
    /// Resolve a physical device to its canonical ID, creating the device record if it does not already exist.
    /// </summary>
    /// <param name="category">The <see cref="DeviceCategory"/> (e.g., Pump, CGM).</param>
    /// <param name="type">Device type or model name (e.g., "Omnipod 5").</param>
    /// <param name="serial">Device serial number, or <c>null</c> if unknown.</param>
    /// <param name="mills">Timestamp in Unix milliseconds when this device was seen.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The canonical device <see cref="Guid"/>, or <c>null</c> if resolution failed.</returns>
    Task<Guid?> ResolveAsync(DeviceCategory category, string? type, string? serial, long mills, CancellationToken ct = default);
}
