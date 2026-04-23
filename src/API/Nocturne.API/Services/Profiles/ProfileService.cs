using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nocturne.API.Services.Treatments;
using Nocturne.Core.Contracts.Profiles;
using Nocturne.Core.Contracts.Multitenancy;
using Nocturne.Core.Contracts.V4.Repositories;
using Nocturne.Core.Models;
using Nocturne.Core.Models.V4;

namespace Nocturne.API.Services.Profiles;

/// <summary>
/// Full 1:1 legacy-compatible implementation of profile functions for the <see cref="Profile"/> domain model.
/// Based on ClientApp/lib/profilefunctions.js with exact algorithm matching.
/// </summary>
/// <remarks>
/// Profile values (DIA, sensitivity, carb ratio, basal rate, targets) are resolved from time-based
/// schedules within a <see cref="ProfileData"/> store. Supports CircadianPercentageProfile adjustments
/// and caches resolved values via <see cref="IMemoryCache"/> with a 5-second TTL.
/// When a <see cref="PatientInsulin"/> with a primary bolus configuration exists, its DIA overrides
/// the profile-level DIA value.
/// </remarks>
/// <seealso cref="IProfileService"/>
/// <seealso cref="ProfileDataService"/>
/// <seealso cref="IobService"/>
/// <seealso cref="CobService"/>
/// <seealso cref="PatientInsulin"/>
public class ProfileService : IProfileService
{
    private readonly IMemoryCache _cache;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IPatientInsulinRepository? _insulinRepository;
    private readonly ILogger<ProfileService>? _logger;
    private const int CacheTtlMs = 5000; // 5 seconds cache TTL like legacy

    private string TenantCacheId => _tenantAccessor.Context?.TenantId.ToString()
        ?? throw new InvalidOperationException("Tenant context is not resolved");

    private List<Profile>? _profileData;
    private List<Treatment> _profileTreatments = new();
    private List<Treatment> _tempBasalTreatments = new();
    private List<Treatment> _comboBolusTreatments = new();
    private Treatment? _prevBasalTreatment;

    /// <summary>
    /// Cached DIA from the patient's primary bolus insulin, loaded eagerly in LoadData.
    /// Null means no primary bolus insulin is configured.
    /// </summary>
    private double? _primaryBolusInsulinDia;
    private bool _primaryBolusInsulinDiaLoaded;

