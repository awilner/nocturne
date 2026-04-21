using Nocturne.Core.Contracts;
using Nocturne.Core.Models;
using Nocturne.Core.Models.V4;

namespace Nocturne.API.Services;

/// <summary>
/// Implementation of Insulin on Board (IOB) calculations with exact 1:1 legacy JavaScript compatibility.
/// Computes IOB from three sources: <see cref="DeviceStatus"/> (Loop, OpenAPS, pump),
/// <see cref="Treatment"/> bolus/temp-basal records, and V4 <see cref="TempBasal"/> records.
/// </summary>
/// <remarks>
/// The bolus IOB curve uses a two-phase model:
/// <list type="bullet">
///   <item>Before peak (0-75 min): curved rise with quadratic approximation.</item>
///   <item>After peak (75-180 min): curved decline to zero.</item>
/// </list>
/// Per-treatment <see cref="TreatmentInsulinContext"/> overrides profile-level DIA and peak values
/// when available, enabling accurate multi-insulin calculations.
/// </remarks>
/// <seealso cref="IIobService"/>
/// <seealso cref="IProfileService"/>
/// <seealso cref="CobService"/>
/// <seealso cref="TreatmentService"/>
public class IobService : IIobService
{
    // Constants from legacy implementation
    private const long RECENCY_THRESHOLD = 30 * 60 * 1000; // 30 minutes in milliseconds
    private const double DEFAULT_DIA = 3.0; // Default Duration of Insulin Action in hours
    private const double SCALE_FACTOR_BASE = 3.0; // Base for scale factor calculation
    private const double PEAK_MINUTES = 75.0; // Peak insulin action at 75 minutes
    private const double MAX_IOB_MINUTES = 180.0; // IOB calculation cutoff at 180 minutes

    /// <summary>
    /// Main IOB calculation function that combines <see cref="DeviceStatus"/> and <see cref="Treatment"/> data.
    /// Exact implementation of legacy calcTotal function.
    /// </summary>
    /// <remarks>
    /// Priority: device status IOB (Loop/OpenAPS/pump) takes precedence. If unavailable,
    /// treatment-based IOB is used. V4 <see cref="TempBasal"/> basal IOB is always merged
    /// into the treatment result regardless of source priority.
    /// </remarks>
    /// <param name="treatments">Bolus and temp basal treatments.</param>
    /// <param name="deviceStatus">Device status entries from Loop, OpenAPS, or pump.</param>
    /// <param name="profile">Optional profile service for DIA, sensitivity, and basal rate lookups.</param>
    /// <param name="time">Unix millisecond timestamp; defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <param name="tempBasals">Optional V4 temp basal records for parallel basal IOB calculation.</param>
    /// <returns>An <see cref="IobResult"/> containing the computed IOB, basal IOB, activity, and display strings.</returns>
    public IobResult CalculateTotal(
        List<Treatment> treatments,
        List<DeviceStatus> deviceStatus,
        IProfileService? profile = null,
        long? time = null,
        string? specProfile = null,
        List<TempBasal>? tempBasals = null
    )
    {
        var currentTime = time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Get IOB from device status (pumps, OpenAPS, Loop) - prioritized source
        var result = LastIobDeviceStatus(deviceStatus, currentTime);

        // Calculate IOB from treatments (Care Portal entries)
        var treatmentResult =
            treatments?.Any() == true
                ? FromTreatments(treatments, profile, currentTime, specProfile)
                : new IobResult();

        // Calculate basal IOB from V4 TempBasal records (parallel path to legacy treatment-based basal IOB)
        var tempBasalResult =
            tempBasals?.Any() == true
                ? FromTempBasals(tempBasals, profile, currentTime, specProfile)
                : new IobResult();

        // Merge V4 TempBasal basal IOB into the treatment result
        if (tempBasalResult.BasalIob.HasValue)
        {
            treatmentResult.BasalIob = (treatmentResult.BasalIob ?? 0) + tempBasalResult.BasalIob.Value;
            treatmentResult.Activity = (treatmentResult.Activity ?? 0) + (tempBasalResult.Activity ?? 0);
        }

        if (IsEmpty(result))
        {
            result = treatmentResult;
        }
        else
        {
            // Add treatment IOB as separate property for device status sources
            if (treatmentResult.Iob > 0)
            {
                result.TreatmentIob = RoundToThreeDecimals(treatmentResult.Iob);
            }

            // Add treatment basal IOB to device status basal IOB if available
            if (treatmentResult.BasalIob.HasValue)
            {
                result.BasalIob = (result.BasalIob ?? 0) + treatmentResult.BasalIob.Value;
                result.BasalIob = RoundToThreeDecimals(result.BasalIob.Value);
            }
        }

        // Apply final rounding to IOB
        if (result.Iob > 0)
        {
            result.Iob = RoundToThreeDecimals(result.Iob);
        }

        return AddDisplay(result);
    }

