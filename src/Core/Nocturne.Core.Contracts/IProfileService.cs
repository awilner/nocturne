using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nocturne.Core.Models;

namespace Nocturne.Core.Contracts;

/// <summary>
/// Unified profile interface for both COB and IOB calculations with 1:1 legacy compatibility.
/// Must match profile functions from ClientApp/lib/profilefunctions.js.
/// </summary>
/// <remarks>
/// This service is stateful: call <see cref="LoadData"/> to initialize profile data and
/// <see cref="UpdateTreatments"/> to supply treatment overlays before querying time-based values.
/// Profile lookups are time-aware -- the active profile at a given timestamp is resolved from
/// the profile switch treatment history.
/// </remarks>
/// <seealso cref="Profile"/>
/// <seealso cref="Treatment"/>
/// <seealso cref="IIobService"/>
/// <seealso cref="IProfileDataService"/>
public interface IProfileService
{
    // Core profile data management

    /// <summary>
    /// Load <see cref="Profile"/> data into the service for time-based lookups.
    /// </summary>
    /// <param name="profileData">List of <see cref="Profile"/> records to load.</param>
    void LoadData(List<Profile> profileData);

    /// <summary>
    /// Check whether profile data has been loaded via <see cref="LoadData"/>.
    /// </summary>
    /// <returns><c>true</c> if profile data is available; <c>false</c> otherwise.</returns>
    bool HasData();

    /// <summary>
    /// Clear all loaded profile data and treatment overlays.
    /// </summary>
    void Clear();

    // Profile retrieval and selection

    /// <summary>
    /// Get the <see cref="Profile"/> active at a given time.
    /// </summary>
    /// <param name="time">Optional timestamp in Unix milliseconds. Defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name to retrieve.</param>
    /// <returns>The active <see cref="Profile"/>, or <c>null</c> if no data is loaded.</returns>
    Profile? GetCurrentProfile(long? time = null, string? specProfile = null);

    /// <summary>
    /// Get the name of the active profile at a given time.
    /// </summary>
    /// <param name="time">Optional timestamp in Unix milliseconds. Defaults to now.</param>
    /// <returns>Active profile name, or <c>null</c> if no data is loaded.</returns>
    string? GetActiveProfileName(long? time = null);

    /// <summary>
    /// List all available basal profile names.
    /// </summary>
    /// <returns>List of basal profile names from the loaded <see cref="Profile"/> data.</returns>
    List<string> ListBasalProfiles();

    /// <summary>
    /// Get the glucose units (mg/dL or mmol/L) for a profile.
    /// </summary>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>Units string (e.g., "mg/dl" or "mmol"), or <c>null</c> if unavailable.</returns>
    string? GetUnits(string? specProfile = null);

    /// <summary>
    /// Get the timezone for a profile.
    /// </summary>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>IANA timezone string, or <c>null</c> if unavailable.</returns>
    string? GetTimezone(string? specProfile = null);

    // Time-based value retrieval (core legacy functionality)

    /// <summary>
    /// Retrieve a time-varying profile value by type name.
    /// </summary>
    /// <remarks>
    /// This is the core lookup used by legacy Nightscout. Valid value types include
    /// "dia", "sens", "carbratio", "carbs_hr", "target_low", "target_high", and "basal".
    /// </remarks>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="valueType">Profile value type name (e.g., "sens", "carbratio").</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>The profile value active at <paramref name="time"/>.</returns>
    double GetValueByTime(long time, string valueType, string? specProfile = null);

    // Specific profile values (for COB/IOB calculations)

    /// <summary>
    /// Get Duration of Insulin Action (DIA) in hours at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>DIA in hours.</returns>
    double GetDIA(long time, string? specProfile = null);

    /// <summary>
    /// Get insulin sensitivity factor (ISF) in mg/dL per unit at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>ISF in mg/dL per unit of insulin.</returns>
    double GetSensitivity(long time, string? specProfile = null);

    /// <summary>
    /// Get carb ratio (grams of carbs per unit of insulin) at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>Carb ratio in grams per unit.</returns>
    double GetCarbRatio(long time, string? specProfile = null);

    /// <summary>
    /// Get carb absorption rate (grams per hour) at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>Carb absorption rate in grams per hour.</returns>
    double GetCarbAbsorptionRate(long time, string? specProfile = null);

    /// <summary>
    /// Get the low BG target (mg/dL) at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>Low BG target in mg/dL.</returns>
    double GetLowBGTarget(long time, string? specProfile = null);

    /// <summary>
    /// Get the high BG target (mg/dL) at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>High BG target in mg/dL.</returns>
    double GetHighBGTarget(long time, string? specProfile = null);

    /// <summary>
    /// Get the scheduled basal rate (U/hr) at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>Basal rate in units per hour.</returns>
    double GetBasalRate(long time, string? specProfile = null);

    // Treatment integration

    /// <summary>
    /// Update the treatment overlays used for profile switch, temp basal, and combo bolus resolution.
    /// </summary>
    /// <param name="profileTreatments">Profile switch <see cref="Treatment"/> records.</param>
    /// <param name="tempBasalTreatments">Temp basal <see cref="Treatment"/> records.</param>
    /// <param name="comboBolusTreatments">Combo bolus <see cref="Treatment"/> records.</param>
    void UpdateTreatments(
        List<Treatment>? profileTreatments = null,
        List<Treatment>? tempBasalTreatments = null,
        List<Treatment>? comboBolusTreatments = null
    );

    /// <summary>
    /// Get the active profile switch <see cref="Treatment"/> at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <returns>The active profile switch <see cref="Treatment"/>, or <c>null</c> if none.</returns>
    Treatment? GetActiveProfileTreatment(long time);

    /// <summary>
    /// Get the active temp basal <see cref="Treatment"/> at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <returns>The active temp basal <see cref="Treatment"/>, or <c>null</c> if none.</returns>
    Treatment? GetTempBasalTreatment(long time);

    /// <summary>
    /// Get the active combo bolus <see cref="Treatment"/> at a given time.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <returns>The active combo bolus <see cref="Treatment"/>, or <c>null</c> if none.</returns>
    Treatment? GetComboBolusTreatment(long time);

    /// <summary>
    /// Get the effective temp basal result at a given time, factoring in temp basals and combo boluses.
    /// </summary>
    /// <param name="time">Timestamp in Unix milliseconds.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>A <see cref="TempBasalResult"/> describing the effective basal at <paramref name="time"/>.</returns>
    TempBasalResult GetTempBasal(long time, string? specProfile = null);
}