    /// <summary>
    /// Initializes a new instance of <see cref="ProfileService"/>.
    /// </summary>
    /// <param name="cache">In-memory cache for resolved profile values.</param>
    /// <param name="tenantAccessor">Provides the current tenant context for cache key scoping.</param>
    /// <param name="insulinRepository">Optional repository for loading the patient's primary bolus insulin DIA.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ProfileService(
        IMemoryCache cache,
        ITenantAccessor tenantAccessor,
        IPatientInsulinRepository? insulinRepository = null,
        ILogger<ProfileService>? logger = null)
    {
        _cache = cache;
        _tenantAccessor = tenantAccessor;
        _insulinRepository = insulinRepository;
        _logger = logger;
    }

    /// <summary>
    /// Clears all cached profile data and resets the primary bolus insulin DIA state.
    /// Individual <see cref="IMemoryCache"/> entries expire based on TTL.
    /// </summary>
    public void Clear()
    {
        // Note: IMemoryCache doesn't have a Clear method in .NET
        // Individual cache entries will expire based on TTL
        _profileData = null;
        _prevBasalTreatment = null;
        _primaryBolusInsulinDia = null;
        _primaryBolusInsulinDiaLoaded = false;
        _profileTreatments.Clear();
        _tempBasalTreatments.Clear();
        _comboBolusTreatments.Clear();
    }

    /// <summary>
    /// Loads profile data, converts legacy formats via <see cref="ConvertToProfileStore"/>,
    /// preprocesses time-based schedules, and eagerly loads the primary bolus insulin DIA
    /// from <see cref="IPatientInsulinRepository"/>.
    /// </summary>
    /// <param name="profileData">The list of <see cref="Profile"/> records to load.</param>
    public void LoadData(List<Profile> profileData)
    {
        if (profileData?.Any() == true)
        {
            _profileData = ConvertToProfileStore(profileData);

            // Process each profile and preprocess time values
            foreach (var record in _profileData)
            {
                if (record.Store?.Any() == true)
                {
                    foreach (var profile in record.Store.Values)
                    {
                        PreprocessProfileOnLoad(profile);
                    }
                }
                record.Mills = DateTimeOffset.Parse(record.StartDate).ToUnixTimeMilliseconds();
            }
        }

        // Eagerly load the primary bolus insulin DIA so GetDIA can remain synchronous
        LoadPrimaryBolusInsulinDia();
    }

    /// <inheritdoc />
    public bool HasData() => _profileData?.Any() == true;

    /// <summary>
    /// Gets the active <see cref="Profile"/> for the given time, resolving profile switches from
    /// <see cref="Treatment"/> records and CircadianPercentageProfile overrides.
    /// </summary>
    /// <param name="time">Unix millisecond timestamp; defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name to resolve.</param>
    /// <returns>A <see cref="Profile"/> wrapping the resolved <see cref="ProfileData"/>.</returns>
    public Profile? GetCurrentProfile(long? time = null, string? specProfile = null)
    {
        time ??= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Round to the minute for better caching (like legacy)
        var minuteTime = (long)(Math.Round(time.Value / 60000.0) * 60000);
        var cacheKey = $"profile:{TenantCacheId}:{minuteTime}:{specProfile}";

        if (_cache.TryGetValue(cacheKey, out ProfileData? cachedResult))
        {
            return CreateProfileFromData(cachedResult);
        }

        var pdataActive = ProfileFromTime(time.Value);
        var data = HasData() ? pdataActive : null;
        var timeProfile = GetActiveProfileName(time.Value);

        var returnValue =
            data?.Store?.ContainsKey(timeProfile ?? "") == true
                ? data.Store[timeProfile!]
                : new ProfileData();

        _cache.Set(cacheKey, returnValue, TimeSpan.FromMilliseconds(CacheTtlMs));

        return CreateProfileFromData(returnValue);
    }

    /// <inheritdoc />
    public string? GetActiveProfileName(long? time = null)
    {
        if (!HasData())
            return null;

        time ??= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var pdataActive = ProfileFromTime(time.Value);
        var timeProfile = pdataActive?.DefaultProfile;
        var treatment = GetActiveProfileTreatment(time.Value);

        if (treatment != null && pdataActive?.Store?.ContainsKey(treatment.Profile ?? "") == true)
        {
            timeProfile = treatment.Profile;
        }

        return timeProfile;
    }

    /// <inheritdoc />
    public List<string> ListBasalProfiles()
    {
        var profiles = new List<string>();

        if (HasData())
        {
            var current = GetActiveProfileName();
            if (!string.IsNullOrEmpty(current))
            {
                profiles.Add(current);
            }

            var firstProfile = _profileData?.FirstOrDefault();
            if (firstProfile?.Store?.Any() == true)
            {
                foreach (var key in firstProfile.Store.Keys)
                {
                    if (key != current && !key.Contains("@@@@@"))
                    {
                        profiles.Add(key);
                    }
                }
            }
        }

        return profiles;
    }

    /// <inheritdoc />
    public string? GetUnits(string? specProfile = null)
    {
        var currentProfile = GetCurrentProfile(null, specProfile);
        var units = currentProfile?.Store?.Values.FirstOrDefault()?.Units ?? "";

        return units.ToLowerInvariant().Contains("mmol") ? "mmol" : "mg/dl";
    }

    /// <inheritdoc />
    public string? GetTimezone(string? specProfile = null)
    {
        var currentProfile = GetCurrentProfile(null, specProfile);
        var timezone = currentProfile?.Store?.Values.FirstOrDefault()?.Timezone;

        // Work around Loop uploading non-ISO compliant time zone string
        if (!string.IsNullOrEmpty(timezone))
        {
            timezone = timezone.Replace("ETC", "Etc");
        }

        return timezone;
    }

    /// <summary>
    /// Resolves a time-scheduled profile value (basal, sens, carbratio, etc.) at the given
    /// Unix millisecond timestamp. Applies CircadianPercentageProfile adjustments when active.
    /// </summary>
    /// <param name="time">Unix millisecond timestamp.</param>
    /// <param name="valueType">
    /// The profile value type key: <c>"dia"</c>, <c>"sens"</c>, <c>"carbratio"</c>,
    /// <c>"carbs_hr"</c>, <c>"target_low"</c>, <c>"target_high"</c>, or <c>"basal"</c>.
    /// </param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>The resolved value, or a sensible default if the profile data is missing.</returns>
    public double GetValueByTime(long time, string valueType, string? specProfile = null)
    {
        // Round to the minute for better caching
        var minuteTime = (long)(Math.Round(time / 60000.0) * 60000);
        var cacheKey = $"profileValue:{TenantCacheId}:{minuteTime}:{valueType}:{specProfile}";

        if (_cache.TryGetValue(cacheKey, out double cachedValue))
        {
            return cachedValue;
        }

        // CircadianPercentageProfile support
        var timeshift = 0.0;
        var percentage = 100.0;
        var activeTreatment = GetActiveProfileTreatment(time);
        var isCcpProfile =
            string.IsNullOrEmpty(specProfile)
            && activeTreatment?.CircadianPercentageProfile == true;

        if (isCcpProfile)
        {
            percentage = activeTreatment?.Percentage ?? 100.0;
            timeshift = activeTreatment?.Timeshift ?? 0.0; // in hours
        }

        var offset = timeshift % 24;
        var adjustedTime = time + (long)(offset * 3600000); // Convert hours to milliseconds

        var currentProfile = GetCurrentProfile(adjustedTime, specProfile);
        var profileData = currentProfile?.Store?.Values.FirstOrDefault();

        if (profileData == null)
        {
            return GetDefaultValue(valueType);
        }

        var valueContainer = GetValueContainer(profileData, valueType);

        // Convert time to seconds from midnight (like legacy)
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(minuteTime);
        var timeZone = GetTimezone(specProfile);

        if (!string.IsNullOrEmpty(timeZone))
        {
            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                dateTime = TimeZoneInfo.ConvertTime(dateTime, timeZoneInfo);
            }
            catch
            {
                // Fall back to UTC if timezone conversion fails
            }
        }

        var midnight = dateTime.Date;
        var timeAsSecondsFromMidnight = (int)(dateTime - midnight).TotalSeconds;

        var returnValue = GetValueFromContainer(
            valueContainer,
            timeAsSecondsFromMidnight,
            valueType
        );

        // Apply CircadianPercentageProfile adjustments
        if (isCcpProfile && returnValue != 0)
        {
            switch (valueType)
            {
                case "sens":
                case "carbratio":
                    returnValue = returnValue * 100 / percentage;
                    break;
                case "basal":
                    returnValue = returnValue * percentage / 100;
                    break;
            }
        }

        _cache.Set(cacheKey, returnValue, TimeSpan.FromMilliseconds(CacheTtlMs));
        return returnValue;
    }

    /// <summary>
    /// Gets the Duration of Insulin Action (DIA) in hours at the given time.
    /// </summary>
    /// <remarks>
    /// Resolution priority:
    /// <list type="number">
    ///   <item>If the profile is externally managed (e.g. Loop, Glooko), the profile's own DIA is used.</item>
    ///   <item>If a primary bolus <see cref="PatientInsulin"/> is configured, its DIA is used.</item>
    ///   <item>Falls back to the profile-level DIA schedule.</item>
    /// </list>
    /// </remarks>
    /// <param name="time">Unix millisecond timestamp.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>DIA in hours (default 3.0).</returns>
    public double GetDIA(long time, string? specProfile = null)
    {
        // If the current profile is externally managed (e.g. Loop, Glooko),
        // always use the profile's own DIA value
        var currentProfile = ProfileFromTime(time);
        if (currentProfile?.IsExternallyManaged == true)
        {
            return GetValueByTime(time, "dia", specProfile);
        }

        // If a primary bolus insulin is configured, use its DIA
        if (_primaryBolusInsulinDia.HasValue)
        {
            return _primaryBolusInsulinDia.Value;
        }

        // Fall back to profile DIA (existing behavior)
        return GetValueByTime(time, "dia", specProfile);
    }

    /// <inheritdoc />
    public double GetSensitivity(long time, string? specProfile = null) =>
        GetValueByTime(time, "sens", specProfile);

    /// <inheritdoc />
    public double GetCarbRatio(long time, string? specProfile = null) =>
        GetValueByTime(time, "carbratio", specProfile);

    /// <inheritdoc />
    public double GetCarbAbsorptionRate(long time, string? specProfile = null) =>
        GetValueByTime(time, "carbs_hr", specProfile);

    /// <inheritdoc />
    public double GetLowBGTarget(long time, string? specProfile = null) =>
        GetValueByTime(time, "target_low", specProfile);

    /// <inheritdoc />
    public double GetHighBGTarget(long time, string? specProfile = null) =>
        GetValueByTime(time, "target_high", specProfile);

    /// <inheritdoc />
    public double GetBasalRate(long time, string? specProfile = null) =>
        GetValueByTime(time, "basal", specProfile);

    /// <summary>
    /// Updates the treatment lists used for profile switch, temp basal, and combo bolus resolution.
    /// Deduplicates temp basals by <see cref="Treatment.Mills"/>, computes
    /// <see cref="Treatment.EndMills"/>, and sorts by time.
    /// </summary>
    /// <param name="profileTreatments">Profile switch treatments.</param>
    /// <param name="tempBasalTreatments">Temporary basal treatments.</param>
    /// <param name="comboBolusTreatments">Combo bolus treatments.</param>
    public void UpdateTreatments(
        List<Treatment>? profileTreatments = null,
        List<Treatment>? tempBasalTreatments = null,
        List<Treatment>? comboBolusTreatments = null
    )
    {
        _profileTreatments = profileTreatments ?? new List<Treatment>();
        _tempBasalTreatments = tempBasalTreatments ?? new List<Treatment>();
        _comboBolusTreatments = comboBolusTreatments ?? new List<Treatment>();

        // Dedupe temp basal events by mills (like legacy uniqBy)
        _tempBasalTreatments = _tempBasalTreatments
            .GroupBy(t => t.Mills)
            .Select(g => g.First())
            .ToList();

        // Add duration end mills for temp basals
        foreach (var treatment in _tempBasalTreatments)
        {
            var durationMs = (long)((treatment.Duration ?? 0) * 60000); // Convert minutes to milliseconds
            treatment.EndMills = treatment.Mills + durationMs;
        }

        // Sort by mills
        _tempBasalTreatments.Sort((a, b) => a.Mills.CompareTo(b.Mills));

        // Clear cache by creating a new instance (workaround for IMemoryCache not having Clear)
        // In practice, individual cache entries will expire naturally
    }

    /// <summary>
    /// Gets the active profile switch <see cref="Treatment"/> at the given time.
    /// Handles CircadianPercentageProfile treatments and inline profile JSON.
    /// </summary>
    /// <param name="time">Unix millisecond timestamp.</param>
    /// <returns>The active profile switch treatment, or <see langword="null"/> if none.</returns>
    public Treatment? GetActiveProfileTreatment(long time)
    {
        var minuteTime = (long)(Math.Round(time / 60000.0) * 60000);
        var cacheKey = $"profileCache:{TenantCacheId}:{minuteTime}";

        if (_cache.TryGetValue(cacheKey, out Treatment? cachedTreatment))
        {
            return cachedTreatment;
        }

        Treatment? treatment = null;

        if (HasData())
        {
            var pdataActive = ProfileFromTime(time);

            foreach (var t in _profileTreatments)
            {
                if (time >= t.Mills && t.Mills >= (pdataActive?.Mills ?? 0))
                {
                    var durationMs = (t.Duration ?? 0) * 60000; // Convert minutes to milliseconds

                    if (durationMs != 0 && time < t.Mills + durationMs)
                    {
                        treatment = t;
                        HandleProfileJson(treatment, pdataActive);
                    }
                    else if (durationMs == 0)
                    {
                        treatment = t;
                        HandleProfileJson(treatment, pdataActive);
                    }
                }
            }
        }

        _cache.Set(cacheKey, treatment, TimeSpan.FromMilliseconds(CacheTtlMs));
        return treatment;
    }

    /// <summary>
    /// Gets the active temp basal <see cref="Treatment"/> at the given time using O(log n) binary search.
    /// Caches the previous result for sequential access optimization.
    /// </summary>
    /// <param name="time">Unix millisecond timestamp.</param>
    /// <returns>The active temp basal treatment, or <see langword="null"/> if none.</returns>
    public Treatment? GetTempBasalTreatment(long time)
    {
        // Most queries will match the latest found value, caching improves performance
        if (
            _prevBasalTreatment != null
            && time >= _prevBasalTreatment.Mills
            && time <= (_prevBasalTreatment.EndMills ?? long.MaxValue)
        )
        {
            return _prevBasalTreatment;
        }

        // Binary search for O(log n) performance (like legacy)
        var first = 0;
        var last = _tempBasalTreatments.Count - 1;

        while (first <= last)
        {
            var i = first + (last - first) / 2;
            var t = _tempBasalTreatments[i];

            if (time >= t.Mills && time <= (t.EndMills ?? long.MaxValue))
            {
                _prevBasalTreatment = t;
                return t;
            }

            if (time < t.Mills)
            {
                last = i - 1;
            }
            else
            {
                first = i + 1;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the active combo bolus <see cref="Treatment"/> at the given time via linear scan.
    /// </summary>
    /// <param name="time">Unix millisecond timestamp.</param>
    /// <returns>The active combo bolus treatment, or <see langword="null"/> if none.</returns>
    public Treatment? GetComboBolusTreatment(long time)
    {
        foreach (var t in _comboBolusTreatments)
        {
            var durationMs = (t.Duration ?? 0) * 60000; // Convert minutes to milliseconds
            if (time < t.Mills + durationMs && time > t.Mills)
            {
                return t;
            }
        }
        return null;
    }

    /// <summary>
    /// Computes the effective basal rate at the given time, combining the scheduled basal rate
    /// with any active temp basal and combo bolus treatments.
    /// </summary>
    /// <param name="time">Unix millisecond timestamp.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>A <see cref="TempBasalResult"/> containing basal, temp basal, combo bolus, and total rates.</returns>
    public TempBasalResult GetTempBasal(long time, string? specProfile = null)
    {
        var minuteTime = (long)(Math.Round(time / 60000.0) * 60000);
        var cacheKey = $"basalCache:{TenantCacheId}:{minuteTime}:{specProfile}";
        if (_cache.TryGetValue(cacheKey, out TempBasalResult? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var basal = GetBasalRate(time, specProfile);
        var tempBasal = basal;
        var comboBolusBasal = 0.0;
        var treatment = GetTempBasalTreatment(time);
        var comboBolusTreatment = GetComboBolusTreatment(time);

        // Check for absolute temp basal rate (supports temp to 0)
        // Loop and other systems may use either 'Absolute' or 'Rate' field
        if (treatment != null && (treatment.Duration ?? 0) > 0)
        {
            if (treatment.Absolute.HasValue)
            {
                tempBasal = treatment.Absolute.Value;
            }
            else if (treatment.Rate.HasValue)
            {
                tempBasal = treatment.Rate.Value;
            }
            else if (treatment.Percent.HasValue)
            {
                tempBasal = basal * (100 + treatment.Percent.Value) / 100;
            }
            else if (treatment.Amount.HasValue)
            {
                // Fallback for systems using 'Amount' instead of 'Rate'
                tempBasal = treatment.Amount.Value;
            }
            else if (treatment.Insulin.HasValue && treatment.Duration.GetValueOrDefault() > 0)
            {
                // Fallback: use calculated Rate property
                tempBasal = treatment.Rate ?? 0;
            }
        }

        if (comboBolusTreatment?.Relative.HasValue == true)
        {
            comboBolusBasal = comboBolusTreatment.Relative.Value;
        }

        var result = new TempBasalResult
        {
            Basal = basal,
            Treatment = treatment,
            ComboBolusTreatment = comboBolusTreatment,
            TempBasal = tempBasal,
            ComboBolusBasal = comboBolusBasal,
            TotalBasal = tempBasal + comboBolusBasal,
        };

        _cache.Set(cacheKey, result, TimeSpan.FromMilliseconds(CacheTtlMs));
        return result;
    }

    // Private helper methods
    private void LoadPrimaryBolusInsulinDia()
    {
        if (_primaryBolusInsulinDiaLoaded || _insulinRepository == null)
            return;

        try
        {
            var insulin = _insulinRepository.GetPrimaryBolusInsulinAsync().GetAwaiter().GetResult();
            _primaryBolusInsulinDia = insulin?.Dia;
            _primaryBolusInsulinDiaLoaded = true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load primary bolus insulin DIA, falling back to profile DIA");
            _primaryBolusInsulinDia = null;
            _primaryBolusInsulinDiaLoaded = true;
        }
    }

    private List<Profile> ConvertToProfileStore(List<Profile> dataArray)
    {
        var convertedProfiles = new List<Profile>();

        foreach (var profile in dataArray)
        {
            if (string.IsNullOrEmpty(profile.DefaultProfile))
            {
                var newProfile = new Profile
                {
                    DefaultProfile = "Default",
                    Store = new Dictionary<string, ProfileData>(),
                    StartDate = !string.IsNullOrEmpty(profile.StartDate)
                        ? profile.StartDate
                        : "1980-01-01",
                    Id = profile.Id,
                    ConvertedOnTheFly = true,
                };

                // Move profile data to Default store entry
                var sourceData = profile.Store?.Values.FirstOrDefault();
                var defaultData = new ProfileData
                {
                    Dia = sourceData?.Dia ?? 3.0,
                    CarbsHr = sourceData?.CarbsHr ?? 20,
                    Timezone = sourceData?.Timezone,
                    Units = sourceData?.Units,
                    Basal = sourceData?.Basal ?? new List<TimeValue>(),
                    CarbRatio = sourceData?.CarbRatio ?? new List<TimeValue>(),
                    Sens = sourceData?.Sens ?? new List<TimeValue>(),
                    TargetLow = sourceData?.TargetLow ?? new List<TimeValue>(),
                    TargetHigh = sourceData?.TargetHigh ?? new List<TimeValue>(),
                };

                newProfile.Store["Default"] = defaultData;
                convertedProfiles.Add(newProfile);

                _logger?.LogDebug(
                    "Profile not updated yet. Converted profile: {ProfileId}",
                    newProfile.Id
                );
            }
            else
            {
                // Remove conversion flag if present
                profile.ConvertedOnTheFly = false;
                convertedProfiles.Add(profile);
            }
        }

        return convertedProfiles;
    }

    private void PreprocessProfileOnLoad(ProfileData profileData)
    {
        // Convert time strings to seconds for faster operations
        PreprocessTimeValues(profileData.Basal);
        PreprocessTimeValues(profileData.CarbRatio);
        PreprocessTimeValues(profileData.Sens);
        PreprocessTimeValues(profileData.TargetLow);
        PreprocessTimeValues(profileData.TargetHigh);
    }

    private void PreprocessTimeValues(List<TimeValue>? timeValues)
    {
        if (timeValues == null)
            return;

        foreach (var timeValue in timeValues)
        {
            if (!string.IsNullOrEmpty(timeValue.Time))
            {
                var seconds = TimeStringToSeconds(timeValue.Time);
                if (seconds >= 0)
                {
                    timeValue.TimeAsSeconds = seconds;
                }
            }
        }
    }

    private int TimeStringToSeconds(string time)
    {
        var parts = time.Split(':');
        if (
            parts.Length >= 2
            && int.TryParse(parts[0], out var hours)
            && int.TryParse(parts[1], out var minutes)
        )
        {
            return hours * 3600 + minutes * 60;
        }
        return -1;
    }

    private Profile? ProfileFromTime(long time)
    {
        if (!HasData())
            return null;

        Profile? profileData = _profileData![0];

        // Find the most recent profile that started before or at the given time
        foreach (var profile in _profileData!)
        {
            if (time >= profile.Mills)
            {
                profileData = profile;
            }
            else
            {
                // Profiles are assumed to be sorted by Mills, so we can break early
                break;
            }
        }

        return profileData;
    }

    private object? GetValueContainer(ProfileData profileData, string valueType)
    {
        return valueType switch
        {
            "dia" => profileData.Dia,
            "sens" => profileData.Sens,
            "carbratio" => profileData.CarbRatio,
            "carbs_hr" => profileData.CarbsHr,
            "target_low" => profileData.TargetLow,
            "target_high" => profileData.TargetHigh,
            "basal" => profileData.Basal,
            _ => null,
        };
    }

    private double GetValueFromContainer(
        object? valueContainer,
        int timeAsSecondsFromMidnight,
        string valueType
    )
    {
        if (valueContainer == null)
            return GetDefaultValue(valueType);

        // If it's a simple value (like dia, carbs_hr)
        if (valueContainer is double doubleValue)
        {
            return doubleValue;
        }

        if (valueContainer is int intValue)
        {
            return intValue;
        }

        // If it's a time-based array
        if (valueContainer is List<TimeValue> timeValues && timeValues.Any())
        {
            // Sort by time to ensure we have the correct order
            var sortedValues = timeValues.OrderBy(tv => tv.TimeAsSeconds ?? 0).ToList();
            var returnValue = sortedValues[0].Value; // Default to first (earliest) value

            // Find the most recent time slot before or at the current time
            foreach (var timeValue in sortedValues)
            {
                if (timeAsSecondsFromMidnight >= (timeValue.TimeAsSeconds ?? 0))
                {
                    returnValue = timeValue.Value;
                }
                else
                {
                    // We've gone past the current time, use the previous value
                    break;
                }
            }

            return returnValue;
        }

        return GetDefaultValue(valueType);
    }

    private double GetDefaultValue(string valueType)
    {
        return valueType switch
        {
            "dia" => 3.0,
            "sens" => 50.0,
            "carbratio" => 12.0,
            "carbs_hr" => 20.0,
            "target_low" => 70.0,
            "target_high" => 180.0,
            "basal" => 1.0,
            _ => 0.0,
        };
    }

    private Profile CreateProfileFromData(ProfileData? data)
    {
        if (data == null)
            return new Profile();

        return new Profile
        {
            Store = new Dictionary<string, ProfileData> { { "Default", data } },
            DefaultProfile = "Default",
        };
    }

    private void HandleProfileJson(Treatment treatment, Profile? pdataActive)
    {
        if (!string.IsNullOrEmpty(treatment.ProfileJson) && pdataActive != null)
        {
            var profileName = treatment.Profile ?? "";

            if (!profileName.Contains("@@@@@"))
            {
                profileName += $"@@@@@{treatment.Mills}";
                treatment.Profile = profileName;
            }

            if (!pdataActive.Store.ContainsKey(profileName))
            {
                // Parse JSON and add to store
                try
                {
                    var profileData = JsonSerializer.Deserialize<ProfileData>(
                        treatment.ProfileJson
                    );
                    if (profileData != null)
                    {
                        pdataActive.Store[profileName] = profileData;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(
                        ex,
                        "Failed to parse profile JSON for treatment {TreatmentId}",
                        treatment.Id
                    );
                }
            }
        }
    }
}
