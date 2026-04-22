using Microsoft.Extensions.Logging;
using Nocturne.Core.Contracts.Profiles;
using Nocturne.Core.Models;

namespace Nocturne.API.Services.ChartData.Stages;

/// <summary>
/// Chart data pipeline stage that loads profile data and derives the configuration values
/// used by all subsequent stages: timezone, glucose thresholds, and default basal rate.
/// </summary>
/// <remarks>
/// <para>
/// Up to 100 profiles are fetched and loaded into the stateful <see cref="IProfileService"/>
/// singleton so that time-indexed lookups (low/high BG targets, basal rate, DIA) are available
/// to later stages without additional database round trips.
/// </para>
/// <para>
/// Hard-coded very-low (54 mg/dL) and very-high (250 mg/dL) thresholds are not configurable
/// per profile; low and high targets are read from the profile at <see cref="ChartDataContext.EndTime"/>.
/// When no profile is available the fallback thresholds are 70/180 mg/dL and 1.0 U/hr basal.
/// </para>
/// </remarks>
/// <seealso cref="IChartDataStage"/>
/// <seealso cref="ChartDataContext"/>
internal sealed class ProfileLoadStage(
    IProfileDataService profileDataService,
    IProfileService profileService,
    ILogger<ProfileLoadStage> logger
) : IChartDataStage
{
    private const double DefaultVeryLow = 54;
    private const double DefaultVeryHigh = 250;

    public async Task<ChartDataContext> ExecuteAsync(ChartDataContext context, CancellationToken cancellationToken)
    {
        var profiles = await profileDataService.GetProfilesAsync(count: 100, cancellationToken: cancellationToken);
        var profileList = profiles?.ToList() ?? new List<Profile>();

        if (profileList.Count > 0)
        {
            profileService.LoadData(profileList);
            logger.LogDebug("Loaded {Count} profiles into profile service", profileList.Count);
        }

        var timezone = profileService.HasData() ? profileService.GetTimezone() : null;

        ChartThresholdsDto thresholds;
        double defaultBasalRate;

        if (profileService.HasData())
        {
            thresholds = new ChartThresholdsDto
            {
                VeryLow = DefaultVeryLow,
                Low = profileService.GetLowBGTarget(context.EndTime, null),
                High = profileService.GetHighBGTarget(context.EndTime, null),
                VeryHigh = DefaultVeryHigh,
            };
            defaultBasalRate = profileService.GetBasalRate(context.EndTime, null);
        }
        else
        {
            thresholds = new ChartThresholdsDto
            {
                VeryLow = DefaultVeryLow,
                Low = 70,
                High = 180,
                VeryHigh = DefaultVeryHigh,
            };
            defaultBasalRate = 1.0;
        }

        return context with
        {
            Timezone = timezone,
            Thresholds = thresholds,
            DefaultBasalRate = defaultBasalRate,
        };
    }
}