    /// <summary>
    /// Get the most recent IOB from <see cref="DeviceStatus"/> entries with prioritization.
    /// Exact implementation of legacy lastIOBDeviceStatus function.
    /// </summary>
    /// <param name="deviceStatus">The device status entries to search.</param>
    /// <param name="time">Unix millisecond timestamp for recency filtering.</param>
    /// <returns>The most recent <see cref="IobResult"/>, with Loop sources prioritized over others.</returns>
    public IobResult LastIobDeviceStatus(List<DeviceStatus> deviceStatus, long time)
    {
        if (deviceStatus?.Any() != true)
        {
            return new IobResult();
        }

        var futureMills = time + 5 * 60 * 1000; // Allow for clocks to be a little off
        var recentMills = time - RECENCY_THRESHOLD; // Get all IOBs within time range
        var iobs = deviceStatus
            .Where(status =>
                status.Mills > 0 && status.Mills <= futureMills && status.Mills >= recentMills
            )
            .Select(FromDeviceStatus)
            .Where(item => !IsEmpty(item))
            .OrderBy(iob => iob.Mills ?? 0)
            .ToList();

        if (!iobs.Any())
        {
            return new IobResult();
        }

        // Prioritize Loop IOBs if available (highest priority)
        var loopIobs = iobs.Where(iob => iob.Source == "Loop").ToList();
        if (loopIobs.Any())
        {
            return loopIobs.Last(); // Most recent Loop IOB
        }

        // Return the most recent IOB entry
        return iobs.Last();
    }

    /// <summary>
    /// Extract IOB from a single <see cref="DeviceStatus"/> entry.
    /// Priority: Loop > OpenAPS > Pump (MM Connect).
    /// </summary>
    /// <param name="deviceStatusEntry">The device status entry to extract IOB from.</param>
    /// <returns>An <see cref="IobResult"/> with source attribution, or an empty result if no IOB data found.</returns>
    public IobResult FromDeviceStatus(DeviceStatus deviceStatusEntry)
    {
        // Highest priority: Loop IOB
        if (HasLoopIob(deviceStatusEntry))
        {
            var loopIob = deviceStatusEntry.Loop!.Iob!;
            var timestamp = deviceStatusEntry.Mills; // fallback

            if (
                !string.IsNullOrEmpty(loopIob.Timestamp)
                && DateTimeOffset.TryParse(loopIob.Timestamp, out var parsedTime)
            )
            {
                timestamp = parsedTime.ToUnixTimeMilliseconds();
            }

            return new IobResult
            {
                Iob = loopIob.Iob ?? 0.0,
                Source = "Loop",
                Device = deviceStatusEntry.Device,
                Mills = timestamp,
            };
        }

        // Second priority: OpenAPS IOB
        if (HasOpenApsIob(deviceStatusEntry))
        {
            var openApsIob = deviceStatusEntry.OpenAps!.Iob!;

            var iobValue = openApsIob.Iob ?? 0.0;
            var basalIobValue = openApsIob.BasalIob;
            var activityValue = openApsIob.Activity;

            // Handle timestamp field variations (time vs timestamp)
            var timestampStr = openApsIob.Timestamp ?? openApsIob.Time;
            var timestamp = deviceStatusEntry.Mills; // fallback

            if (
                !string.IsNullOrEmpty(timestampStr)
                && DateTimeOffset.TryParse(timestampStr, out var parsedTime)
            )
            {
                timestamp = parsedTime.ToUnixTimeMilliseconds();
            }

            return new IobResult
            {
                Iob = iobValue,
                BasalIob = basalIobValue,
                Activity = activityValue,
                Source = "OpenAPS",
                Device = deviceStatusEntry.Device,
                Mills = timestamp,
            };
        }

        // Third priority: Pump IOB (MM Connect)
        if (HasPumpIob(deviceStatusEntry))
        {
            var pumpIob = deviceStatusEntry.Pump!.Iob!;
            var iobValue = pumpIob.Iob ?? pumpIob.BolusIob ?? 0.0;

            var source = deviceStatusEntry.Connect != null ? "MM Connect" : "Pump";

            return new IobResult
            {
                Iob = iobValue,
                Source = source,
                Device = deviceStatusEntry.Device,
                Mills = deviceStatusEntry.Mills,
            };
        }

        return new IobResult();
    }

