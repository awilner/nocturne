namespace Nocturne.Core.Models.V4;

/// <summary>
/// Represents a physical device identified by category, type, and serial number.
/// </summary>
/// <remarks>
/// <para>
/// Devices are auto-discovered from uploaded data and tracked by their
/// <see cref="Category"/>, <see cref="Type"/>, and <see cref="Serial"/>. Other V4 record
/// types (such as <see cref="Bolus"/>, <see cref="TempBasal"/>, <see cref="PumpSnapshot"/>,
/// and <see cref="UploaderSnapshot"/>) reference a device via a <c>DeviceId</c> foreign key.
/// </para>
/// <para>
/// <see cref="FirstSeenMills"/> and <see cref="LastSeenMills"/> are computed from their
/// respective <see cref="DateTime"/> timestamp properties for v1/v3 API compatibility.
/// </para>
/// </remarks>
/// <seealso cref="DeviceCategory"/>
/// <seealso cref="PatientDevice"/>
/// <seealso cref="DeviceCatalog"/>
/// <seealso cref="DeviceCatalogEntry"/>
public class Device
{
    /// <summary>
    /// UUID v7 primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Device category discriminator (e.g. <see cref="DeviceCategory.InsulinPump"/>,
    /// <see cref="DeviceCategory.CGM"/>, <see cref="DeviceCategory.Uploader"/>).
    /// </summary>
    public DeviceCategory Category { get; set; }

    /// <summary>
    /// Device type/model name (e.g. "Omnipod DASH", "Medtronic 780G")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Device serial number
    /// </summary>
    public string Serial { get; set; } = string.Empty;

    /// <summary>
    /// When this device was first seen as UTC DateTime
    /// </summary>
    public DateTime FirstSeenTimestamp { get; set; }

    /// <summary>
    /// When this device was last seen as UTC DateTime
    /// </summary>
    public DateTime LastSeenTimestamp { get; set; }

    /// <summary>
    /// When this device was first seen in Unix milliseconds, computed from <see cref="FirstSeenTimestamp"/>.
    /// </summary>
    public long FirstSeenMills => new DateTimeOffset(FirstSeenTimestamp, TimeSpan.Zero).ToUnixTimeMilliseconds();

    /// <summary>
    /// When this device was last seen in Unix milliseconds, computed from <see cref="LastSeenTimestamp"/>.
    /// </summary>
    public long LastSeenMills => new DateTimeOffset(LastSeenTimestamp, TimeSpan.Zero).ToUnixTimeMilliseconds();

    /// <summary>
    /// Catch-all for fields not mapped to dedicated columns
    /// </summary>
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}
