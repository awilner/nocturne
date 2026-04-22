using Nocturne.API.Services.Analytics;
using Nocturne.Core.Contracts;
using Nocturne.Core.Contracts.Treatments;
using Nocturne.Core.Models;

namespace Nocturne.API.Services.ChartData.Stages;

/// <summary>
/// Chart data pipeline stage that bridges the v4 data model to the legacy treatment model
/// required by the IOB/COB algorithms.
/// </summary>
/// <remarks>
/// <para>
/// The v4 data model stores boluses and carb intakes as separate first-class entities
/// (<see cref="Bolus"/> and <see cref="CarbIntake"/>), whereas <see cref="IIobService"/>
/// and <see cref="ICobService"/> operate on the legacy <see cref="Treatment"/> type.
/// This stage calls <see cref="ChartDataService.BuildTreatmentsFromV4Data"/> to produce
/// synthetic <see cref="Treatment"/> objects and writes them to
/// <see cref="ChartDataContext.SyntheticTreatments"/>.
/// </para>
/// <para>
/// <see cref="TreatmentFood"/> records for the extended buffer window are also fetched and
/// grouped by <c>CarbIntakeId</c> so that multi-food carb entries with time offsets can be
/// correctly represented downstream in <see cref="DtoMappingStage"/>.
/// </para>
/// </remarks>
/// <seealso cref="IChartDataStage"/>
/// <seealso cref="ChartDataContext"/>
internal sealed class TreatmentAdapterStage(ITreatmentFoodService treatmentFoodService) : IChartDataStage
{
    public async Task<ChartDataContext> ExecuteAsync(ChartDataContext context, CancellationToken cancellationToken)
    {
        var carbIntakeIds = context.CarbIntakeList.Select(c => c.Id).ToList();

        var allTreatmentFoods = await treatmentFoodService.GetByCarbIntakeIdsAsync(carbIntakeIds, cancellationToken);

        var foodsByCarbIntake = allTreatmentFoods
            .GroupBy(f => f.CarbIntakeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var syntheticTreatments = ChartDataService.BuildTreatmentsFromV4Data(
            context.BolusList.ToList(),
            context.CarbIntakeList.ToList(),
            foodsByCarbIntake
        );

        return context with
        {
            SyntheticTreatments = syntheticTreatments,
            FoodsByCarbIntake = foodsByCarbIntake,
        };
    }
}