    /// <summary>
    /// Calculate IOB from <see cref="Treatment"/> records (Care Portal entries) with exact legacy algorithm.
    /// Sums bolus IOB from treatments with <see cref="Treatment.Insulin"/> and basal IOB from
    /// temp basal treatments, using <see cref="CalcTreatment"/> and <see cref="CalcBasalTreatment"/>.
    /// </summary>
    /// <param name="treatments">The treatments to calculate IOB from.</param>
    /// <param name="profile">Optional <see cref="IProfileService"/> for DIA and sensitivity lookups.</param>
    /// <param name="time">Unix millisecond timestamp; defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>An <see cref="IobResult"/> with aggregated bolus IOB, basal IOB, and activity.</returns>
    public IobResult FromTreatments(
        List<Treatment> treatments,
        IProfileService? profile = null,
        long? time = null,
        string? specProfile = null
    )
    {
        var currentTime = time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (treatments?.Any() != true)
        {
            return new IobResult
            {
                Iob = 0.0,
                Activity = 0.0,
                Source = "Care Portal",
            };
        }

        var totalIob = 0.0;
        var totalActivity = 0.0;
        var totalBasalIob = 0.0;
        Treatment? lastBolus = null;

        foreach (var treatment in treatments)
        {
            if (treatment.Mills <= currentTime)
            {
                // Calculate bolus IOB from treatments with insulin
                if (treatment.Insulin.HasValue && treatment.Insulin.Value > 0)
                {
                    var contribution = CalcTreatment(treatment, profile, currentTime, specProfile);

                    if (contribution.IobContrib > 0)
                    {
                        lastBolus = treatment;
                    }

                    totalIob += contribution.IobContrib;
                    totalActivity += contribution.ActivityContrib;
                }

                // Calculate basal IOB from temp basal treatments
                if (treatment.EventType == "Temp Basal" && treatment.Duration.HasValue)
                {
                    var basalIob = CalcBasalTreatment(treatment, profile, currentTime, specProfile);
                    totalBasalIob += basalIob.IobContrib;
                    totalActivity += basalIob.ActivityContrib;
                }
            }
        }

        return new IobResult
        {
            Iob = RoundToThreeDecimals(totalIob),
            BasalIob = totalBasalIob > 0 ? RoundToThreeDecimals(totalBasalIob) : null,
            Activity = totalActivity,
            LastBolus = lastBolus,
            Source = "Care Portal",
        };
    }

    /// <summary>
    /// Calculate IOB contribution from a single <see cref="Treatment"/> using the exact legacy
    /// two-phase insulin curve. Uses <see cref="TreatmentInsulinContext.Dia"/> and
    /// <see cref="TreatmentInsulinContext.Peak"/> when available on the treatment,
    /// otherwise falls back to <see cref="IProfileService.GetDIA"/>.
    /// </summary>
    /// <param name="treatment">The treatment to calculate IOB for.</param>
    /// <param name="profile">Optional profile service for DIA/sensitivity lookups.</param>
    /// <param name="time">Unix millisecond timestamp; defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>An <see cref="IobContribution"/> with the IOB and activity contributions.</returns>
    public IobContribution CalcTreatment(
        Treatment treatment,
        IProfileService? profile = null,
        long? time = null,
        string? specProfile = null
    )
    {
        if (!treatment.Insulin.HasValue || treatment.Insulin.Value <= 0)
        {
            return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
        }

        var currentTime = time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Per-treatment insulin context takes priority over profile DIA/peak
        var dia = treatment.InsulinContext?.Dia
            ?? profile?.GetDIA(currentTime, specProfile)
            ?? DEFAULT_DIA;
        var peak = treatment.InsulinContext?.Peak
            ?? PEAK_MINUTES;
        var sens = profile?.GetSensitivity(currentTime, specProfile) ?? 50.0;

        // Exact legacy algorithm constants
        var scaleFactor = SCALE_FACTOR_BASE / dia;

        var bolusTime = treatment.Mills;
        var minAgo = (scaleFactor * (currentTime - bolusTime)) / 1000.0 / 60.0;

        // Before peak (0-75 minutes): curved rise
        if (minAgo < peak)
        {
            var x1 = minAgo / 5.0 + 1.0;
            var iobContrib = treatment.Insulin.Value * (1.0 - 0.001852 * x1 * x1 + 0.001852 * x1);
            var activityContrib =
                sens * treatment.Insulin.Value * (2.0 / dia / 60.0 / peak) * minAgo;

            return new IobContribution
            {
                IobContrib = Math.Max(0.0, iobContrib), // Prevent negative IOB
                ActivityContrib = activityContrib,
            };
        }

        // After peak (75-180 minutes): curved decline
        if (minAgo < MAX_IOB_MINUTES)
        {
            var x2 = (minAgo - peak) / 5.0;
            var iobContrib =
                treatment.Insulin.Value * (0.001323 * x2 * x2 - 0.054233 * x2 + 0.55556);
            var activityContrib =
                sens
                * treatment.Insulin.Value
                * (2.0 / dia / 60.0 - ((minAgo - peak) * 2.0) / dia / 60.0 / (60.0 * 3.0 - peak));

            return new IobContribution
            {
                IobContrib = Math.Max(0.0, iobContrib), // Prevent negative IOB
                ActivityContrib = activityContrib,
            };
        }

        // After 180 minutes: no IOB remaining
        return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
    }

    /// <summary>
    /// Calculate basal IOB contribution from a temp basal <see cref="Treatment"/> using simplified
    /// linear decay over the DIA period. Only processes treatments with
    /// <see cref="Treatment.EventType"/> of <c>"Temp Basal"</c> and non-null
    /// <see cref="Treatment.Duration"/> and <see cref="Treatment.Absolute"/>.
    /// </summary>
    /// <param name="treatment">The temp basal treatment.</param>
    /// <param name="profile">Optional profile service for DIA and basal rate lookups.</param>
    /// <param name="time">Unix millisecond timestamp; defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>An <see cref="IobContribution"/> with the basal IOB contribution.</returns>
    public IobContribution CalcBasalTreatment(
        Treatment treatment,
        IProfileService? profile = null,
        long? time = null,
        string? specProfile = null
    )
    {
        if (
            treatment.EventType != "Temp Basal"
            || !treatment.Duration.HasValue
            || !treatment.Absolute.HasValue
        )
        {
            return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
        }

        var currentTime = time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var dia = profile?.GetDIA(currentTime, specProfile) ?? DEFAULT_DIA;
        var basalRate = profile?.GetBasalRate(currentTime, specProfile) ?? 1.0;

        var treatmentStart = treatment.Mills;
        var treatmentEnd = treatmentStart + (treatment.Duration.Value * 60 * 1000); // Duration in minutes to milliseconds

        // Only calculate if current time is after treatment start
        if (currentTime <= treatmentStart)
        {
            return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
        }

        // Calculate effective insulin delivered so far
        var effectiveEnd = Math.Min(currentTime, treatmentEnd);
        var durationActual = (effectiveEnd - treatmentStart) / 1000.0 / 60.0; // minutes
        var tempRate = treatment.Absolute.Value;
        var excessInsulin = Math.Max(0, (tempRate - basalRate) * (durationActual / 60.0)); // excess insulin in units

        if (excessInsulin <= 0)
        {
            return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
        }

        // Use simplified decay similar to bolus IOB but with different parameters for basal
        var minAgo = (currentTime - treatmentStart) / 1000.0 / 60.0;
        var diaMinutes = dia * 60.0;

        // Simple linear decay over DIA period
        if (minAgo < diaMinutes)
        {
            var decayFactor = Math.Max(0, 1.0 - (minAgo / diaMinutes));
            var basalIob = excessInsulin * decayFactor;

            return new IobContribution
            {
                IobContrib = RoundToThreeDecimals(basalIob),
                ActivityContrib = 0, // Simplified - no activity calculation for basal
            };
        }

        return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
    }

    /// <summary>
    /// Calculate basal IOB contribution from a V4 <see cref="TempBasal"/> record.
    /// Uses the same simplified linear decay algorithm as <see cref="CalcBasalTreatment"/>
    /// but operates on the typed <see cref="TempBasal"/> model instead of legacy <see cref="Treatment"/> objects.
    /// </summary>
    /// <remarks>
    /// For <see cref="TempBasalOrigin.Suspended"/> records, the rate is treated as zero (pump was suspended).
    /// Uses <see cref="TempBasal.ScheduledRate"/> when available, otherwise falls back to
    /// <see cref="IProfileService.GetBasalRate"/>.
    /// </remarks>
    /// <param name="tempBasal">The V4 temp basal record.</param>
    /// <param name="profile">Optional profile service for DIA and basal rate lookups.</param>
    /// <param name="time">Unix millisecond timestamp; defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>An <see cref="IobContribution"/> with the basal IOB contribution.</returns>
    public IobContribution CalcTempBasalIob(
        TempBasal tempBasal,
        IProfileService? profile = null,
        long? time = null,
        string? specProfile = null
    )
    {
        var currentTime = time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Cannot calculate IOB for a temp basal with no end time (still active has no known duration)
        if (!tempBasal.EndMills.HasValue)
        {
            return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
        }

        var dia = profile?.GetDIA(currentTime, specProfile) ?? DEFAULT_DIA;

        // Use ScheduledRate from the TempBasal if available, otherwise fall back to profile lookup
        var scheduledBasalRate = tempBasal.ScheduledRate
            ?? profile?.GetBasalRate(tempBasal.StartMills, specProfile)
            ?? 1.0;

        var treatmentStart = tempBasal.StartMills;
        var treatmentEnd = tempBasal.EndMills.Value;

        // Only calculate if current time is after treatment start
        if (currentTime <= treatmentStart)
        {
            return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
        }

        // Calculate effective insulin delivered so far
        var effectiveEnd = Math.Min(currentTime, treatmentEnd);
        var durationActual = (effectiveEnd - treatmentStart) / 1000.0 / 60.0; // minutes

        // For Suspended origin, rate is 0 (pump was suspended)
        var rate = tempBasal.Origin == TempBasalOrigin.Suspended ? 0 : tempBasal.Rate;
        var excessInsulin = Math.Max(0, (rate - scheduledBasalRate) * (durationActual / 60.0)); // excess insulin in units

        if (excessInsulin <= 0)
        {
            return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
        }

        // Simple linear decay over DIA period — identical to CalcBasalTreatment
        var minAgo = (currentTime - treatmentStart) / 1000.0 / 60.0;
        var diaMinutes = dia * 60.0;

        if (minAgo < diaMinutes)
        {
            var decayFactor = Math.Max(0, 1.0 - (minAgo / diaMinutes));
            var basalIob = excessInsulin * decayFactor;

            return new IobContribution
            {
                IobContrib = RoundToThreeDecimals(basalIob),
                ActivityContrib = 0, // Simplified — no activity calculation for basal
            };
        }

        return new IobContribution { IobContrib = 0, ActivityContrib = 0 };
    }

    /// <summary>
    /// Calculate aggregated basal IOB from a list of V4 <see cref="TempBasal"/> records.
    /// Parallel path to the temp basal loop in <see cref="FromTreatments"/>, operating on
    /// typed <see cref="TempBasal"/> records instead of legacy <see cref="Treatment"/> objects.
    /// </summary>
    /// <param name="tempBasals">The V4 temp basal records.</param>
    /// <param name="profile">Optional profile service for DIA and basal rate lookups.</param>
    /// <param name="time">Unix millisecond timestamp; defaults to now.</param>
    /// <param name="specProfile">Optional specific profile name.</param>
    /// <returns>An <see cref="IobResult"/> with basal IOB only (bolus IOB is always zero).</returns>
    public IobResult FromTempBasals(
        List<TempBasal> tempBasals,
        IProfileService? profile = null,
        long? time = null,
        string? specProfile = null
    )
    {
        var currentTime = time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (tempBasals?.Any() != true)
        {
            return new IobResult
            {
                Iob = 0.0,
                Activity = 0.0,
                Source = "Care Portal",
            };
        }

        var totalBasalIob = 0.0;
        var totalActivity = 0.0;

        foreach (var tempBasal in tempBasals)
        {
            if (tempBasal.StartMills <= currentTime)
            {
                var contribution = CalcTempBasalIob(tempBasal, profile, currentTime, specProfile);
                totalBasalIob += contribution.IobContrib;
                totalActivity += contribution.ActivityContrib;
            }
        }

        return new IobResult
        {
            Iob = 0.0, // Basal IOB does not contribute to bolus IOB
            BasalIob = totalBasalIob > 0 ? RoundToThreeDecimals(totalBasalIob) : null,
            Activity = totalActivity,
            Source = "Care Portal",
        };
    }

    #region Helper Methods

    /// <summary>
    /// Add display formatting to IOB result - exact legacy implementation
    /// </summary>
    private static IobResult AddDisplay(IobResult iob)
    {
        if (IsEmpty(iob) || iob.Iob <= 0)
        {
            return iob;
        }

        var display = iob.Iob.ToString("F2");
        iob.Display = display;
        iob.DisplayLine = $"IOB: {display}U";

        return iob;
    }

    /// <summary>
    /// Check if IOB result is empty
    /// </summary>
    private static bool IsEmpty(IobResult? iob)
    {
        return iob == null || (iob.Iob <= 0 && !iob.BasalIob.HasValue && !iob.Activity.HasValue);
    }

    /// <summary>
    /// Round to three decimal places with exact legacy precision
    /// </summary>
    private static double RoundToThreeDecimals(double num)
    {
        return Math.Round(num + double.Epsilon, 3);
    }

    /// <summary>
    /// Type guard for Loop IOB data
    /// </summary>
    private static bool HasLoopIob(DeviceStatus deviceStatus)
    {
        return deviceStatus.Loop?.Iob != null;
    }

    /// <summary>
    /// Type guard for OpenAPS IOB data
    /// </summary>
    private static bool HasOpenApsIob(DeviceStatus deviceStatus)
    {
        return deviceStatus.OpenAps?.Iob != null;
    }

    /// <summary>
    /// Type guard for Pump IOB data
    /// </summary>
    private static bool HasPumpIob(DeviceStatus deviceStatus)
    {
        return deviceStatus.Pump?.Iob != null;
    }

    #endregion
}
